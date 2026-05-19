# FreeQEMU

[![NuGet](https://img.shields.io/nuget/v/FreeQEMU.svg)](https://www.nuget.org/packages/FreeQEMU)
[![GitHub](https://img.shields.io/github/license/WSU-EIT/FreeQEMU)](https://github.com/WSU-EIT/FreeQEMU)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

> Run Linux commands from .NET using a lightweight QEMU VM. Perfect for cross-platform development, CI/CD pipelines, and testing Linux-specific code on Windows.

## ✨ Features

- 🚀 **Instant Boot Snapshots** - Pre-configured VMs boot in ~5 seconds
- 🔧 **Built-in Presets** - .NET 8/9/10, Docker, Docker+.NET SDK, or stock Debian 12
- 📦 **NuGet Package** - QEMU binaries bundled, just add and go
- 🔄 **File Sync** - Upload/download folders via SCP
- 📡 **Streaming Output** - Real-time command output callbacks
- 🛡️ **Ephemeral Mode** - Changes discarded on shutdown
- 💾 **Snapshot Management** - Save/restore VM states
- 🐳 **Docker Support** - Run containers inside the VM with pre-pulled images

## 📦 Installation

```bash
dotnet add package FreeQEMU
```

**Important for v1.x users:** Also add the QEMU binaries package:
```bash
dotnet add package Mosa.Tools.Package.Qemu
```

> Note: v2.0.0+ will include QEMU binaries transitively.

## 🚀 Quick Start

```csharp
using FreeQEMU;

// Create a VM with .NET 10 pre-installed
await using var vm = new LinuxVm(VmPreset.DotNet10);

// First run downloads/configures VM (~3min), subsequent runs use cached snapshot (~5s)
await vm.EnsureReadyAsync();

// Run commands
var result = await vm.ExecuteAsync("dotnet --version");
Console.WriteLine(result.Output); // "10.0.xxx"

// Upload a project, build it, download results
await vm.UploadFolderAsync("./MyProject", "/root/MyProject");
await vm.ExecuteAsync("cd /root/MyProject && dotnet publish -c Release -o /root/out");
await vm.DownloadFolderAsync("/root/out", "./linux-build");
```

## 🔧 VM Presets

| Preset | Description | First Boot | Cached Boot |
|--------|-------------|------------|-------------|
| `Stock` | Vanilla Debian 12 | ~30s | ~5s |
| `DotNet8` | Debian 12 + .NET 8 SDK | ~3min | ~5s |
| `DotNet9` | Debian 12 + .NET 9 SDK | ~3min | ~5s |
| `DotNet10` | Debian 12 + .NET 10 SDK | ~3min | ~5s |
| `Docker` | Debian 12 + Docker Engine | ~3min | ~5s |
| `DockerDotNet9` | Docker + .NET 9 SDK image pre-pulled | ~5min | ~5s |
| `DockerDotNet10` | Docker + .NET 10 SDK image pre-pulled | ~5min | ~5s |
| `Full` | Debian 12 + .NET 8/9/10 + Docker | ~8min | ~5s |

## ⚙️ Builder Pattern (v2.0.0+)

Use the fluent builder for advanced scenarios:

```csharp
await using var vm = LinuxVm.Create()
    .WithPreset(VmPreset.DockerDotNet10)
    .WithMemory(4096)              // 4GB RAM (default: 2048)
    .WithCpus(4)                   // 4 vCPUs (default: 2)
    .WithDiskSize(15)              // 15GB disk (default: 10)
    .WithSnapshot("my-dev-env")    // Custom snapshot name
    .WithSetupCommands("apt install -y nodejs npm") // Additional setup
    .Build();

await vm.EnsureReadyAsync();
```

## 🐳 Docker Example

Build and run .NET projects using Docker containers inside the VM:

```csharp
await using var vm = new LinuxVm(VmPreset.Docker);
await vm.EnsureReadyAsync();

// Pull .NET SDK image (or use DockerDotNet10 preset to have it pre-cached)
await vm.ExecuteAsync("docker pull mcr.microsoft.com/dotnet/sdk:10.0");

// Upload project
await vm.UploadFolderAsync("./MyProject", "/root/MyProject");

// Build and run in container
await vm.ExecuteAsync(@"
    docker run --rm \
        -v /root/MyProject:/src \
        -w /src \
        mcr.microsoft.com/dotnet/sdk:10.0 \
        sh -c 'dotnet restore && dotnet build && dotnet run'
");
```

## 📊 Progress Reporting

Track VM setup progress:

```csharp
var progress = new Progress<VmSetupProgress>(p => 
    Console.WriteLine($"[{p.Stage}] {p.Message}"));

await vm.EnsureReadyAsync(progress);
```

Output:
```
[DownloadingBaseImage] Checking base image...
[GeneratingKeys] Checking SSH keys...
[CheckingSnapshot] Checking snapshot 'freeqemu-dotnet10'...
[StartingVm] Starting VM...
[WaitingForSsh] Waiting for SSH...
[Ready] VM is ready!
```

## 📡 Streaming Command Output

Get real-time output from long-running commands:

```csharp
var result = await vm.ExecuteAsync(
    "apt update && apt upgrade -y",
    onOutput: line => Console.WriteLine($"  {line}"),
    onError: line => Console.Error.WriteLine($"  ERR: {line}"),
    timeoutSeconds: 300);

if (!result.Success)
    Console.WriteLine($"Failed with exit code: {result.ExitCode}");
```

## 📁 File Transfer

Upload and download files/folders:

```csharp
// Upload a folder
await vm.UploadFolderAsync(
    "./local/project", 
    "/root/project",
    new Progress<FileTransferProgress>(p => 
        Console.WriteLine($"Uploaded: {p.CurrentFile} ({p.FilesTransferred}/{p.TotalFiles})")));

// Download results
await vm.DownloadFolderAsync("/root/output", "./local/output");
```

## 💾 Snapshot Management

Save and restore VM state:

```csharp
// Save current state
await vm.SaveSnapshotAsync("after-npm-install");

// Later: restore to that state
await vm.RestoreSnapshotAsync("after-npm-install");

// List all snapshots
var snapshots = await vm.ListSnapshotsAsync();
foreach (var snap in snapshots)
    Console.WriteLine($"  {snap.Name}: {snap.Size}");
```

## 📋 Requirements

| Requirement | Details |
|-------------|---------|
| **OS** | Windows 10/11 (with Hyper-V or WHPX enabled recommended) |
| **Disk** | ~500MB base image + ~1-2GB per preset snapshot |
| **RAM** | 2-4GB for VM (configurable) |
| **Runtime** | .NET 8.0 or later |

> **Note**: QEMU binaries are provided via the `Mosa.Tools.Package.Qemu` NuGet dependency.

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│ Your .NET Application                                   │
├─────────────────────────────────────────────────────────┤
│ FreeQEMU Library                                        │
│  ┌──────────────┐  ┌─────────────────┐  ┌────────────┐ │
│  │   LinuxVm    │  │ VmConfiguration │  │  VmPreset  │ │
│  │   Builder    │  │                 │  │            │ │
│  └──────────────┘  └─────────────────┘  └────────────┘ │
├─────────────────────────────────────────────────────────┤
│ Internal Components                                     │
│  ┌──────────────────┐  ┌───────────────────────────┐   │
│  │ QemuProcessMgr   │  │ SshConnectionManager      │   │
│  └──────────────────┘  └───────────────────────────┘   │
│  ┌──────────────────┐  ┌───────────────────────────┐   │
│  │ SnapshotManager  │  │ CloudInitGenerator        │   │
│  └──────────────────┘  └───────────────────────────┘   │
├─────────────────────────────────────────────────────────┤
│ QEMU VM (Debian 12)                 SSH.NET             │
│  - qemu-system-x86_64               - Command exec      │
│  - KVM/WHPX acceleration            - SCP transfer      │
│  - qcow2 snapshots                                      │
└─────────────────────────────────────────────────────────┘
```

## 🔄 How It Works

1. **First Run**
   - Downloads official Debian 12 cloud image (~350MB)
   - Generates SSH keypair for secure VM access
   - Boots VM with cloud-init (injects SSH key)
   - Installs tools based on preset (e.g., .NET SDK, Docker)
   - Saves "golden snapshot" for instant restore

2. **Subsequent Runs**
   - Loads snapshot directly using QEMU's `-loadvm` flag
   - VM starts in ~5 seconds with everything ready
   - No re-download, no re-install

3. **Command Execution**
   - Commands execute via SSH connection
   - Real-time stdout/stderr streaming
   - Configurable timeouts

4. **File Transfer**
   - Uses SCP via SSH.NET library
   - Supports folders with recursive transfer
   - Progress reporting

## 🧪 Example: CI/CD Linux Build

```csharp
// Build a .NET project for Linux in CI/CD on Windows
await using var vm = new LinuxVm(VmPreset.DotNet10);
await vm.EnsureReadyAsync();

// Upload source
await vm.UploadFolderAsync("./src/MyApp", "/root/MyApp");

// Build for linux-x64
await vm.ExecuteAsync(
    "cd /root/MyApp && dotnet publish -c Release -r linux-x64 --self-contained -o /root/publish",
    onOutput: Console.WriteLine);

// Download Linux binary
await vm.DownloadFolderAsync("/root/publish", "./artifacts/linux-x64");
```

## 🧪 Example: Docker Container Build

```csharp
// Use Docker inside the VM to build (no .NET installed on VM itself)
await using var vm = LinuxVm.Create()
    .WithPreset(VmPreset.DockerDotNet10)  // Docker + .NET 10 image pre-pulled
    .Build();

await vm.EnsureReadyAsync();

// Upload project
await vm.UploadFolderAsync("./MyApp", "/root/MyApp");

// Build and run entirely in Docker
var result = await vm.ExecuteAsync(@"
    docker run --rm -v /root/MyApp:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
        sh -c 'dotnet publish -c Release -o /app && dotnet /app/MyApp.dll'
", onOutput: Console.WriteLine);

Console.WriteLine($"Exit code: {result.ExitCode}");
```

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| VM won't start | Check if another QEMU process is running; FreeQEMU kills orphaned processes by default |
| SSH connection fails | Ensure port 2222+ isn't blocked; check firewall settings |
| Slow first boot | Normal - downloading ~350MB image and installing tools; subsequent boots use snapshot |
| Out of disk space | Delete old snapshots in `bin/Debug/net10.0/images/` directory |
| QEMU not found | Ensure `Mosa.Tools.Package.Qemu` package is referenced (v1.x requirement) |
| "no space left on device" in VM | Use `WithDiskSize(15)` to increase VM disk size (v2.0.0+) |

## 📄 API Reference

### LinuxVm

| Method | Description |
|--------|-------------|
| `EnsureReadyAsync(progress?)` | Downloads image, creates snapshot, starts VM |
| `ExecuteAsync(command, onOutput?, onError?, timeout?)` | Runs a shell command, returns result |
| `UploadFolderAsync(local, remote, progress?)` | Uploads folder via SCP |
| `DownloadFolderAsync(remote, local, progress?)` | Downloads folder via SCP |
| `SaveSnapshotAsync(name)` | Saves current VM state |
| `RestoreSnapshotAsync(name)` | Restores to saved state |
| `ListSnapshotsAsync()` | Lists available snapshots |
| `StopAsync()` | Gracefully stops the VM |

### LinuxVmBuilder (v2.0.0+)

| Method | Description |
|--------|-------------|
| `LinuxVm.Create()` | Start building a VM configuration |
| `.WithPreset(preset)` | Set the VM preset |
| `.WithMemory(mb)` | Set RAM in megabytes |
| `.WithCpus(count)` | Set number of vCPUs |
| `.WithDiskSize(gb)` | Set disk size in gigabytes |
| `.WithSnapshot(name)` | Set custom snapshot name |
| `.WithSetupCommands(cmd)` | Add custom setup commands |
| `.Build()` | Create the LinuxVm instance |

### CommandResult

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | True if exit code was 0 |
| `ExitCode` | `int` | Process exit code |
| `Output` | `string` | Standard output |
| `Error` | `string` | Standard error |
| `Duration` | `TimeSpan` | Execution time |

### VmPreset

| Value | Description |
|-------|-------------|
| `Stock` | Vanilla Debian 12 |
| `DotNet8` | .NET 8 SDK installed |
| `DotNet9` | .NET 9 SDK installed |
| `DotNet10` | .NET 10 SDK installed |
| `Docker` | Docker Engine installed |
| `DockerDotNet9` | Docker + .NET 9 SDK image pre-pulled |
| `DockerDotNet10` | Docker + .NET 10 SDK image pre-pulled |
| `Full` | .NET 8/9/10 + Docker |

## 📚 Examples in Repository

The repository includes full working examples:

| Project | Description |
|---------|-------------|
| `FreeQEMU.HelloWorldExample` | Basic build/run with .NET SDK preset |
| `FreeQEMU.DockerExample` | Docker container builds with DockerDotNet10 preset |
| `FreeQEMU.HelloWorldExampleNugetTest` | Same as HelloWorld but using NuGet package |
| `FreeQEMU.DockerExampleNugetTest` | Same as Docker but using NuGet package |
| `HelloWorldTest` | Simple .NET 10 console app used as build target |

Part of the FreeQEMU solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
