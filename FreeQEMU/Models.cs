namespace FreeQEMU;

/// <summary>
/// Result of a command execution in the VM.
/// </summary>
public sealed class CommandResult
{
    /// <summary>
    /// Whether the command completed successfully (exit code 0).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The command's exit code.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Standard output from the command.
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Standard error output from the command.
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Time taken to execute the command.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static CommandResult Succeeded(string output, TimeSpan duration) => new()
    {
        Success = true,
        ExitCode = 0,
        Output = output,
        Duration = duration
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static CommandResult Failed(int exitCode, string output, string error, TimeSpan duration) => new()
    {
        Success = false,
        ExitCode = exitCode,
        Output = output,
        Error = error,
        Duration = duration
    };

    /// <summary>
    /// Throws if the command failed.
    /// </summary>
    public CommandResult ThrowIfFailed()
    {
        if (!Success)
            throw new CommandExecutionException(this);
        return this;
    }
}

/// <summary>
/// Progress information for file transfers.
/// </summary>
public sealed class FileTransferProgress
{
    /// <summary>
    /// Current file being transferred.
    /// </summary>
    public string CurrentFile { get; init; } = string.Empty;

    /// <summary>
    /// Number of files transferred so far.
    /// </summary>
    public int FilesTransferred { get; init; }

    /// <summary>
    /// Total number of files to transfer.
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// Bytes transferred so far.
    /// </summary>
    public long BytesTransferred { get; init; }

    /// <summary>
    /// Total bytes to transfer.
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public double ProgressPercent => TotalFiles > 0 ? (double)FilesTransferred / TotalFiles * 100 : 0;
}

/// <summary>
/// Stages of VM setup process.
/// </summary>
public enum VmSetupStage
{
    /// <summary>Downloading base VM image</summary>
    DownloadingBaseImage,
    
    /// <summary>Verifying image checksum</summary>
    VerifyingImage,
    
    /// <summary>Generating SSH keys</summary>
    GeneratingKeys,
    
    /// <summary>Checking if snapshot exists</summary>
    CheckingSnapshot,
    
    /// <summary>Creating golden snapshot</summary>
    CreatingSnapshot,
    
    /// <summary>Installing tools in VM</summary>
    InstallingTools,
    
    /// <summary>Saving snapshot</summary>
    SavingSnapshot,
    
    /// <summary>Starting VM</summary>
    StartingVm,
    
    /// <summary>Waiting for SSH connection</summary>
    WaitingForSsh,
    
    /// <summary>VM is ready</summary>
    Ready
}

/// <summary>
/// Progress information for VM setup.
/// </summary>
public sealed class VmSetupProgress
{
    /// <summary>
    /// Current stage of setup.
    /// </summary>
    public VmSetupStage Stage { get; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Optional progress percentage (0-100) for long operations.
    /// </summary>
    public double? ProgressPercent { get; init; }

    public VmSetupProgress(VmSetupStage stage, string message)
    {
        Stage = stage;
        Message = message;
    }
}

/// <summary>
/// Information about a saved snapshot.
/// </summary>
public sealed class SnapshotInfo
{
    /// <summary>
    /// Snapshot name/tag.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Snapshot ID.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// VM memory size at snapshot time.
    /// </summary>
    public string VmSize { get; init; } = string.Empty;

    /// <summary>
    /// When the snapshot was created.
    /// </summary>
    public DateTime? CreatedAt { get; init; }
}
