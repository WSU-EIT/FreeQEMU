namespace FreeQEMU;

/// <summary>
/// Configuration for a Linux VM instance.
/// </summary>
public sealed class VmConfiguration
{
    /// <summary>
    /// Memory allocated to the VM in megabytes.
    /// </summary>
    public int MemoryMb { get; set; } = 2048;

    /// <summary>
    /// Number of virtual CPUs.
    /// </summary>
    public int CpuCount { get; set; } = 2;

    /// <summary>
    /// SSH port for connecting to the VM. If 0, a unique port will be auto-assigned.
    /// </summary>
    public int SshPort { get; set; } = 0;

    /// <summary>
    /// QMP monitor port for QEMU control. If 0, derived from SshPort.
    /// </summary>
    public int QmpPort { get; set; } = 0;

    /// <summary>
    /// Timeout in seconds for VM boot.
    /// </summary>
    public int BootTimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// The preset used for this configuration.
    /// </summary>
    public VmPreset Preset { get; set; } = VmPreset.Stock;

    /// <summary>
    /// Name of the snapshot to use/create. Derived from preset if not set.
    /// </summary>
    public string? SnapshotName { get; set; }

    /// <summary>
    /// Custom setup commands to run when creating a snapshot.
    /// Added after preset commands.
    /// </summary>
    public string? CustomSetupCommands { get; set; }

    /// <summary>
    /// Base image URL. Default: Debian 12 cloud image.
    /// </summary>
    public string BaseImageUrl { get; set; } = 
        "https://cloud.debian.org/images/cloud/bookworm/latest/debian-12-generic-amd64.qcow2";

    /// <summary>
    /// URL to fetch SHA512 checksums for verifying the base image.
    /// Derived from BaseImageUrl by replacing filename with SHA512SUMS.
    /// </summary>
    public string ChecksumUrl => 
        BaseImageUrl.Replace(Path.GetFileName(BaseImageUrl), "SHA512SUMS");

        /// <summary>
        /// Working directory for VM files. Default: AppContext.BaseDirectory.
        /// </summary>
        public string WorkingDirectory { get; set; } = AppContext.BaseDirectory;

        /// <summary>
        /// Disk size for the VM in gigabytes. The Debian cloud image will be resized to this size.
        /// Default: 10 (10GB). Increase if you need more space for Docker images or large projects.
        /// </summary>
        public int DiskSizeGb { get; set; } = 10;

        /// <summary>
        /// SSH username for VM connections.
        /// </summary>
        public string SshUsername { get; set; } = "root";

        /// <summary>
        /// If true, kills ALL running QEMU processes before starting this VM.
        /// Useful for development/testing to clean up orphaned processes.
        /// Default: true
        /// </summary>
        public bool KillAllQemuOnStart { get; set; } = true;

        /// <summary>
        /// If true, suppresses verbose console output from internal operations.
    /// Default: false
    /// </summary>
    public bool QuietMode { get; set; } = false;

    // Port assignment tracking
    private static int _nextPort = 2222;
    private static readonly object _portLock = new();
    private int _assignedSshPort;
    private int _assignedQmpPort;

    /// <summary>
    /// Gets the actual SSH port (auto-assigned if SshPort was 0).
    /// </summary>
    internal int ActualSshPort
    {
        get
        {
            EnsurePortsAssigned();
            return _assignedSshPort;
        }
    }

    /// <summary>
    /// Gets the actual QMP port (auto-assigned if QmpPort was 0).
    /// </summary>
    internal int ActualQmpPort
    {
        get
        {
            EnsurePortsAssigned();
            return _assignedQmpPort;
        }
    }

    private void EnsurePortsAssigned()
    {
        if (_assignedSshPort == 0) {
            lock (_portLock) {
                if (_assignedSshPort == 0) {
                    _assignedSshPort = SshPort != 0 ? SshPort : _nextPort++;
                    _assignedQmpPort = QmpPort != 0 ? QmpPort : _assignedSshPort + 2000;
                }
            }
        }
    }

    /// <summary>
    /// Base image filename - derived from port to ensure each VM has its own image.
    /// </summary>
    internal string BaseImageFileName => $"debian12-vm-{ActualSshPort}.qcow2";

    /// <summary>
    /// Creates a configuration from a preset.
    /// </summary>
    public static VmConfiguration FromPreset(VmPreset preset)
    {
        VmConfiguration config = new() { Preset = preset };
        
        config.SnapshotName = preset switch
        {
            VmPreset.Stock => null,
            VmPreset.DotNet8 => "freeqemu-dotnet8",
            VmPreset.DotNet9 => "freeqemu-dotnet9",
            VmPreset.DotNet10 => "freeqemu-dotnet10",
            VmPreset.Docker => "freeqemu-docker",
            VmPreset.DockerDotNet9 => "freeqemu-docker-dotnet9",
            VmPreset.DockerDotNet10 => "freeqemu-docker-dotnet10",
            VmPreset.Full => "freeqemu-full",
            _ => null
        };

        return config;
    }

    /// <summary>
    /// Gets the setup commands for the configured preset.
    /// </summary>
    public string? GetSetupCommands()
    {
        string? presetCommands = Preset switch
        {
            VmPreset.Stock => null,
            VmPreset.DotNet8 => SetupCommands.DotNet8,
            VmPreset.DotNet9 => SetupCommands.DotNet9,
            VmPreset.DotNet10 => SetupCommands.DotNet10,
            VmPreset.Docker => SetupCommands.Docker,
            VmPreset.DockerDotNet9 => SetupCommands.DockerDotNet9,
            VmPreset.DockerDotNet10 => SetupCommands.DockerDotNet10,
            VmPreset.Full => SetupCommands.Full,
            _ => null
        };

        if (string.IsNullOrEmpty(CustomSetupCommands))
            return presetCommands;

        if (string.IsNullOrEmpty(presetCommands))
            return CustomSetupCommands;

        return $"{presetCommands} && {CustomSetupCommands}";
    }

    // === Derived paths ===
    
    internal string ImagesDirectory => Path.Combine(WorkingDirectory, "images");
    internal string KeysDirectory => Path.Combine(WorkingDirectory, "keys");
    internal string LogsDirectory => Path.Combine(WorkingDirectory, "logs");
    internal string QemuDirectory => Path.Combine(WorkingDirectory, "Tools", "qemu");
    
    internal string BaseImagePath => Path.Combine(ImagesDirectory, BaseImageFileName);
    internal string PrivateKeyPath => Path.Combine(KeysDirectory, "id_rsa");
    internal string PublicKeyPath => Path.Combine(KeysDirectory, "id_rsa.pub");
}
