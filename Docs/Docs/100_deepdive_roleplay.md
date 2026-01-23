# 100 — Meeting: FreeQEMU v2 Migration Onboarding

> **Document ID:** 100  
> **Category:** Meeting  
> **Purpose:** Onboard junior dev on FreeQEMU architecture and v1→v2 migration strategy  
> **Attendees:** [Architect], [PublicAPI], [Internals], [Quality], [Sanity], [JrDev]  
> **Date:** 2025-01-20  
> **Predicted Outcome:** JrDev understands codebase, team aligns on migration approach  
> **Actual Outcome:** ✅ Architecture understood, migration strategy defined  
> **Resolution:** Proceed with doc 101 project plan

---

## Context

We have a working v1 codebase for FreeQEMU — a .NET library that runs Linux commands via QEMU VMs with instant-boot snapshots. The v2 folder exists but is empty (just .csproj shells). We need to migrate v1 → v2 with KISS/DRY refactoring, publish to GitHub, and create a NuGet package.

---

## Discussion

**[Architect]:** Alright team, let's bring our new junior dev up to speed on FreeQEMU. This is a library project, so we'll use Library roles: [PublicAPI], [Internals], [Perf], [Docs]. I'll frame the overall system.

FreeQEMU lets .NET developers run Linux commands without leaving Windows. It spins up a lightweight QEMU VM with Debian, connects via SSH, and executes commands. The magic is in "golden snapshots" — pre-configured VM states that boot in ~5 seconds instead of 30+ seconds.

**[JrDev]:** Wait, so this bundles actual QEMU binaries inside a NuGet package?

**[Internals]:** Exactly. We depend on `Mosa.Tools.Package.Qemu` which bundles Windows QEMU binaries. When you `dotnet add package FreeQEMU`, you get everything needed to run Linux VMs. No separate QEMU install required.

**[JrDev]:** That's clever. What's in the v1 codebase right now?

**[Architect]:** Let me break down the solution structure:

```
v1/
├── FreeQEMU/                         # Core library (the NuGet package)
│   ├── LinuxVm.cs                    # Main public API
│   ├── LinuxVmBuilder.cs             # Fluent builder
│   ├── VmConfiguration.cs            # Config + SetupCommands
│   ├── VmPreset.cs                   # Enum: Stock, DotNet8/9/10, Docker, Full
│   ├── Models.cs                     # DTOs (CommandResult, SnapshotInfo, etc.)
│   ├── Exceptions.cs                 # Custom exception hierarchy
│   └── Internal/
│       ├── Interfaces.cs             # Internal contracts
│       ├── QemuProcessManager.cs     # QEMU process lifecycle (~570 lines!)
│       ├── SshConnectionManager.cs   # SSH.NET wrapper
│       └── SnapshotManager.cs        # qemu-img snapshot ops
│
├── FreeQEMU.NugetClientPublisher/    # Tool to publish FreeQEMU to NuGet
├── FreeDebian/                       # Test harness
├── FreeDebian.HelloWorldExample/     # Simple usage example
├── FreeDebian.NugetClientPublisher/  # Publishes "FreeGLBA.Client" (naming confusion!)
└── HelloWorldTest/                   # Minimal project to build inside VM
```

**[JrDev]:** I see some naming confusion — "FreeDebian" vs "FreeQEMU" vs "FreeGLBA"?

**[Quality]:** Good catch! That's technical debt. The project evolved organically:
- Started as "exploratorydocker" (legacy name still in some URLs)
- Became "FreeDebian" (the test harness)
- Core library is "FreeQEMU" (correct name)
- "FreeGLBA.Client" appears in one publisher — unclear what that's for

**[Architect]:** That naming mess is exactly why we're doing v2. We'll clean this up.

---

**[PublicAPI]:** Let me explain the public API surface. Users interact with two main classes:

**1. LinuxVm** — The main entry point:
```csharp
using var vm = new LinuxVm(VmPreset.DotNet10);
await vm.EnsureReadyAsync();
var result = await vm.ExecuteAsync("dotnet --version");
```

**2. LinuxVmBuilder** — Fluent configuration:
```csharp
using var vm = LinuxVm.Create()
    .WithPreset(VmPreset.DotNet10)
    .WithMemory(4096)
    .WithCpus(4)
    .Build();
```

The API is clean. Users don't need to know about QEMU, SSH, or snapshots — it just works.

**[JrDev]:** What about `VmPreset`? What options exist?

**[PublicAPI]:** Six presets:

| Preset | What's Installed | First Boot | Cached Boot |
|--------|-----------------|------------|-------------|
| `Stock` | Vanilla Debian 12 | ~30s | ~30s |
| `DotNet8` | + .NET 8 SDK | ~3min | ~5s |
| `DotNet9` | + .NET 9 SDK | ~3min | ~5s |
| `DotNet10` | + .NET 10 SDK | ~3min | ~5s |
| `Docker` | + Docker Engine | ~3min | ~5s |
| `Full` | + .NET 8/9/10 + Docker | ~8min | ~5s |

First boot downloads the Debian cloud image, installs tools, and saves a "golden snapshot." Subsequent boots load from snapshot — hence the ~5 second boot time.

---

**[Internals]:** Now for the internals. Three main managers:

