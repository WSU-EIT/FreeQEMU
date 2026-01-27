using FreeQEMU.Internal;

namespace FreeQEMU;

/// <summary>
/// High-level API for running commands in a Linux VM.
/// Automatically handles VM provisioning, snapshots, and lifecycle.
/// </summary>
/// <example>
/// // Quick usage with preset
/// using var vm = new LinuxVm(VmPreset.DotNet10);
/// await vm.EnsureReadyAsync();
/// var result = await vm.ExecuteAsync("dotnet --version");
/// Console.WriteLine(result.Output);
/// 
/// // Custom configuration
/// using var vm = LinuxVm.Create()
///     .WithPreset(VmPreset.Stock)
///     .WithMemory(4096)
///     .WithCpus(4)
///     .WithSnapshot("my-custom-snapshot")
///     .Build();
/// </example>
public sealed class LinuxVm : IDisposable, IAsyncDisposable
{
    private readonly VmConfiguration _config;
    private readonly QemuProcessManager _processManager;
    private readonly SshConnectionManager _sshManager;
    private readonly SnapshotManager _snapshotManager;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Creates a new Linux VM with the specified preset configuration.
    /// </summary>
    /// <param name="preset">The VM preset (Stock, DotNet8, DotNet9, DotNet10, Docker, Full)</param>
    public LinuxVm(VmPreset preset = VmPreset.Stock)
        : this(VmConfiguration.FromPreset(preset))
    {
    }

    /// <summary>
    /// Creates a new Linux VM with custom configuration.
    /// </summary>
    public LinuxVm(VmConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _processManager = new QemuProcessManager(config);
        _sshManager = new SshConnectionManager(config);
        _snapshotManager = new SnapshotManager(config, _processManager);
    }

    /// <summary>
    /// Gets whether the VM is currently running.
    /// </summary>
    public bool IsRunning => _isRunning && !_disposed;

    /// <summary>
    /// Gets the current VM configuration.
    /// </summary>
    public VmConfiguration Configuration => _config;

    /// <summary>
    /// Creates a new LinuxVm builder for fluent configuration.
    /// </summary>
    public static LinuxVmBuilder Create() => new();

    /// <summary>
    /// Ensures the VM is ready to execute commands.
    /// Downloads base image if needed, creates golden snapshot if missing, and starts the VM.
    /// </summary>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task EnsureReadyAsync(
        IProgress<VmSetupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Kill any orphaned QEMU processes first to avoid file lock issues
        await _processManager.KillOrphanedProcessesAsync();

        // Step 1: Ensure base image exists
        progress?.Report(new VmSetupProgress(VmSetupStage.DownloadingBaseImage, "Checking base image..."));
        await _processManager.EnsureBaseImageAsync(progress, cancellationToken);

        // Step 2: Ensure SSH keys exist (and are valid format)
        progress?.Report(new VmSetupProgress(VmSetupStage.GeneratingKeys, "Checking SSH keys..."));
        var keysRegenerated = await _processManager.EnsureSshKeysAsync(cancellationToken);

        // Step 3: Check/create golden snapshot for preset
        var snapshotName = _config.SnapshotName;
        if (!string.IsNullOrEmpty(snapshotName))
        {
            progress?.Report(new VmSetupProgress(VmSetupStage.CheckingSnapshot, $"Checking snapshot '{snapshotName}'..."));
            
            var snapshotExists = await _snapshotManager.ExistsAsync(snapshotName, cancellationToken);
            
            // If keys were regenerated, existing snapshot has wrong keys baked in
            if (snapshotExists && keysRegenerated)
            {
                Console.WriteLine($"  [Snapshot] Keys changed, deleting old snapshot '{snapshotName}'...");
                await _snapshotManager.DeleteAsync(snapshotName, cancellationToken);
                snapshotExists = false;
            }
            
            if (!snapshotExists)
            {
                progress?.Report(new VmSetupProgress(VmSetupStage.CreatingSnapshot, $"Creating golden snapshot '{snapshotName}'..."));
                await CreateGoldenSnapshotAsync(snapshotName, progress, cancellationToken);
            }
        }

        // Step 4: Start VM (from snapshot if available)
        progress?.Report(new VmSetupProgress(VmSetupStage.StartingVm, "Starting VM..."));
        await StartAsync(cancellationToken);

        // Step 5: Wait for SSH
        progress?.Report(new VmSetupProgress(VmSetupStage.WaitingForSsh, "Waiting for SSH..."));
        await _sshManager.WaitForConnectionAsync(_config.BootTimeoutSeconds, cancellationToken);

