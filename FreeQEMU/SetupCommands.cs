namespace FreeQEMU;

/// <summary>
/// Predefined setup command strings for each VM preset.
/// These commands are executed when creating a golden snapshot for fast subsequent boots.
/// </summary>
internal static class SetupCommands
{
    /// <summary>
    /// Base commands to download and prepare the .NET install script.
    /// </summary>
    private const string DotNetInstallBase = 
        "apt-get update -qq && apt-get install -y -qq wget ca-certificates && " +
        "wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh && " +
        "chmod +x /tmp/dotnet-install.sh";

    /// <summary>
    /// Setup commands for .NET 8 SDK preset.
    /// </summary>
    public const string DotNet8 = 
        DotNetInstallBase + " && " +
        "/tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet && " +
        "ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet && " +
        "dotnet --version";

    /// <summary>
    /// Setup commands for .NET 9 SDK preset.
    /// </summary>
    public const string DotNet9 = 
        DotNetInstallBase + " && " +
        "/tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet && " +
        "ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet && " +
        "dotnet --version";

    /// <summary>
    /// Setup commands for .NET 10 SDK preset.
    /// </summary>
    public const string DotNet10 = 
        DotNetInstallBase + " && " +
        "/tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet && " +
        "ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet && " +
        "dotnet --version";

    /// <summary>
    /// Setup commands for Docker Engine preset.
    /// Note: Uses --batch flag for gpg to avoid /dev/tty issues when running via SSH.
    /// Removes existing gpg key file first to handle re-runs cleanly.
    /// </summary>
    public const string Docker = 
        "apt-get update -qq && " +
        "apt-get install -y -qq ca-certificates curl gnupg && " +
        "install -m 0755 -d /etc/apt/keyrings && " +
        "rm -f /etc/apt/keyrings/docker.gpg && " +
        "curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --batch --dearmor -o /etc/apt/keyrings/docker.gpg && " +
        "chmod a+r /etc/apt/keyrings/docker.gpg && " +
        "echo 'deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian bookworm stable' > /etc/apt/sources.list.d/docker.list && " +
        "apt-get update -qq && " +
        "apt-get install -y -qq docker-ce docker-ce-cli containerd.io && " +
        "docker --version";

    /// <summary>
    /// Setup commands for Docker + .NET 9 SDK image preset.
    /// Installs Docker and pre-pulls the .NET 9 SDK Docker image.
    /// </summary>
    public const string DockerDotNet9 = 
        Docker + " && " +
        "echo 'Pulling .NET 9 SDK Docker image...' && " +
        "docker pull mcr.microsoft.com/dotnet/sdk:9.0 && " +
        "docker images";

    /// <summary>
    /// Setup commands for Docker + .NET 10 SDK image preset.
    /// Installs Docker and pre-pulls the .NET 10 SDK Docker image.
    /// </summary>
    public const string DockerDotNet10 = 
        Docker + " && " +
        "echo 'Pulling .NET 10 SDK Docker image...' && " +
        "docker pull mcr.microsoft.com/dotnet/sdk:10.0 && " +
        "docker images";

    /// <summary>
        /// Setup commands for Full preset (.NET 8/9/10 + Docker).
        /// Note: Uses --batch flag for gpg to avoid /dev/tty issues when running via SSH.
        /// </summary>
        public const string Full = 
            DotNetInstallBase + " && " +
            "/tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet && " +
            "/tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet && " +
            "/tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet && " +
            "ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet && " +
            "apt-get install -y -qq ca-certificates curl gnupg && " +
            "install -m 0755 -d /etc/apt/keyrings && " +
            "rm -f /etc/apt/keyrings/docker.gpg && " +
            "curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --batch --dearmor -o /etc/apt/keyrings/docker.gpg && " +
            "chmod a+r /etc/apt/keyrings/docker.gpg && " +
            "echo 'deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian bookworm stable' > /etc/apt/sources.list.d/docker.list && " +
            "apt-get update -qq && " +
            "apt-get install -y -qq docker-ce docker-ce-cli containerd.io && " +
            "dotnet --list-sdks && docker --version";
    }
