# FreeQEMU HelloWorld Example (NuGet Package Test)

This example is identical to `FreeQEMU.HelloWorldExample` but uses the **NuGet package** instead of a project reference. It validates that the published package works correctly.

## Purpose

- ✅ Verify the FreeQEMU NuGet package works as an external dependency
- ✅ Test the package API matches the documented usage
- ✅ Ensure QEMU binaries are included correctly

## Package References

```xml
<PackageReference Include="FreeQEMU" Version="1.0.1" />
<!-- QEMU binaries - needed explicitly for v1.x -->
<PackageReference Include="Mosa.Tools.Package.Qemu" Version="2.6.0.1532" />
```

> **Note**: v1.x requires explicit `Mosa.Tools.Package.Qemu` reference. This will be fixed in v2.0.0+ where QEMU flows transitively.

## What This Example Does

1. **Creates a VM** with `VmPreset.DotNet10` using the NuGet package API
2. **Uploads** the `HelloWorldTest` project to the VM
3. **Builds and publishes** the project
4. **Runs** the application and captures output
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
cd FreeQEMU.HelloWorldExampleNugetTest
dotnet run
```

## Output

Results are saved to `runs/HelloWorldTest/latest/`:
- `output.txt` - Console output log
- `published-output.tar.gz` - Tarball of published files
- `published/` - Extracted published files

## v1.0.1 API

This example uses the v1.0.1 API:

```csharp
// v1.0.1 API - constructor based
await using var vm = new LinuxVm(VmPreset.DotNet10);
await vm.EnsureReadyAsync();
```

## v2.0.0+ API

Future versions will support the builder pattern:

```csharp
// v2.0.0+ API - builder pattern
await using var vm = LinuxVm.Create()
    .WithPreset(VmPreset.DotNet10)
    .WithDiskSize(10)
    .Build();
```

## See Also

- [FreeQEMU.HelloWorldExample](../FreeQEMU.HelloWorldExample/) - Project reference version
- [FreeQEMU on NuGet](https://www.nuget.org/packages/FreeQEMU) - Published package

Part of the FreeQEMU solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
