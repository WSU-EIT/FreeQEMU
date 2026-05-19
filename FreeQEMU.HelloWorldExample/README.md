# FreeQEMU HelloWorld Example

This example demonstrates basic FreeQEMU usage - creating a VM with .NET SDK, uploading a project, building it, and downloading the published output.

## What This Example Does

1. **Creates a VM** with `VmPreset.DotNet10` (.NET 10 SDK pre-installed)
2. **Uploads** the `HelloWorldTest` project to the VM
3. **Builds and publishes** the project using `dotnet publish`
4. **Runs** the application and captures output
5. **Downloads** the published artifacts as a tarball
6. **Extracts** the files locally

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
cd FreeQEMU.HelloWorldExample
dotnet run
```

## First Run

The first run will:
- Download Debian 12 cloud image (~350MB)
- Install .NET 10 SDK
- Create a snapshot for instant future boots
- Total time: ~3-5 minutes

## Subsequent Runs

After the snapshot is created:
- VM boots in ~5 seconds from snapshot
- No re-downloading or re-installing

## Output

Results are saved to `runs/HelloWorldTest/latest/`:
- `output.txt` - Console output log
- `published-output.tar.gz` - Tarball of published files
- `published/` - Extracted published files

## Sample Output

```
╔══════════════════════════════════════════════════════════════╗
║           FreeQEMU Build & Run Test                          ║
╚══════════════════════════════════════════════════════════════╝

  STEP 1: Starting VM with .NET 10 SDK
  [DownloadingBaseImage] Checking base image...
  [CheckingSnapshot] Checking snapshot 'freeqemu-dotnet10'...
  [StartingVm] Starting VM...
  ✓ VM is ready (took 5.2s)

  STEP 2: Verifying .NET SDK
  .NET SDK version: 10.0.100-preview.4.25258.110

  STEP 5: Running the application
    OUTPUT: Hello, World!

  ✓ SUCCESS: HelloWorld example complete!
```

## Key Code

```csharp
// Create VM with .NET 10 preset
await using var vm = LinuxVm.Create()
    .WithPreset(VmPreset.DotNet10)
    .WithDiskSize(10)
    .Build();

await vm.EnsureReadyAsync();

// Upload, build, run
await vm.UploadFolderAsync(projectPath, remoteProjectPath);
await vm.ExecuteAsync($"cd {remoteProjectPath} && dotnet publish -c Release -o /root/publish");
await vm.ExecuteAsync($"dotnet /root/publish/{projectName}.dll");

// Download results
await vm.DownloadFolderAsync("/root/export", downloadDir);
```

## See Also

- [FreeQEMU.DockerExample](../FreeQEMU.DockerExample/) - Docker container builds
- [FreeQEMU README](../FreeQEMU/README.md) - Full library documentation

Part of the FreeQEMU solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
