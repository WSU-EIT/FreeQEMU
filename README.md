# FreeQEMU

Run Linux commands from .NET using lightweight QEMU virtual machines. Published as a NuGet package with example consumers (HelloWorld, Docker) plus a publisher tool. Useful for cross-platform builds and isolated command execution from .NET applications.

## Projects in this solution

| Project | Role |
|---------|------|
| `FreeQEMU` | Core library — NuGet package source |
| `FreeQEMU.HelloWorldExample` | Example: build/run with .NET SDK preset (project reference) |
| `FreeQEMU.DockerExample` | Example: build/run via Docker containers (project reference) |
| `FreeQEMU.HelloWorldExampleNugetTest` | Same as HelloWorld but using published NuGet package |
| `FreeQEMU.DockerExampleNugetTest` | Same as Docker but using published NuGet package |
| `FreeQEMU.NugetClientPublisher` | Interactive CLI tool for packing and pushing to NuGet.org |
| `HelloWorldTest` | Minimal console app used as the build target in all examples |

See each project README.md for its specific role.

Part of the FreeQEMU solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
