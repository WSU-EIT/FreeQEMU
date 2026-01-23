namespace FreeQEMU;

/// <summary>
/// Fluent builder for creating LinuxVm instances with custom configuration.
/// </summary>
public sealed class LinuxVmBuilder
{
    private VmPreset _preset = VmPreset.Stock;
    private int _memoryMb = 2048;
    private int _cpuCount = 2;
    private int _sshPort = 2222;
    private string? _snapshotName;
    private string? _customSetupCommands;
    private string? _baseImageUrl;
    private string? _workingDirectory;
    private int _bootTimeoutSeconds = 180;
    private int _diskSizeGb = 10;

    /// <summary>
    /// Sets the VM preset configuration.
    /// </summary>
    public LinuxVmBuilder WithPreset(VmPreset preset)
    {
        _preset = preset;
        return this;
    }

    /// <summary>
    /// Sets the VM memory in megabytes.
    /// </summary>
    public LinuxVmBuilder WithMemory(int memoryMb)
    {
        _memoryMb = memoryMb;
        return this;
    }

    /// <summary>
    /// Sets the number of virtual CPUs.
    /// </summary>
    public LinuxVmBuilder WithCpus(int cpuCount)
    {
        _cpuCount = cpuCount;
        return this;
    }

    /// <summary>
    /// Sets the SSH port for VM connections.
    /// </summary>
    public LinuxVmBuilder WithSshPort(int port)
    {
        _sshPort = port;
        return this;
    }

    /// <summary>
    /// Sets a custom snapshot name to use or create.
    /// </summary>
    public LinuxVmBuilder WithSnapshot(string name)
    {
        _snapshotName = name;
        return this;
    }

    /// <summary>
    /// Adds custom setup commands to run when creating the snapshot.
    /// </summary>
    public LinuxVmBuilder WithSetupCommands(string commands)
    {
        _customSetupCommands = commands;
        return this;
    }

    /// <summary>
    /// Sets a custom base image URL (default: Debian 12 cloud image).
    /// </summary>
    public LinuxVmBuilder WithBaseImageUrl(string url)
    {
        _baseImageUrl = url;
        return this;
    }


    /// <summary>
    /// Sets the working directory for VM files (images, keys, logs).
    /// Default: Application's base directory.
    /// </summary>
    public LinuxVmBuilder WithWorkingDirectory(string path)
    {
        _workingDirectory = path;
        return this;
    }

    /// <summary>
    /// Sets the boot timeout in seconds.
    /// </summary>
    public LinuxVmBuilder WithBootTimeout(int seconds)
    {
        _bootTimeoutSeconds = seconds;
        return this;
    }

    /// <summary>
    /// Sets the disk size in gigabytes.
    /// Default: 10GB. Increase for Docker images or large projects.
    /// </summary>
    public LinuxVmBuilder WithDiskSize(int sizeGb)
    {
        _diskSizeGb = sizeGb;
        return this;
    }

    /// <summary>
    /// Builds the LinuxVm instance with the configured settings.
    /// </summary>
    public LinuxVm Build()
    {
        var config = VmConfiguration.FromPreset(_preset);
        
        config.MemoryMb = _memoryMb;
        config.CpuCount = _cpuCount;
        config.SshPort = _sshPort;
        config.BootTimeoutSeconds = _bootTimeoutSeconds;
        config.DiskSizeGb = _diskSizeGb;
        
        if (!string.IsNullOrEmpty(_snapshotName))
            config.SnapshotName = _snapshotName;
        
        if (!string.IsNullOrEmpty(_customSetupCommands))
            config.CustomSetupCommands = _customSetupCommands;
        
        if (!string.IsNullOrEmpty(_baseImageUrl))
            config.BaseImageUrl = _baseImageUrl;
        
        if (!string.IsNullOrEmpty(_workingDirectory))
            config.WorkingDirectory = _workingDirectory;

        return new LinuxVm(config);
    }
}
