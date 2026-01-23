# FreeQEMU Docker Example (NuGet Package Test)

This example is identical to `FreeQEMU.DockerExample` but uses the **NuGet package** instead of a project reference. It validates that the published package works correctly for Docker scenarios.

## Purpose

- ✅ Verify the FreeQEMU NuGet package works for Docker workflows
- ✅ Test the `VmPreset.Docker` preset from the package
- ✅ Ensure Docker container builds work end-to-end

## Package References

```xml
<PackageReference Include="FreeQEMU" Version="1.0.1" />
<!-- QEMU binaries - needed explicitly for v1.x -->
<PackageReference Include="Mosa.Tools.Package.Qemu" Version="2.6.0.1532" />
```

> **Note**: v1.x requires explicit `Mosa.Tools.Package.Qemu` reference. This will be fixed in v2.0.0+.

## What This Example Does

1. **Creates a VM** with `VmPreset.Docker` using the NuGet package API
2. **Uploads** the `HelloWorldTest` project to the VM
3. **Pulls** the .NET SDK Docker image (not pre-cached in v1.0.1)
4. **Builds, publishes, and runs** the project in Docker containers
5. **Downloads** the published artifacts

## Configuration

Edit `appsettings.json`:

```json
{
  "SolutionRoot": "%USERPROFILE%\\source\\repos\\WSU-EIT\\FreeQEMU"
}
```

## Running

```bash
cd FreeQEMU.DockerExampleNugetTest
dotnet run
```

## v1.0.1 vs v2.0.0 Differences

| Feature | v1.0.1 | v2.0.0+ |
|---------|--------|---------|
| Docker preset | `VmPreset.Docker` | `VmPreset.Docker` |
| Pre-cached SDK image | ❌ Must pull | ✅ `VmPreset.DockerDotNet10` |
| Builder pattern | ❌ Constructor only | ✅ `LinuxVm.Create()...Build()` |
| Disk size config | ❌ Fixed | ✅ `WithDiskSize(gb)` |
| QEMU transitive | ❌ Must reference | ✅ Automatic |

## v1.0.1 API (This Example)

```csharp
// v1.0.1 API - constructor based, no pre-cached images
await using var vm = new LinuxVm(VmPreset.Docker);
await vm.EnsureReadyAsync();

// Must pull SDK image manually
await vm.ExecuteAsync("docker pull mcr.microsoft.com/dotnet/sdk:10.0");
```

## v2.0.0+ API (Future)

```csharp
// v2.0.0+ API - builder pattern with pre-cached images
await using var vm = LinuxVm.Create()
    .WithPreset(VmPreset.DockerDotNet10)  // SDK image pre-pulled!
    .WithDiskSize(10)
    .Build();
```

## Output

Results are saved to `runs/docker-build/latest/`:
- `output.txt` - Console output log
- `published-output.tar.gz` - Tarball of published files
- `published/` - Extracted published files

## First Run Note

With v1.0.1's `VmPreset.Docker`:
- The .NET SDK image is NOT pre-cached
- First Docker run will pull ~1GB image
- Subsequent runs use Docker's local cache (until VM restarts)

For faster Docker builds, wait for v2.0.0+ with `VmPreset.DockerDotNet10`.

## See Also

- [FreeQEMU.DockerExample](../FreeQEMU.DockerExample/) - Project reference version
- [FreeQEMU on NuGet](https://www.nuget.org/packages/FreeQEMU) - Published package
