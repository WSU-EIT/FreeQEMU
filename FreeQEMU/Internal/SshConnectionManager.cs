using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Renci.SshNet;

namespace FreeQEMU.Internal;

// <summary>
// Manages SSH connections for command execution and file transfers.
// </summary>
internal sealed class SshConnectionManager : ISshConnectionManager
{
    private readonly VmConfiguration _config;
    private bool _disposed;

    public SshConnectionManager(VmConfiguration config)
    {
        _config = config;
    }

    private void Log(string message)
    {
        if (!_config.QuietMode)
            Console.WriteLine(message);
    }

    public async Task WaitForConnectionAsync(int timeoutSeconds, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        int attempts = 0;

        Log($"  [SSH] Waiting for SSH on port {_config.ActualSshPort}...");

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempts++;

            if (await TryConnectAsync(cancellationToken))
            {
                Log($"  [SSH] Port {_config.ActualSshPort} is open after {stopwatch.Elapsed.TotalSeconds:F1}s ({attempts} attempts)");

                // Port is open, but SSH might not be fully ready - give cloud-init time to set up keys
                Log($"  [SSH] Waiting for cloud-init to configure SSH keys...");
                await Task.Delay(5000, cancellationToken); // Wait 5s for cloud-init
                
                Log($"  [SSH] Testing SSH authentication...");
                
                if (await TryAuthenticateAsync(timeout - stopwatch.Elapsed, cancellationToken))
                {
                    Log($"  [SSH] Authentication successful!");
                    return;
                }
            }

            if (attempts % 10 == 0)
            {
                Log($"  [SSH] Still waiting... ({stopwatch.Elapsed.TotalSeconds:F0}s elapsed, {attempts} attempts)");
            }

            await Task.Delay(1000, cancellationToken);
        }

