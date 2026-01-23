# FreeQEMU

[![NuGet](https://img.shields.io/nuget/v/FreeQEMU.svg)](https://www.nuget.org/packages/FreeQEMU)

Run Linux commands from .NET using a lightweight QEMU VM.

## Quick Start

    dotnet add package FreeQEMU
    dotnet add package Mosa.Tools.Package.Qemu

## Projects

- **FreeQEMU** - Core library (NuGet package)
- **FreeQEMU.HelloWorldExample** - Build .NET projects in VM
- **FreeQEMU.DockerExample** - Build with Docker containers
- **FreeQEMU.NugetClientPublisher** - Tool to publish to NuGet.org

## VM Presets

- Stock - Vanilla Debian 12
- DotNet8/9/10 - .NET SDK installed
- Docker - Docker Engine
- DockerDotNet9/10 - Docker + .NET SDK image (v2.0.0+)
- Full - .NET 8/9/10 + Docker

## Documentation

See [FreeQEMU/README.md](FreeQEMU/README.md) for full documentation.

## Links

- [NuGet Package](https://www.nuget.org/packages/FreeQEMU)
- [GitHub Repository](https://github.com/WSU-EIT/FreeQEMU)

## License

MIT License

---
Made with love by WSU-EIT
