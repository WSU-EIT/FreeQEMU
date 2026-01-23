namespace FreeQEMU;

/// <summary>
/// Base exception for FreeQEMU errors.
/// </summary>
public class FreeQemuException : Exception
{
    public FreeQemuException(string message) : base(message) { }
    public FreeQemuException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when VM setup fails.
/// </summary>
public class VmSetupException : FreeQemuException
{
    public VmSetupException(string message) : base(message) { }
    public VmSetupException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a command execution fails.
/// </summary>
public class CommandExecutionException : FreeQemuException
{
    public CommandResult Result { get; }

    public CommandExecutionException(CommandResult result)
        : base($"Command failed with exit code {result.ExitCode}: {result.Error}")
    {
        Result = result;
    }
}

/// <summary>
/// Thrown when SSH connection fails.
/// </summary>
public class SshConnectionException : FreeQemuException
{
    public SshConnectionException(string message) : base(message) { }
    public SshConnectionException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when QEMU process operations fail.
/// </summary>
public class QemuProcessException : FreeQemuException
{
    public QemuProcessException(string message) : base(message) { }
    public QemuProcessException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when snapshot operations fail.
/// </summary>
public class SnapshotException : FreeQemuException
{
    public SnapshotException(string message) : base(message) { }
    public SnapshotException(string message, Exception innerException) : base(message, innerException) { }
}
