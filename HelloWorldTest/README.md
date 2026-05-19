# HelloWorldTest

Console executable that prints "Hello, World!" to standard output.

This project is the build target used by `FreeQEMU.HelloWorldExample` and `FreeQEMU.HelloWorldExampleNugetTest`. Both example runners upload this project to a QEMU Linux VM, invoke `dotnet publish`, execute the resulting binary inside the VM, and then download the published artifacts back to the host. It therefore serves as an end-to-end smoke test for the FreeQEMU library's upload, build, run, and download pipeline.

## Key types

| Type | Description |
|------|-------------|
| `Program` | Entry point; writes `"Hello, World!"` via `Console.WriteLine` |

## Project references and packages

No project references or NuGet packages — the project is intentionally minimal to keep the smoke test fast and dependency-free.

## Build info

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | `Exe` |

Part of the FreeQEMU solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