        progress?.Report(new VmSetupProgress(VmSetupStage.Ready, "VM is ready!"));
    }

    /// <summary>
    /// Starts the VM without setup checks. Use EnsureReadyAsync for automatic setup.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_isRunning) return;

        await _processManager.StartAsync(_config.SnapshotName, cancellationToken);
        _isRunning = true;
    }

    /// <summary>
    /// Stops the VM gracefully.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;

        await _processManager.StopAsync(cancellationToken);
        _isRunning = false;
    }

    /// <summary>
    /// Executes a command in the VM and returns the result.
    /// </summary>
    /// <param name="command">The shell command to execute</param>
    /// <param name="timeoutSeconds">Command timeout in seconds (default: 300)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Command execution result</returns>
    public async Task<CommandResult> ExecuteAsync(
        string command,
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureRunning();

        return await _sshManager.ExecuteCommandAsync(command, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Executes a command with real-time output streaming.
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(
        string command,
        Action<string> onOutput,
        Action<string>? onError = null,
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureRunning();

        return await _sshManager.ExecuteCommandAsync(command, onOutput, onError, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Uploads a local folder to the VM.
    /// </summary>
    /// <param name="localPath">Local folder path</param>
    /// <param name="remotePath">Remote path in VM (default: /root/work)</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UploadFolderAsync(
        string localPath,
        string remotePath = "/root/work",
        IProgress<FileTransferProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureRunning();

        await _sshManager.UploadFolderAsync(localPath, remotePath, progress, cancellationToken);
    }

    /// <summary>
    /// Downloads a folder from the VM to local path.
    /// </summary>
    public async Task DownloadFolderAsync(
        string remotePath,
        string localPath,
        IProgress<FileTransferProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureRunning();

        await _sshManager.DownloadFolderAsync(remotePath, localPath, progress, cancellationToken);
    }

    /// <summary>
    /// Saves the current VM state as a snapshot for fast restoration.
    /// </summary>
    public async Task SaveSnapshotAsync(string name, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureRunning();

        // Sync filesystem first
        await _sshManager.ExecuteCommandAsync("sync", 30, cancellationToken);
        await Task.Delay(1000, cancellationToken);

        await _snapshotManager.SaveAsync(name, cancellationToken);
    }

    /// <summary>
    /// Restores the VM to a previously saved snapshot.
    /// </summary>
    public async Task RestoreSnapshotAsync(string name, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureRunning();

        await _snapshotManager.RestoreAsync(name, cancellationToken);
    }

    /// <summary>
    /// Lists all available snapshots.
    /// </summary>
    public async Task<IReadOnlyList<SnapshotInfo>> ListSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await _snapshotManager.ListAsync(cancellationToken);
    }

    private async Task CreateGoldenSnapshotAsync(
        string snapshotName,
        IProgress<VmSetupProgress>? progress,
        CancellationToken cancellationToken)
    {
        // Boot fresh VM (no snapshot)
        await _processManager.StartAsync(loadSnapshot: null, cancellationToken);
        _isRunning = true;

        // Start streaming boot log in background
        var bootLogTask = StreamBootLogAsync(progress, cancellationToken);

        try
        {
            // Wait for SSH
            progress?.Report(new VmSetupProgress(VmSetupStage.WaitingForSsh, "Waiting for VM to boot (watching serial log)..."));
            await _sshManager.WaitForConnectionAsync(_config.BootTimeoutSeconds, cancellationToken);

            // Run setup commands for the preset
            var setupCommands = _config.GetSetupCommands();
            if (!string.IsNullOrEmpty(setupCommands))
            {
                progress?.Report(new VmSetupProgress(VmSetupStage.InstallingTools, "Installing tools (this may take several minutes)..."));
                var result = await _sshManager.ExecuteCommandAsync(
                    setupCommands,
                    line => Console.WriteLine($"    {line}"),
                    line => Console.Error.WriteLine($"    ERR: {line}"),
                    900, // 15 min timeout
                    cancellationToken);
                
                if (!result.Success)
                {
                    throw new VmSetupException($"Setup commands failed: {result.Error}");
                }
            }

            // Sync and save snapshot
            progress?.Report(new VmSetupProgress(VmSetupStage.SavingSnapshot, $"Saving snapshot '{snapshotName}'..."));
            await _sshManager.ExecuteCommandAsync("sync", 30, cancellationToken);
            await Task.Delay(2000, cancellationToken);

            await _snapshotManager.SaveAsync(snapshotName, cancellationToken);

            // Stop VM (will be restarted from snapshot)
            await _processManager.StopAsync(cancellationToken);
            _isRunning = false;
        }
        catch
        {
            await _processManager.StopAsync(CancellationToken.None);
            _isRunning = false;
            throw;
        }
    }

    /// <summary>
    /// Streams the QEMU serial log (boot messages) to the console.
    /// </summary>
    private async Task StreamBootLogAsync(IProgress<VmSetupProgress>? progress, CancellationToken cancellationToken)
    {
        // Find the latest log directory
        var logsDir = _config.LogsDirectory;
        if (!Directory.Exists(logsDir))
            return;

        // Wait for log file to appear
        string? logFile = null;
        for (int i = 0; i < 30 && logFile == null; i++)
        {
            var runDirs = Directory.GetDirectories(logsDir, "QemuRun_*")
                .OrderByDescending(d => Directory.GetCreationTime(d))
                .ToArray();
            
            if (runDirs.Length > 0)
            {
                var candidateLog = Path.Combine(runDirs[0], "qemu-serial.log");
                if (File.Exists(candidateLog))
                    logFile = candidateLog;
            }
            
            if (logFile == null)
                await Task.Delay(500, cancellationToken);
        }

        if (logFile == null)
            return;

        // Stream the log file
        try
        {
            await using var stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line != null)
                {
                    Console.WriteLine($"  [BOOT] {line}");
                }
                else
                {
                    // No more data, wait a bit
                    await Task.Delay(100, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation requested
        }
        catch
        {
            // Ignore errors streaming log
        }
    }

    private void EnsureRunning()
    {
        if (!_isRunning)
            throw new InvalidOperationException("VM is not running. Call StartAsync or EnsureReadyAsync first.");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isRunning)
        {
            _processManager.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            _isRunning = false;
        }

        _sshManager.Dispose();
        _processManager.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isRunning)
        {
            await _processManager.StopAsync(CancellationToken.None);
            _isRunning = false;
        }

        _sshManager.Dispose();
        _processManager.Dispose();
    }
}
