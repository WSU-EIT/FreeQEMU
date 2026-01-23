namespace FreeQEMU;

/// <summary>
/// Manages QEMU process lifecycle.
/// </summary>
internal interface IQemuProcessManager : IDisposable
{
    /// <summary>
    /// Ensures the base VM image exists, downloading if necessary.
    /// </summary>
    Task EnsureBaseImageAsync(IProgress<VmSetupProgress>? progress, CancellationToken cancellationToken);

    /// <summary>
    /// Ensures SSH keys exist, generating if necessary.
    /// Returns true if keys were regenerated (requires snapshot rebuild).
    /// </summary>
    Task<bool> EnsureSshKeysAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts the QEMU process.
    /// </summary>
    /// <param name="loadSnapshot">Optional snapshot name to load on boot</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartAsync(string? loadSnapshot, CancellationToken cancellationToken);

    /// <summary>
    /// Stops the QEMU process gracefully.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends a command to the QEMU monitor.
    /// </summary>
    Task<string?> SendMonitorCommandAsync(string command, CancellationToken cancellationToken);

    /// <summary>
    /// Gets whether QEMU is currently running.
    /// </summary>
    bool IsRunning { get; }
}

/// <summary>
/// Manages SSH connections and command execution.
/// </summary>
internal interface ISshConnectionManager : IDisposable
{
    /// <summary>
    /// Waits for SSH to become available.
    /// </summary>
    Task WaitForConnectionAsync(int timeoutSeconds, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a command over SSH.
    /// </summary>
    Task<CommandResult> ExecuteCommandAsync(string command, int timeoutSeconds, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a command with output streaming.
    /// </summary>
    Task<CommandResult> ExecuteCommandAsync(
        string command,
        Action<string> onOutput,
        Action<string>? onError,
        int timeoutSeconds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Uploads a folder to the VM.
    /// </summary>
    Task UploadFolderAsync(
        string localPath,
        string remotePath,
        IProgress<FileTransferProgress>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a folder from the VM.
    /// </summary>
    Task DownloadFolderAsync(
        string remotePath,
        string localPath,
        IProgress<FileTransferProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>
/// Manages VM snapshots.
/// </summary>
internal interface ISnapshotManager
{
    /// <summary>
    /// Checks if a snapshot exists.
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all available snapshots.
    /// </summary>
    Task<IReadOnlyList<SnapshotInfo>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the current VM state as a snapshot.
    /// </summary>
    Task SaveAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Restores a snapshot.
    /// </summary>
    Task RestoreAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a snapshot.
    /// </summary>
    Task DeleteAsync(string name, CancellationToken cancellationToken);
}
