using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FreeQEMU.Internal;

/// <summary>
/// Manages QEMU process lifecycle including starting, stopping, and monitor commands.
/// </summary>
internal sealed class QemuProcessManager : IQemuProcessManager
{
    private readonly VmConfiguration _config;
    private Process? _process;
    private bool _disposed;

    public QemuProcessManager(VmConfiguration config)
    {
        _config = config;
    }

    public bool IsRunning => _process != null && !_process.HasExited;

    private void Log(string message)
    {
        if (!_config.QuietMode)
            Console.WriteLine(message);
    }

    /// <summary>
    /// Kills any orphaned QEMU processes to release file locks.
    /// Call this before any file operations on the disk image.
    /// </summary>
    public async Task KillOrphanedProcessesAsync()
    {
        await KillAllQemuProcessesAsync();
    }


    public async Task EnsureBaseImageAsync(IProgress<VmSetupProgress>? progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_config.ImagesDirectory);

        // Diagnostic dump at startup (only in verbose mode)
        if (!_config.QuietMode)
        {
            Console.WriteLine();
            Console.WriteLine("  === FreeQEMU Disk Diagnostics ===");
            Console.WriteLine($"  Images Directory: {_config.ImagesDirectory}");
            Console.WriteLine($"  Base Image Path:  {_config.BaseImagePath}");
            Console.WriteLine($"  QEMU Directory:   {_config.QemuDirectory}");
            
            if (Directory.Exists(_config.ImagesDirectory))
            {
                var files = Directory.GetFiles(_config.ImagesDirectory);
                Console.WriteLine($"  Files in images directory ({files.Length}):");
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    Console.WriteLine($"    - {info.Name} ({info.Length / 1024 / 1024} MB)");
                }
            }

            // Check for qemu-img and show snapshot info
            var qemuImgPath = Path.Combine(_config.QemuDirectory, "qemu-img.exe");
            Console.WriteLine($"  qemu-img exists: {File.Exists(qemuImgPath)}");

