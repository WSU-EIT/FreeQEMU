# FreeQEMU

[![NuGet](https://img.shields.io/nuget/v/FreeQEMU.svg)](https://www.nuget.org/packages/FreeQEMU)
[![GitHub](https://img.shields.io/github/license/WSU-EIT/FreeQEMU)](https://github.com/WSU-EIT/FreeQEMU)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

> Run Linux commands from .NET using a lightweight QEMU VM. Perfect for cross-platform development, CI/CD pipelines, and testing Linux-specific code on Windows.

## ✨ Features

- 🚀 **Instant Boot Snapshots** - Pre-configured VMs boot in ~5 seconds
- 🔧 **Built-in Presets** - .NET 8/9/10, Docker, or stock Debian 12
- 📦 **NuGet Package** - QEMU binaries bundled, just add and go
- 🔄 **File Sync** - Upload/download folders via SCP
- 📡 **Streaming Output** - Real-time command output callbacks
- 🛡️ **Ephemeral Mode** - Changes discarded on shutdown
- 💾 **Snapshot Management** - Save/restore VM states

## 📦 Installation

```bash
dotnet add package FreeQEMU
```

## 🚀 Quick Start

```csharp
using FreeQEMU;

// Create a VM with .NET 10 pre-installed
using var vm = new LinuxVm(VmPreset.DotNet10);

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
| `Stock` | Vanilla Debian 12 | ~30s | ~30s |
| `DotNet8` | Debian 12 + .NET 8 SDK | ~3min | ~5s |
| `DotNet9` | Debian 12 + .NET 9 SDK | ~3min | ~5s |
| `DotNet10` | Debian 12 + .NET 10 SDK | ~3min | ~5s |
| `Docker` | Debian 12 + Docker Engine | ~3min | ~5s |
| `Full` | Debian 12 + .NET 8/9/10 + Docker | ~8min | ~5s |

## ⚙️ Custom Configuration

Use the fluent builder for advanced scenarios:

```csharp
using var vm = LinuxVm.Create()
    .WithPreset(VmPreset.DotNet10)
    .WithMemory(4096)           // 4GB RAM (default: 2048)
    .WithCpus(4)                // 4 vCPUs (default: 2)
    .WithSnapshot("my-dev-env") // Custom snapshot name
    .WithSetupCommands("apt install -y nodejs npm") // Additional setup
    .Build();

await vm.EnsureReadyAsync();
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
| **OS** | Windows 10/11 |
| **Disk** | ~500MB for base Debian image |
| **RAM** | ~2GB for VM (configurable) |
| **Runtime** | .NET 8.0 or later |

> **Note**: QEMU binaries are bundled via the `Mosa.Tools.Package.Qemu` NuGet dependency. No separate installation required.

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│ Your .NET Application                                   │
├─────────────────────────────────────────────────────────┤
│ FreeQEMU Library                                        │
│  ┌──────────────┐  ┌─────────────────┐  ┌────────────┐ │
│  │   LinuxVm    │  │ VmConfiguration │  │  VmPreset  │ │
│  └──────────────┘  └─────────────────┘  └────────────┘ │
├─────────────────────────────────────────────────────────┤
│ Internal Components                                     │
│  ┌──────────────────┐  ┌───────────────────────────┐   │
│  │ QemuProcessMgr   │  │ SshConnectionManager      │   │
│  └──────────────────┘  └───────────────────────────┘   │
│  ┌──────────────────┐                                   │
│  │ SnapshotManager  │                                   │
│  └──────────────────┘                                   │
├─────────────────────────────────────────────────────────┤
│ QEMU VM (Debian 12)                 SSH.NET             │
│  - qemu-system-x86_64               - Command exec      │
│  - cloud-init                       - SCP transfer      │
│  - qcow2 snapshots                                      │
└─────────────────────────────────────────────────────────┘
```

## 🔄 How It Works

1. **First Run**
   - Downloads official Debian 12 cloud image (~350MB)
   - Generates SSH keypair for secure VM access
   - Boots VM with cloud-init (injects SSH key)
   - Installs tools based on preset (e.g., .NET SDK)
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
using var vm = new LinuxVm(VmPreset.DotNet10);
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

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| VM won't start | Check if another QEMU process is running; FreeQEMU kills orphaned processes by default |
| SSH connection fails | Ensure port 2222+ isn't blocked; check firewall settings |
| Slow first boot | Normal - downloading ~350MB image and installing tools; subsequent boots use snapshot |
| Out of disk space | Delete old snapshots in the working directory |

## 📄 API Reference

### LinuxVm

| Method | Description |
|--------|-------------|
| `EnsureReadyAsync()` | Downloads image, creates snapshot, starts VM |
| `ExecuteAsync(command)` | Runs a shell command, returns result |
| `UploadFolderAsync(local, remote)` | Uploads folder via SCP |
| `DownloadFolderAsync(remote, local)` | Downloads folder via SCP |
| `SaveSnapshotAsync(name)` | Saves current VM state |
| `RestoreSnapshotAsync(name)` | Restores to saved state |
| `StopAsync()` | Gracefully stops the VM |

### CommandResult

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | True if exit code was 0 |
| `ExitCode` | `int` | Process exit code |
| `Output` | `string` | Standard output |
| `Error` | `string` | Standard error |
| `Duration` | `TimeSpan` | Execution time |

## 📚 Related Projects

- [exploratorydocker](../exploratorydocker/) - Interactive CLI with menus and advanced features
- [FreeDebian](../FreeDebian/) - Test harness for FreeQEMU

## 📄 License

MIT License - See [LICENSE](../LICENSE) for details.

## 🤝 Contributing

Contributions welcome! Please see the [main repository](https://github.com/DanielPepka/exploratorydocker) for guidelines.
