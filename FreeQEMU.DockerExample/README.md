# FreeQEMU Docker Example

This example demonstrates using FreeQEMU with Docker - building and running .NET projects inside Docker containers within the VM.

## What This Example Does

1. **Creates a VM** with `VmPreset.DockerDotNet10` (Docker + .NET 10 SDK image pre-pulled)
2. **Uploads** the `HelloWorldTest` project to the VM
3. **Builds, publishes, and runs** the project using Docker containers
4. **Downloads** the published artifacts as a tarball
5. **Extracts** the files locally

## Why Docker?

- **Isolation**: Build environment is completely isolated in containers
- **Reproducibility**: Same container image = same build every time
- **No SDK on host**: .NET SDK runs in container, not on the VM itself
- **Pre-cached images**: `DockerDotNet10` preset has the SDK image pre-pulled in the snapshot

## Prerequisites

- Windows 10/11
- .NET 10 SDK
- The solution must be built (project references FreeQEMU library)

## Configuration

Edit `appsettings.json` to set your solution root:

```json
{
  "SolutionRoot": "%USERPROFILE%\\source\\repos\\WSU-EIT\\FreeQEMU"
}
```

Supports environment variables like `%USERPROFILE%`.

## Running

```bash
cd FreeQEMU.DockerExample
dotnet run
```

## First Run

The first run will:
- Download Debian 12 cloud image (~350MB)
- Install Docker Engine
- Pull `mcr.microsoft.com/dotnet/sdk:10.0` image (~1GB)
- Create a snapshot for instant future boots
- Total time: ~5-8 minutes

## Subsequent Runs

After the snapshot is created:
- VM boots in ~5 seconds from snapshot
- Docker image is already cached
- No re-downloading or re-pulling

## Output

Results are saved to `runs/docker-build/latest/`:
- `output.txt` - Console output log
- `published-output.tar.gz` - Tarball of published files
- `published/` - Extracted published files

## Sample Output

```
╔══════════════════════════════════════════════════════════════╗
║           FreeQEMU Docker Build Example                      ║
╚══════════════════════════════════════════════════════════════╝

  STEP 1: Starting Docker-enabled VM
  ✓ VM is ready (took 5.1s)

  STEP 2: Verifying Docker and disk space
  Docker version 27.5.1, build 9f9e405
  Disk: 6.5G available of 9.7G
  ✓ SDK image pre-cached: mcr.microsoft.com/dotnet/sdk:10.0

  STEP 4: Build, Publish, and Run with Docker
    === Restoring packages ===
    === Building project ===
    === Publishing project ===
    === Running application ===
    Hello, World!

  ✓ SUCCESS: Docker build example complete!
```

## Key Code

```csharp
// Create VM with Docker + pre-pulled .NET SDK image
await using var vm = LinuxVm.Create()
    .WithPreset(VmPreset.DockerDotNet10)
    .WithDiskSize(10)
    .Build();

await vm.EnsureReadyAsync();

// Upload project
await vm.UploadFolderAsync(projectPath, remoteProjectPath);

// Build and run in Docker container
string buildAndRunCommand = $"""
    docker run --rm \
        -v {remoteProjectPath}:/src \
        -w /src \
        mcr.microsoft.com/dotnet/sdk:10.0 \
        sh -c "dotnet restore && \
               dotnet build -c Release && \
               dotnet publish -c Release -o /app && \
               dotnet /app/{projectName}.dll"
    """;

await vm.ExecuteAsync(buildAndRunCommand, line => Console.WriteLine(line));
```

## VM Presets for Docker

| Preset | Description |
|--------|-------------|
| `Docker` | Docker Engine only (pulls images on demand) |
| `DockerDotNet9` | Docker + .NET 9 SDK image pre-pulled |
| `DockerDotNet10` | Docker + .NET 10 SDK image pre-pulled |

Using `DockerDotNet10` saves ~1GB download time on each fresh run since the SDK image is cached in the snapshot.

## Disk Space

Docker images are large. The example uses `WithDiskSize(10)` to ensure enough space:
- Base Debian: ~1GB
- Docker Engine: ~500MB
- .NET 10 SDK image: ~1.3GB
- Build artifacts: varies

Use `WithDiskSize(15)` if you need to pull multiple images.

## See Also

- [FreeQEMU.HelloWorldExample](../FreeQEMU.HelloWorldExample/) - Basic .NET SDK builds
- [FreeQEMU README](../FreeQEMU/README.md) - Full library documentation