            if (File.Exists(_config.BaseImagePath))
            {
                Console.WriteLine($"  Base image exists: YES ({new FileInfo(_config.BaseImagePath).Length / 1024 / 1024} MB)");
                
                var snapshotManager = new SnapshotManager(_config);
                var snapshotList = await snapshotManager.ListAsync(cancellationToken);
                Console.WriteLine($"  Snapshots found: {snapshotList.Count}");
                foreach (var snap in snapshotList)
                    Console.WriteLine($"    - {snap.Name} (ID: {snap.Id}, Size: {snap.VmSize})");
            }
            else
            {
                Console.WriteLine("  Base image exists: NO");
            }
            Console.WriteLine("  === End Diagnostics ===");
            Console.WriteLine();
        }

        // If we already have the image file, check if it's been prepared (resized or has snapshots)
        // Resizing and snapshots both modify the qcow2 file, changing its checksum
        if (File.Exists(_config.BaseImagePath))
        {
            var fileInfo = new FileInfo(_config.BaseImagePath);
            var fileSizeMb = fileInfo.Length / 1024 / 1024;
            
            // Original Debian cloud image is ~350MB. If it's larger, it's been resized.
            // Also check for snapshots.
            var snapshotManager = new SnapshotManager(_config);
            var snapshots = await snapshotManager.ListAsync(cancellationToken);
            
            if (snapshots.Count > 0 || fileSizeMb > 400)
            {
                var reason = snapshots.Count > 0 
                    ? $"{snapshots.Count} snapshot(s)" 
                    : "already resized";
                progress?.Report(new VmSetupProgress(VmSetupStage.VerifyingImage, 
                    $"Using cached image ({reason})"));
                return; // Image has been prepared, skip checksum
            }
        }

        // 1. Fetch expected hash
        progress?.Report(new VmSetupProgress(VmSetupStage.VerifyingImage, "Fetching checksum..."));
        string expectedHash;
        try
        {
            expectedHash = await FetchExpectedHashAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new VmSetupException($"Failed to fetch checksum: {ex.Message}. Cannot verify image integrity.", ex);
        }

        // 2. Check if we have a valid cached image
        if (File.Exists(_config.BaseImagePath))
        {
            progress?.Report(new VmSetupProgress(VmSetupStage.VerifyingImage, "Verifying cached image..."));
            var actualHash = await ComputeFileHashAsync(_config.BaseImagePath, progress, cancellationToken);

            Log($"  [Hash] Expected: {expectedHash}");
            Log($"  [Hash] Actual:   {actualHash}");
            Log($"  [Hash] Match:    {string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase)}");

            if (string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report(new VmSetupProgress(VmSetupStage.VerifyingImage, "Checksum verified ✓ (using cache)"));
                            return; // Cache is valid!
                        }

                        // Cache is invalid — delete and re-download
                        progress?.Report(new VmSetupProgress(VmSetupStage.VerifyingImage, "Checksum mismatch — re-downloading..."));
                        File.Delete(_config.BaseImagePath);
                    }

                    // 3. Download fresh image
                    await DownloadImageAsync(progress, cancellationToken);

                    // 4. Verify downloaded image BEFORE resizing (resize changes the hash)
                    progress?.Report(new VmSetupProgress(VmSetupStage.VerifyingImage, "Verifying downloaded image..."));
                    var downloadedHash = await ComputeFileHashAsync(_config.BaseImagePath, progress, cancellationToken);

                    if (!string.Equals(downloadedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(_config.BaseImagePath);
                        throw new VmSetupException(
                            $"Downloaded image failed checksum verification.\n" +
                            $"Expected: {expectedHash}\n" +
                            $"Actual:   {downloadedHash}");
                    }

                    progress?.Report(new VmSetupProgress(VmSetupStage.VerifyingImage, "Checksum verified ✓"));

                    // 5. Resize the disk image to configured size (after verification)
                    await ResizeDiskImageAsync(progress, cancellationToken);
                }

    /// <summary>
    /// Fetches the expected SHA512 hash for the base image from Debian's servers.
    /// </summary>
    private async Task<string> FetchExpectedHashAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var checksumContent = await httpClient.GetStringAsync(_config.ChecksumUrl, cancellationToken);
        var imageFileName = Path.GetFileName(_config.BaseImageUrl);

        // Parse SHA512SUMS format: "<hash>  <filename>"
        foreach (var line in checksumContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Contains(imageFileName))
            {
                // Hash is the first part before whitespace
                var hash = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                return hash;
            }
        }

        throw new VmSetupException($"Checksum not found for {imageFileName} in SHA512SUMS");
    }

    /// <summary>
    /// Computes the SHA512 hash of a file with progress reporting.
    /// </summary>
    private async Task<string> ComputeFileHashAsync(
        string filePath,
        IProgress<VmSetupProgress>? progress,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha512 = SHA512.Create();

        var buffer = new byte[81920];
        long totalRead = 0;
        long fileSize = stream.Length;
        int lastReportedPercent = -1;

        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            sha512.TransformBlock(buffer, 0, read, null, 0);
            totalRead += read;

            var percent = (int)((double)totalRead / fileSize * 100);
            // Only report every 5%
            if (percent >= lastReportedPercent + 5)
            {
                lastReportedPercent = percent / 5 * 5; // Round down to nearest 5
                progress?.Report(new VmSetupProgress(VmSetupStage.VerifyingImage,
                    $"Verifying checksum... {lastReportedPercent}%") { ProgressPercent = lastReportedPercent });
            }
        }

        sha512.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha512.Hash!).ToLowerInvariant();
    }

    /// <summary>
    /// Downloads the base image with progress reporting.
    /// </summary>
    private async Task DownloadImageAsync(IProgress<VmSetupProgress>? progress, CancellationToken cancellationToken)
    {
        progress?.Report(new VmSetupProgress(VmSetupStage.DownloadingBaseImage, "Downloading Debian base image..."));

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(30);

        using var response = await httpClient.GetAsync(_config.BaseImageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var buffer = new byte[81920];
            long bytesRead = 0;
            int lastReportedPercent = -1;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = File.Create(_config.BaseImagePath);

            int read;
            while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytesRead += read;

                if (totalBytes > 0)
                {
                    var percent = (int)((double)bytesRead / totalBytes * 100);
                    // Only report every 5%
                    if (percent >= lastReportedPercent + 5)
                    {
                        lastReportedPercent = percent / 5 * 5; // Round down to nearest 5
                        progress?.Report(new VmSetupProgress(VmSetupStage.DownloadingBaseImage,
                            $"Downloading... {lastReportedPercent}%") { ProgressPercent = lastReportedPercent });
                    }
                }
            }
        }

        /// <summary>
        /// Resizes the disk image to the configured size using qemu-img.
        /// </summary>
        private async Task ResizeDiskImageAsync(IProgress<VmSetupProgress>? progress, CancellationToken cancellationToken)
        {
            var qemuImgPath = Path.Combine(_config.QemuDirectory, "qemu-img.exe");
            if (!File.Exists(qemuImgPath))
                throw new VmSetupException($"qemu-img not found at: {qemuImgPath}");

            progress?.Report(new VmSetupProgress(VmSetupStage.DownloadingBaseImage, 
                $"Resizing disk to {_config.DiskSizeGb}GB..."));

            var psi = new ProcessStartInfo
            {
                FileName = qemuImgPath,
                Arguments = $"resize \"{_config.BaseImagePath}\" {_config.DiskSizeGb}G",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                throw new VmSetupException("Failed to start qemu-img process");

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                throw new VmSetupException($"Failed to resize disk image: {stderr}");
            }

            Log($"  [Disk] Resized to {_config.DiskSizeGb}GB");
        }

        public Task<bool> EnsureSshKeysAsync(CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_config.KeysDirectory);

            // Check if keys exist AND are valid
            if (File.Exists(_config.PrivateKeyPath) && File.Exists(_config.PublicKeyPath))
            {
                // Validate the public key format - it should start with "ssh-rsa AAAA"
                // Old broken keys just had raw base64 without proper OpenSSH structure
                var publicKey = File.ReadAllText(_config.PublicKeyPath);
                if (publicKey.StartsWith("ssh-rsa AAAA"))
                {
                    return Task.FromResult(false); // Keys look valid, no regeneration needed
            }
            
            // Keys are in wrong format - delete and regenerate
            Log("  [Keys] Detected invalid key format, regenerating...");
            File.Delete(_config.PrivateKeyPath);
            File.Delete(_config.PublicKeyPath);
        }

        Log("  [Keys] Generating new SSH key pair...");
        
        // Generate RSA key pair
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        
        // Export private key in PEM format (PKCS#1)
        var privateKeyPem = new StringBuilder();
        privateKeyPem.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
        privateKeyPem.AppendLine(Convert.ToBase64String(rsa.ExportRSAPrivateKey(), Base64FormattingOptions.InsertLineBreaks));
        privateKeyPem.AppendLine("-----END RSA PRIVATE KEY-----");
        File.WriteAllText(_config.PrivateKeyPath, privateKeyPem.ToString());

        // Export public key in OpenSSH format
        var publicKeyOpenSsh = FormatOpenSshPublicKey(rsa);
        File.WriteAllText(_config.PublicKeyPath, publicKeyOpenSsh);
        
        Log("  [Keys] SSH keys generated successfully");

        return Task.FromResult(true); // Keys were regenerated
    }

    public async Task StartAsync(string? loadSnapshot, CancellationToken cancellationToken)
    {
        if (IsRunning)
            throw new QemuProcessException("QEMU is already running");

        // Always kill any orphaned QEMU processes to avoid file lock issues
        await KillAllQemuProcessesAsync();

        var qemuPath = Path.Combine(_config.QemuDirectory, "qemu-system-x86_64.exe");
        if (!File.Exists(qemuPath))
            throw new QemuProcessException($"QEMU not found at: {qemuPath}");

        // Create work directory for this run
        var runId = Guid.NewGuid().ToString("N")[..8];
        var workDir = Path.Combine(_config.LogsDirectory, $"QemuRun_{runId}");
        Directory.CreateDirectory(workDir);

        // Create cloud-init seed ISO
        var seedIsoPath = Path.Combine(workDir, "seed.iso");
        var publicKey = await File.ReadAllTextAsync(_config.PublicKeyPath, cancellationToken);
        CreateSeedIso(seedIsoPath, publicKey.Trim());

        var logFile = Path.Combine(workDir, "qemu-serial.log");

        // Build QEMU arguments
        // Note: Don't use -snapshot with -loadvm (incompatible)
        // Note: Windows QEMU doesn't support -daemonize, so we run in foreground
        // Note: seed.iso MUST be readonly=on, otherwise savevm fails with
        // "Device 'virtioX' is writable but does not support snapshots"
        var loadVmArg = !string.IsNullOrEmpty(loadSnapshot) ? $"-loadvm {loadSnapshot} " : "";

        Log($"  [VM] Starting on SSH port {_config.ActualSshPort}, QMP port {_config.ActualQmpPort}");
        Log($"  [VM] Image: {_config.BaseImageFileName}");

        var args = $"-m {_config.MemoryMb} " +
                   $"-smp {_config.CpuCount} " +
                   $"-machine q35 " +
                   $"-cpu qemu64 " +
                   $"-drive file=\"{_config.BaseImagePath}\",format=qcow2,if=virtio " +
                   $"-drive file=\"{seedIsoPath}\",format=raw,if=virtio,readonly=on " +
                   $"-netdev user,id=net0,hostfwd=tcp:127.0.0.1:{_config.ActualSshPort}-:22 " +
                   $"-device virtio-net-pci,netdev=net0 " +
                   $"-serial file:\"{logFile}\" " +
                   $"-display none " +
                   $"-qmp tcp:127.0.0.1:{_config.ActualQmpPort},server,nowait " +
                   loadVmArg;

        var psi = new ProcessStartInfo
        {
            FileName = qemuPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(qemuPath)
        };

        _process = new Process { StartInfo = psi };
        
        try
        {
            _process.Start();
        }
        catch (Exception ex)
        {
            throw new QemuProcessException($"Failed to start QEMU: {ex.Message}", ex);
        }

        // Wait a moment for QEMU to initialize
        await Task.Delay(2000, cancellationToken);

        if (_process.HasExited)
        {
            var stderr = await _process.StandardError.ReadToEndAsync(cancellationToken);
            throw new QemuProcessException($"QEMU exited immediately: {stderr}");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!IsRunning) return;

        Log("  [VM] Stopping VM...");

        try
        {
            // Try graceful shutdown via QMP
            Log("  [VM] Sending system_powerdown...");
            await SendMonitorCommandAsync("system_powerdown", cancellationToken);
            
            // Wait for graceful shutdown (up to 30 seconds)
            Log("  [VM] Waiting for graceful shutdown...");
            for (int i = 0; i < 300 && IsRunning; i++)
            {
                await Task.Delay(100, cancellationToken);
            }
            
            if (!IsRunning)
            {
                Log("  [VM] Graceful shutdown complete");
                return;
            }
        }
        catch (Exception ex)
        {
            Log($"  [VM] Graceful shutdown failed: {ex.Message}");
        }

        // Force kill if still running
        if (IsRunning)
        {
            Log("  [VM] Force killing QEMU process...");
            try
            {
                _process!.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync(cancellationToken);
                Log("  [VM] QEMU process killed");
            }
            catch (Exception ex)
            {
                Log($"  [VM] Kill failed: {ex.Message}");
            }
        }

        _process = null;
    }

    public async Task<string?> SendMonitorCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", _config.ActualQmpPort, cancellationToken);
            
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            await using var writer = new StreamWriter(stream) { AutoFlush = true };

            // Read QMP greeting
            await reader.ReadLineAsync(cancellationToken);

            // Enable QMP command mode
            await writer.WriteLineAsync("{\"execute\": \"qmp_capabilities\"}");
            await ReadQmpResponse(reader, cancellationToken); // Skip response

            // Send command
            var qmpCommand = command switch
            {
                "system_powerdown" => "{\"execute\": \"system_powerdown\"}",
                var s when s.StartsWith("savevm ") => 
                    $"{{\"execute\": \"human-monitor-command\", \"arguments\": {{\"command-line\": \"{command}\"}}}}",
                var s when s.StartsWith("loadvm ") => 
                    $"{{\"execute\": \"human-monitor-command\", \"arguments\": {{\"command-line\": \"{command}\"}}}}",
                "info snapshots" => 
                    "{\"execute\": \"human-monitor-command\", \"arguments\": {\"command-line\": \"info snapshots\"}}",
                _ => $"{{\"execute\": \"human-monitor-command\", \"arguments\": {{\"command-line\": \"{command}\"}}}}"
            };

            await writer.WriteLineAsync(qmpCommand);
            return await ReadQmpResponse(reader, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new QemuProcessException($"Failed to send monitor command: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads a QMP response, skipping any asynchronous events.
    /// </summary>
    private static async Task<string?> ReadQmpResponse(StreamReader reader, CancellationToken cancellationToken)
    {
        // QMP can send async events at any time. We need to skip them and find our response.
        // Events have "event" property, responses have "return" or "error" property.
        for (int i = 0; i < 10; i++) // Max 10 attempts to find a response
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                
                // Skip events (they have "event" property)
                if (doc.RootElement.TryGetProperty("event", out _))
                {
                    continue; // Skip this event, read next line
                }
                
                // Found a response - extract the return value
                if (doc.RootElement.TryGetProperty("return", out var returnValue))
                {
                    return returnValue.ValueKind == JsonValueKind.String 
                        ? returnValue.GetString() 
                        : returnValue.ToString();
                }
                
                // Error response
                if (doc.RootElement.TryGetProperty("error", out var errorValue))
                {
                    return $"Error: {errorValue}";
                }
                
                // Unknown response format
                return line;
            }
            catch
            {
                return line;
            }
        }

        return null;
    }

    private void CreateSeedIso(string outputPath, string sshPublicKey)
    {
        // Minimal cloud-init ISO using DiscUtils
        // For now, create a simple ISO structure
        var metaData = "instance-id: iid-local01\nlocal-hostname: freeqemu-vm\n";
        
        var userData = $@"#cloud-config
ssh_pwauth: false
disable_root: false

bootcmd:
  - test -f /etc/ssh/ssh_host_rsa_key || ssh-keygen -t rsa -f /etc/ssh/ssh_host_rsa_key -N '' -q
  - test -f /etc/ssh/ssh_host_ecdsa_key || ssh-keygen -t ecdsa -f /etc/ssh/ssh_host_ecdsa_key -N '' -q
  - test -f /etc/ssh/ssh_host_ed25519_key || ssh-keygen -t ed25519 -f /etc/ssh/ssh_host_ed25519_key -N '' -q

users:
  - name: root
    lock_passwd: true
    ssh_authorized_keys:
      - {sshPublicKey}

runcmd:
  - mkdir -p /root/work
";

        // Use DiscUtils to create ISO (requires NuGet reference)
        var builder = new DiscUtils.Iso9660.CDBuilder
        {
            UseJoliet = true,
            VolumeIdentifier = "cidata"
        };
        builder.AddFile("meta-data", Encoding.UTF8.GetBytes(metaData));
        builder.AddFile("user-data", Encoding.UTF8.GetBytes(userData));
        
        using var fs = File.Create(outputPath);
        builder.Build(fs);
    }

    private static string FormatOpenSshPublicKey(System.Security.Cryptography.RSA rsa)
    {
        // Format RSA public key in OpenSSH format: ssh-rsa <base64-data> <comment>
        // OpenSSH format is: string "ssh-rsa" | mpint e | mpint n
        var parameters = rsa.ExportParameters(includePrivateParameters: false);
        
        using var ms = new MemoryStream();
        
        // Write key type
        WriteOpenSshString(ms, "ssh-rsa");
        
        // Write exponent (e) as mpint
        WriteOpenSshMpint(ms, parameters.Exponent!);
        
        // Write modulus (n) as mpint
        WriteOpenSshMpint(ms, parameters.Modulus!);
        
        var base64 = Convert.ToBase64String(ms.ToArray());
        return $"ssh-rsa {base64} freeqemu-generated";
    }

    private static void WriteOpenSshString(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        WriteOpenSshLength(stream, bytes.Length);
        stream.Write(bytes);
    }

    private static void WriteOpenSshMpint(Stream stream, byte[] value)
    {
        // If the high bit is set, we need to prepend a zero byte
        if (value.Length > 0 && (value[0] & 0x80) != 0)
        {
            WriteOpenSshLength(stream, value.Length + 1);
            stream.WriteByte(0);
            stream.Write(value);
        }
        else
        {
            WriteOpenSshLength(stream, value.Length);
            stream.Write(value);
        }
    }

    private static void WriteOpenSshLength(Stream stream, int length)
    {
        // OpenSSH uses big-endian 32-bit length prefix
        stream.WriteByte((byte)(length >> 24));
        stream.WriteByte((byte)(length >> 16));
        stream.WriteByte((byte)(length >> 8));
        stream.WriteByte((byte)length);
    }

    /// <summary>
    /// Kills ALL running QEMU processes. Use with caution - affects all VMs!
    /// </summary>
    private async Task KillAllQemuProcessesAsync()
    {
        try
        {
            var qemuProcesses = Process.GetProcessesByName("qemu-system-x86_64");
            if (qemuProcesses.Length > 0)
            {
                Log($"  [VM] Killing {qemuProcesses.Length} QEMU process(es)...");
                foreach (var proc in qemuProcesses)
                {
                    try
                    {
                        Log($"  [VM] Killing QEMU process {proc.Id}...");
                        proc.Kill(entireProcessTree: true);
                        await proc.WaitForExitAsync();
                        Log($"  [VM] Killed QEMU process {proc.Id}");
                    }
                    catch (Exception ex)
                    {
                        Log($"  [VM] Failed to kill process {proc.Id}: {ex.Message}");
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }
                
                // Wait for ports to be released
                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            Log($"  [VM] Error killing QEMU processes: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsRunning)
        {
            try
            {
                _process?.Kill(entireProcessTree: true);
            }
            catch { }
        }

        _process?.Dispose();
    }
}
