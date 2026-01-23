namespace FreeQEMU;

/// <summary>
/// Predefined VM configurations for common use cases.
/// </summary>
public enum VmPreset
{
    /// <summary>
    /// Stock Debian 12 with no additional tools installed.
    /// Fastest to create but requires manual tool installation.
    /// </summary>
    Stock,

    /// <summary>
    /// Debian 12 with .NET 8 SDK installed.
    /// </summary>
    DotNet8,

    /// <summary>
    /// Debian 12 with .NET 9 SDK installed.
    /// </summary>
    DotNet9,

    /// <summary>
    /// Debian 12 with .NET 10 SDK installed.
    /// </summary>
    DotNet10,

    /// <summary>
    /// Debian 12 with Docker installed.
    /// </summary>
    Docker,

    /// <summary>
    /// Debian 12 with Docker installed and .NET 9 SDK Docker image pre-pulled.
    /// Ready to build .NET 9 projects using Docker containers.
    /// </summary>
    DockerDotNet9,

    /// <summary>
    /// Debian 12 with Docker installed and .NET 10 SDK Docker image pre-pulled.
    /// Ready to build .NET 10 projects using Docker containers.
    /// </summary>
    DockerDotNet10,

    /// <summary>
    /// Debian 12 with .NET 8, 9, 10 SDKs and Docker installed.
    /// Takes longest to create but provides full development environment.
    /// </summary>
    Full
}