**QemuProcessManager** (~570 lines) — Does too much:
- Downloads Debian base image with SHA512 verification
- Generates SSH keypairs (RSA 2048, OpenSSH format)
- Creates cloud-init seed ISO for VM configuration
- Starts/stops QEMU process
- Sends QMP monitor commands

**SshConnectionManager** (~350 lines) — Clean, focused:
- Wraps SSH.NET for command execution
- Handles file transfers via SCP
- Retry logic for connections

**SnapshotManager** (~200 lines) — Also clean:
- Lists snapshots via qemu-img
- Saves/restores via QMP commands
- Parses snapshot info

**[JrDev]:** That QemuProcessManager sounds like it violates single responsibility.

**[Sanity]:** Mid-check — Yes! That's a prime candidate for refactoring. We could extract:
- `ImageManager` — Download, verify, cache
- `SshKeyManager` — Generate and validate keys
- `CloudInitManager` — Create seed ISOs
- `QemuProcessManager` — Just process lifecycle

But... are we overcomplicating? For a v2, maybe just keep it working and add TODOs for future extraction?

**[Architect]:** Good question. Let's prioritize:
1. **Must do:** Fix naming, clean folder structure, publish to NuGet
2. **Should do:** Extract `SetupCommands` to own file (it's embedded in VmConfiguration)
3. **Could do:** Split QemuProcessManager (defer to v2.1)

---

**[Quality]:** Let me highlight test coverage concerns:

**Current state:**
- `FreeDebian` is an integration test harness (not unit tests)
- It uploads `HelloWorldTest`, builds it in the VM, runs it
- No actual unit test project with xUnit/NUnit

**For v2 we need:**
- A proper `FreeQEMU.Tests` project
- Unit tests for parsers, builders, config
- Integration tests (what FreeDebian does now)

**[JrDev]:** What about the NuGet publishers? Why are there two?

**[Internals]:** Another DRY violation! Both publishers are ~95% identical code:
- Interactive menu (view config, pack, push, lookup versions, trim old versions)
- Configuration via appsettings.json + user secrets
- Same build/pack/push logic

The only differences are:
- Package name in config
- Banner text in console

**[Sanity]:** We should either:
1. Merge into one generic publisher with config-driven package name
2. Extract shared base class

Option 1 is simpler. One publisher, multiple appsettings profiles.

---

**[Architect]:** Let's talk v2 migration strategy. Goals:

1. **Clean naming** — Remove FreeDebian confusion, clarify FreeGLBA purpose
2. **File organization** — Maybe split Models.cs, extract SetupCommands
3. **DRY publishers** — Single configurable publisher
4. **Publish to GitHub** — v2 folder goes public at github.com/WSU-EIT/FreeQEMU
5. **Publish NuGet** — FreeQEMU package to nuget.org
6. **Dogfood test** — New project that uses the NuGet package (not project reference)

**[JrDev]:** What about the existing v2/Docs? They seem to be FreeCRM/FreeManager docs, not FreeQEMU docs.

**[Quality]:** Correct! The Guides (000-008) are generic doc templates we reuse across projects. They stay. But we need FreeQEMU-specific docs:
- README.md with usage examples
- Migration notes (v1→v2 changes)
- API reference

---

**[Sanity]:** Final check — Did we miss anything?

**[PublicAPI]:** The v2 .csproj files already exist with correct NuGet metadata. We just need to copy the code.

**[Internals]:** Dependencies are already in v2 .csproj:
- `Mosa.Tools.Package.Qemu` (QEMU binaries)
- `SSH.NET` (SSH client)
- `DiscUtils.Iso9660` (cloud-init ISO creation)

**[Quality]:** We should verify the v2 builds after migration before publishing.

**[Architect]:** Agreed. The plan crystallizes into phases:
1. **Prep** — Understand what exists (this meeting ✓)
2. **Migrate** — Copy code to v2 with minimal cleanup
3. **Refactor** — Apply KISS/DRY improvements
4. **Publish** — GitHub + NuGet
5. **Validate** — Test project using NuGet package

---

## Decisions

1. **Keep FreeQEMU name** — It's correct and already in NuGet metadata
2. **Rename test projects** — FreeDebian → FreeQEMU.TestHarness, etc.
3. **Single publisher** — Merge both NuGet publishers into one configurable tool
4. **Defer QemuProcessManager split** — Document as tech debt for v2.1
5. **Extract SetupCommands** — Move from VmConfiguration to own file
6. **Split Models.cs** — One file per DTO class
7. **Clarify FreeGLBA** — Ask CTO what FreeGLBA.Client is for

---

⏸️ **CTO Input Needed**

**Question:** What is FreeGLBA.Client? Is it a separate NuGet package we need to publish?

**Options:**
1. It's the same as FreeQEMU — consolidate
2. It's a different package — keep separate publisher
3. It's obsolete — remove

@CTO — Which way?

---

## Open Questions

- What is FreeGLBA.Client?
- Should v2 target .NET 10 only, or multi-target (.NET 8, 9, 10)?
- Do we need GitHub Actions CI/CD for the public repo?

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Create project plan (doc 101) | [Architect] | P1 |
| Clarify FreeGLBA purpose | [CTO] | P1 |
| Copy FreeQEMU core to v2 | [Internals] | P2 |
| Create FreeQEMU.Tests project | [Quality] | P2 |
| Merge NuGet publishers | [Internals] | P3 |

---

*Created: 2025-01-20*  
*Maintained by: [Quality]*