        throw new SshConnectionException($"SSH connection timed out after {timeoutSeconds} seconds ({attempts} attempts)");
    }

    private async Task<bool> TryAuthenticateAsync(TimeSpan remainingTimeout, CancellationToken cancellationToken)
    {
        var authStopwatch = Stopwatch.StartNew();
        var maxAuthAttempts = 30; // Try for up to 30 attempts
        
        for (int i = 0; i < maxAuthAttempts && authStopwatch.Elapsed < remainingTimeout; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Log($"  [SSH] Auth attempt {i + 1}/{maxAuthAttempts}...");
            
            try
            {
                using var client = CreateSshClient();
                client.Connect();
                
                // Try a simple command to verify connection works
                using var cmd = client.CreateCommand("echo ok");
                cmd.CommandTimeout = TimeSpan.FromSeconds(10);
                var result = cmd.Execute();
                
                client.Disconnect();
                
                if (result.Trim() == "ok")
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"  [SSH] Auth attempt {i + 1} failed: {ex.Message}");
            }
            
            await Task.Delay(2000, cancellationToken); // Wait 2s between auth attempts
        }
        
        return false;
    }


    public async Task<CommandResult> ExecuteCommandAsync(
        string command,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return await ExecuteCommandAsync(command, null, null, timeoutSeconds, cancellationToken);
    }

    public async Task<CommandResult> ExecuteCommandAsync(
        string command,
        Action<string>? onOutput,
        Action<string>? onError,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        using var client = CreateSshClient();

        try
        {
            await ConnectWithRetryAsync(client, cancellationToken);

            using var cmd = client.CreateCommand(command);
            cmd.CommandTimeout = TimeSpan.FromSeconds(timeoutSeconds);

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            var asyncResult = cmd.BeginExecute();

            // Stream output if handlers provided
            if (onOutput != null || onError != null)
            {
                using var outputReader = new StreamReader(cmd.OutputStream);
                using var errorReader = new StreamReader(cmd.ExtendedOutputStream);

                while (!asyncResult.IsCompleted)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    while (!outputReader.EndOfStream)
                    {
                        var line = await outputReader.ReadLineAsync(cancellationToken);
                        if (line != null)
                        {
                            outputBuilder.AppendLine(line);
                            onOutput?.Invoke(line);
                        }
                    }

                    while (!errorReader.EndOfStream)
                    {
                        var line = await errorReader.ReadLineAsync(cancellationToken);
                        if (line != null)
                        {
                            errorBuilder.AppendLine(line);
                            onError?.Invoke(line);
                        }
                    }

                    await Task.Delay(100, cancellationToken);
                }

                // Read remaining output
                outputBuilder.Append(await outputReader.ReadToEndAsync(cancellationToken));
                errorBuilder.Append(await errorReader.ReadToEndAsync(cancellationToken));
            }

            cmd.EndExecute(asyncResult);

            // If no streaming, read all output now
            if (onOutput == null && onError == null)
            {
                outputBuilder.Append(cmd.Result);
                errorBuilder.Append(cmd.Error);
            }

            stopwatch.Stop();

            var output = outputBuilder.ToString().TrimEnd();
            var error = errorBuilder.ToString().TrimEnd();

            return cmd.ExitStatus == 0
                ? CommandResult.Succeeded(output, stopwatch.Elapsed)
                : CommandResult.Failed(cmd.ExitStatus ?? -1, output, error, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            throw new SshConnectionException($"Command execution failed: {ex.Message}", ex);
        }
    }

    public async Task UploadFolderAsync(
        string localPath,
        string remotePath,
        IProgress<FileTransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(localPath))
            throw new DirectoryNotFoundException($"Local path not found: {localPath}");

        using var ssh = CreateSshClient();
        using var scp = CreateScpClient();

        await ConnectWithRetryAsync(ssh, cancellationToken);
        await ConnectWithRetryAsync(scp, cancellationToken);

        // Clean and create remote directory
        ssh.RunCommand($"rm -rf {remotePath}");
        ssh.RunCommand($"mkdir -p {remotePath}");

        var files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories)
            .Where(f => !ShouldIgnore(f, localPath))
            .ToList();

        var totalFiles = files.Count;
        var filesTransferred = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(localPath, file).Replace('\\', '/');
            var remoteFilePath = $"{remotePath}/{relativePath}";
            var remoteDir = Path.GetDirectoryName(remoteFilePath)?.Replace('\\', '/');

            if (!string.IsNullOrEmpty(remoteDir) && remoteDir != remotePath)
            {
                ssh.RunCommand($"mkdir -p \"{remoteDir}\"");
            }

            await using var fileStream = File.OpenRead(file);
            scp.Upload(fileStream, remoteFilePath);

            filesTransferred++;
            progress?.Report(new FileTransferProgress
            {
                CurrentFile = relativePath,
                FilesTransferred = filesTransferred,
                TotalFiles = totalFiles
            });
        }
    }

    public async Task DownloadFolderAsync(
        string remotePath,
        string localPath,
        IProgress<FileTransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        localPath = Path.GetFullPath(localPath);
        Directory.CreateDirectory(localPath);

        using var ssh = CreateSshClient();
        using var scp = CreateScpClient();

        await ConnectWithRetryAsync(ssh, cancellationToken);
        await ConnectWithRetryAsync(scp, cancellationToken);

        // Get list of files
        var result = ssh.RunCommand($"find {remotePath} -type f 2>/dev/null");
        if (result.ExitStatus != 0 || string.IsNullOrWhiteSpace(result.Result))
        {
            return; // No files to download
        }

        var remoteFiles = result.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var totalFiles = remoteFiles.Length;
        var filesTransferred = 0;

        foreach (var remoteFile in remoteFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var trimmedPath = remoteFile.Trim();
            if (string.IsNullOrEmpty(trimmedPath)) continue;

            var relativePath = trimmedPath.StartsWith(remotePath)
                ? trimmedPath[remotePath.Length..].TrimStart('/')
                : Path.GetFileName(trimmedPath);

            var localFilePath = Path.Combine(localPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var localDir = Path.GetDirectoryName(localFilePath);

            if (!string.IsNullOrEmpty(localDir))
                Directory.CreateDirectory(localDir);

            await using var fileStream = File.Create(localFilePath);
            scp.Download(trimmedPath, fileStream);

            filesTransferred++;
            progress?.Report(new FileTransferProgress
            {
                CurrentFile = relativePath,
                FilesTransferred = filesTransferred,
                TotalFiles = totalFiles
            });
        }
    }

    private SshClient CreateSshClient()
    {
        if (!File.Exists(_config.PrivateKeyPath))
            throw new SshConnectionException("SSH private key not found");

        var connectionInfo = new ConnectionInfo(
            "127.0.0.1",
            _config.ActualSshPort,
            _config.SshUsername,
            new PrivateKeyAuthenticationMethod(_config.SshUsername, new PrivateKeyFile(_config.PrivateKeyPath)))
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        return new SshClient(connectionInfo);
    }

    private ScpClient CreateScpClient()
    {
        var connectionInfo = new ConnectionInfo(
            "127.0.0.1",
            _config.ActualSshPort,
            _config.SshUsername,
            new PrivateKeyAuthenticationMethod(_config.SshUsername, new PrivateKeyFile(_config.PrivateKeyPath)))
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        return new ScpClient(connectionInfo);
    }

    private async Task<bool> TryConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", _config.ActualSshPort, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ConnectWithRetryAsync(SshClient client, CancellationToken cancellationToken, int maxRetries = 3)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                Log($"  [SSH] Connection attempt {attempt}/{maxRetries}...");
                client.Connect();
                Log($"  [SSH] Connected successfully!");
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Log($"  [SSH] Attempt {attempt} failed: {ex.Message}");
                if (attempt < maxRetries)
                {
                    var delay = attempt * 2;
                    Log($"  [SSH] Retrying in {delay}s...");
                    await Task.Delay(delay * 1000, cancellationToken);
                }
            }
        }

        throw new SshConnectionException($"Failed to connect after {maxRetries} attempts: {lastException?.Message}", lastException!);
    }

    private async Task ConnectWithRetryAsync(ScpClient client, CancellationToken cancellationToken, int maxRetries = 3)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                client.Connect();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxRetries)
                    await Task.Delay(attempt * 1000, cancellationToken);
            }
        }

        throw new SshConnectionException($"Failed to connect after {maxRetries} attempts: {lastException?.Message}", lastException!);
    }

    private static bool ShouldIgnore(string filePath, string basePath)
    {
        var relativePath = Path.GetRelativePath(basePath, filePath);

        // Normalize to forward slashes for consistent matching
        relativePath = relativePath.Replace('\\', '/');

        // Common patterns to ignore
        var ignorePatterns = new[]
        {
            "bin/", "obj/", ".git/", ".vs/", "node_modules/",
            ".dll", ".pdb", ".exe", ".cache"
        };

        return ignorePatterns.Any(p => relativePath.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // No persistent connections to dispose
    }
}
