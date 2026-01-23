# 101 — Project Plan: FreeQEMU v2 Migration & Publication

> **Document ID:** 101  
> **Category:** Planning  
> **Purpose:** Phased project plan for v1→v2 migration, GitHub publication, and NuGet release  
> **Audience:** Dev team, contributors  
> **Predicted Outcome:** Complete v2 release with public GitHub repo and NuGet package  
> **Actual Outcome:** 🔄 In progress  
> **Resolution:** {Update when complete}

---

## Summary

- **Problem:** v1 codebase has naming confusion, DRY violations, and isn't published publicly
- **Goal:** Clean v2 release on GitHub + NuGet, validated with a consumer test project
- **Non-goals:** Major architectural refactoring (defer to v2.1), multi-platform support

## Acceptance Criteria

- [ ] v2/FreeQEMU builds and passes tests
- [ ] GitHub repo (WSU-EIT/FreeQEMU) is public with clean v2 code
- [ ] FreeQEMU NuGet package published to nuget.org
- [ ] Test project successfully consumes NuGet package (not project reference)
- [ ] README.md has clear usage examples

---

# Phase 1: Core Library Migration

**Goal:** Get FreeQEMU core library building in v2

## Task 1.1: Copy Core Source Files

| Item | Source | Destination | Notes |
|------|--------|-------------|-------|
| LinuxVm.cs | v1/FreeQEMU/ | v2/FreeQEMU/ | Main API, copy as-is |
| LinuxVmBuilder.cs | v1/FreeQEMU/ | v2/FreeQEMU/ | Copy as-is |
| VmConfiguration.cs | v1/FreeQEMU/ | v2/FreeQEMU/ | Extract SetupCommands |
| VmPreset.cs | v1/FreeQEMU/ | v2/FreeQEMU/ | Copy as-is |
| Models.cs | v1/FreeQEMU/ | v2/FreeQEMU/ | Split into separate files |
| Exceptions.cs | v1/FreeQEMU/ | v2/FreeQEMU/ | Copy as-is |

**Checklist:**
- [ ] Copy all source files to v2/FreeQEMU/
- [ ] Verify namespaces are correct (`namespace FreeQEMU;`)
- [ ] Build succeeds with `dotnet build v2/FreeQEMU`

## Task 1.2: Copy Internal Folder

| Item | Source | Destination | Notes |
|------|--------|-------------|-------|
| Interfaces.cs | v1/FreeQEMU/Internal/ | v2/FreeQEMU/Internal/ | Copy as-is |
| QemuProcessManager.cs | v1/FreeQEMU/Internal/ | v2/FreeQEMU/Internal/ | Copy as-is (refactor later) |
| SshConnectionManager.cs | v1/FreeQEMU/Internal/ | v2/FreeQEMU/Internal/ | Copy as-is |
| SnapshotManager.cs | v1/FreeQEMU/Internal/ | v2/FreeQEMU/Internal/ | Copy as-is |

**Checklist:**
- [ ] Create v2/FreeQEMU/Internal/ folder
- [ ] Copy all internal files
- [ ] Verify `internal` access modifiers are correct

## Task 1.3: Extract SetupCommands

**Current:** `SetupCommands` class is at bottom of `VmConfiguration.cs`

**Target:** New file `v2/FreeQEMU/SetupCommands.cs`

```csharp
namespace FreeQEMU;

/// <summary>
/// Predefined setup command strings for each preset.
/// </summary>
internal static class SetupCommands
{
    // Move all const strings here
}
```

**Checklist:**
- [ ] Create SetupCommands.cs
- [ ] Move SetupCommands class from VmConfiguration.cs
- [ ] Verify VmConfiguration still compiles

## Task 1.4: Split Models.cs (Optional)

**Current:** Single `Models.cs` with ~190 lines containing:
- `CommandResult`
- `FileTransferProgress`
- `VmSetupStage` (enum)
- `VmSetupProgress`
- `SnapshotInfo`

**Target:** Keep as-is OR split to individual files in `v2/FreeQEMU/Models/`

**Decision:** Defer to v2.1 unless time permits. Single file is fine for now.

- [ ] **SKIP** — Keep Models.cs as single file for v2.0

## Task 1.5: Copy README.md

**Checklist:**
- [ ] Copy v1/FreeQEMU/README.md to v2/FreeQEMU/README.md
- [ ] Update GitHub URLs to WSU-EIT/FreeQEMU
- [ ] Verify NuGet badge URL is correct

## Task 1.6: Verify Build

```bash
cd v2/FreeQEMU
dotnet build
```

**Checklist:**
- [ ] Build succeeds with no errors
- [ ] No warnings (or acceptable warnings documented)
- [ ] XML documentation generates (for IntelliSense)

---

# Phase 2: Publisher Tool Migration

**Goal:** Single NuGet publisher tool that works for FreeQEMU

## Task 2.1: Copy Publisher Source

| Item | Source | Destination |
|------|--------|-------------|
| Program.cs | v1/FreeQEMU.NugetClientPublisher/ | v2/FreeQEMU.NugetClientPublisher/ |
| appsettings.json | v1/FreeQEMU.NugetClientPublisher/ | v2/FreeQEMU.NugetClientPublisher/ |
| NuGetConfig.cs | v1/FreeQEMU.NugetClientPublisher/ | v2/FreeQEMU.NugetClientPublisher/ |

**Checklist:**
- [ ] Copy Program.cs
- [ ] Copy appsettings.json
- [ ] Copy any supporting classes (NuGetConfig, etc.)
- [ ] Update banner text if needed

## Task 2.2: Configure for FreeQEMU Package

Update `appsettings.json`:
```json
{
  "NuGet": {
    "PackageId": "FreeQEMU",
    "Version": "2.0.0",
    "ProjectPath": "../FreeQEMU/FreeQEMU.csproj",
    "Source": "https://api.nuget.org/v3/index.json",
    "Configuration": "Release"
  }
}
```

**Checklist:**
- [ ] Update PackageId to "FreeQEMU"
- [ ] Set Version to "2.0.0"
- [ ] Verify ProjectPath is correct relative path
- [ ] Test with `dotnet run` (dry run mode)

## Task 2.3: Set Up User Secrets

```bash
cd v2/FreeQEMU.NugetClientPublisher
dotnet user-secrets set "NuGet:ApiKey" "<YOUR_NUGET_API_KEY>"
```

**Checklist:**
- [ ] User secrets configured
- [ ] API key works (test with lookup command)

---

# Phase 3: Example Project Migration

**Goal:** Working example that demonstrates FreeQEMU usage

## Task 3.1: Copy HelloWorld Example

| Item | Source | Destination |
|------|--------|-------------|
| Program.cs | v1/FreeDebian.HelloWorldExample/ | v2/FreeDebian.HelloWorldExample/ |

**Checklist:**
- [ ] Copy Program.cs
- [ ] Update project reference to v2/FreeQEMU
- [ ] Verify example builds and runs

## Task 3.2: Copy Sample Project (HelloWorldTest)

This is the minimal .NET project that gets uploaded to and built inside the VM.

| Item | Source | Destination |
|------|--------|-------------|
| Program.cs | v1/HelloWorldTest/ | v2/HelloWorldTest/ |
| HelloWorldTest.csproj | v1/HelloWorldTest/ | v2/HelloWorldTest/ |

**Checklist:**
- [ ] Copy project files
- [ ] Verify it's a standalone console app

---

# Phase 4: GitHub Publication

**Goal:** Public repo at github.com/WSU-EIT/FreeQEMU

## Task 4.1: Prepare Repository Structure

**Target structure for public repo:**
```
FreeQEMU/
├── src/
│   └── FreeQEMU/           # Core library
├── tools/
│   └── NugetPublisher/     # Publisher tool
├── examples/
│   └── HelloWorld/         # Example project
├── tests/
│   └── FreeQEMU.Tests/     # Unit tests (future)
├── docs/                   # Documentation
├── README.md
├── LICENSE
├── .gitignore
└── FreeQEMU.sln
```

**Checklist:**
- [ ] Decide: Publish v2 folder as-is OR restructure?
- [ ] Create root README.md (copy from v2/FreeQEMU/README.md)
- [ ] Add LICENSE file (MIT)
- [ ] Add .gitignore for .NET projects

## Task 4.2: Clean Git History

**Options:**
1. Push v2 as new repo (clean history)
2. Push entire repo including v1 (preserve history)

**Recommendation:** Option 1 — clean start for public repo

**Checklist:**
- [ ] Create new GitHub repo: WSU-EIT/FreeQEMU
- [ ] Initialize with README, LICENSE, .gitignore
- [ ] Copy v2 files (not git history)
- [ ] Initial commit: "Initial v2.0.0 release"

## Task 4.3: Push to GitHub

```bash
cd v2
git init
git add .
git commit -m "Initial v2.0.0 release"
git remote add origin https://github.com/WSU-EIT/FreeQEMU.git
git push -u origin main
```

**Checklist:**
- [ ] Repo is public
- [ ] README displays correctly
- [ ] No secrets in committed code
- [ ] .gitignore excludes bin/, obj/, user secrets

---

# Phase 5: NuGet Publication

**Goal:** FreeQEMU package live on nuget.org

## Task 5.1: Update Package Metadata

In `v2/FreeQEMU/FreeQEMU.csproj`:
```xml
<PropertyGroup>
  <PackageId>FreeQEMU</PackageId>
  <Version>2.0.0</Version>
  <Authors>Daniel Pepka</Authors>
  <Description>Run Linux commands from .NET using a lightweight QEMU VM...</Description>
  <PackageProjectUrl>https://github.com/WSU-EIT/FreeQEMU</PackageProjectUrl>
  <RepositoryUrl>https://github.com/WSU-EIT/FreeQEMU</RepositoryUrl>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageTags>qemu;linux;vm;virtualization;dotnet;docker;cross-platform</PackageTags>
</PropertyGroup>
```

**Checklist:**
- [ ] Version is 2.0.0
- [ ] URLs point to WSU-EIT/FreeQEMU
- [ ] README.md included in package
- [ ] Tags are relevant

## Task 5.2: Build Release Package

```bash
cd v2/FreeQEMU
dotnet pack -c Release
```

**Checklist:**
- [ ] Package builds successfully
- [ ] `.nupkg` file created in bin/Release/
- [ ] Package size is reasonable (~small, QEMU binaries are in dependency)

## Task 5.3: Publish to NuGet

**Using Publisher Tool:**
```bash
cd v2/FreeQEMU.NugetClientPublisher
dotnet run
# Select option 5: Full publish
```

**Or manual:**
```bash
dotnet nuget push bin/Release/FreeQEMU.2.0.0.nupkg --api-key <KEY> --source https://api.nuget.org/v3/index.json
```

**Checklist:**
- [ ] Package uploaded successfully
- [ ] Package visible on nuget.org/packages/FreeQEMU
- [ ] Version 2.0.0 shows correctly

---

# Phase 6: Validation — Consumer Test Project

**Goal:** Prove the NuGet package works for end users

## Task 6.1: Create New Test Project

```bash
mkdir FreeQEMU.ConsumerTest
cd FreeQEMU.ConsumerTest
dotnet new console -n FreeQEMU.ConsumerTest
```

**Checklist:**
- [ ] New project created outside v2 folder
- [ ] No project references to v2/FreeQEMU

## Task 6.2: Add NuGet Package Reference

```bash
dotnet add package FreeQEMU --version 2.0.0
```

**Checklist:**
- [ ] Package restores successfully
- [ ] FreeQEMU appears in dependencies

## Task 6.3: Write Test Code

```csharp
using FreeQEMU;

Console.WriteLine("Testing FreeQEMU NuGet package...");

using var vm = new LinuxVm(VmPreset.DotNet10);
await vm.EnsureReadyAsync(new Progress<VmSetupProgress>(p => 
    Console.WriteLine($"[{p.Stage}] {p.Message}")));

var result = await vm.ExecuteAsync("dotnet --version");
Console.WriteLine($"Result: {result.Output}");
Console.WriteLine($"Success: {result.Success}");

Console.WriteLine("✓ NuGet package works!");
```

**Checklist:**
- [ ] Code compiles
- [ ] VM boots successfully
- [ ] Command executes
- [ ] Output is correct

## Task 6.4: Document Any Issues

If issues found:
- [ ] Create GitHub issue
- [ ] Fix in v2 source
- [ ] Publish patch version (2.0.1)
- [ ] Re-test consumer project

---

# Phase Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| **1. Core Migration** | 1.1-1.6 | ✅ Complete |
| **2. Publisher Tool** | 2.1-2.3 | ✅ Complete |
| **3. Examples** | 3.1-3.2 | ✅ Complete |
| **4. GitHub** | 4.1-4.3 | ⬜ Not started |
| **5. NuGet** | 5.1-5.3 | ⬜ Not started |
| **6. Validation** | 6.1-6.4 | ⬜ Not started |

---

# Tech Debt (Defer to v2.1)

Items identified but not in scope for v2.0:

| Item | Description | Priority |
|------|-------------|----------|
| Split QemuProcessManager | Extract ImageManager, SshKeyManager, CloudInitManager | P2 |
| Split Models.cs | One file per DTO | P3 |
| Unit Tests | Create FreeQEMU.Tests with xUnit | P2 |
| Multi-target | Support .NET 8, 9, 10 (currently .NET 10 only) | P3 |
| GitHub Actions | CI/CD for automated builds and releases | P2 |
| FreeGLBA Clarification | Determine if separate package needed | P1 |

---

# Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| NuGet API key exposed | High | Use user secrets, never commit |
| QEMU dependency version mismatch | Medium | Pin Mosa.Tools.Package.Qemu version |
| Breaking changes from v1 | Medium | Keep same public API surface |
| First boot slow (~3min) | Low | Document in README, expected behavior |

---

## Test Plan

- **Happy path:** Fresh install, boot VM, run command, get output
- **Edge cases:** Missing QEMU binaries, network timeout, SSH auth failure
- **Regression:** Ensure all VmPresets still work

## Ops & Rollout

- **Config/secrets:** NuGet API key via user secrets
- **Monitoring:** N/A (library, not service)
- **Rollback:** Unlist bad version on NuGet, publish patch

## Docs

- [ ] README.md complete with examples
- [ ] Quickstart works for new users
- [ ] ADR: Why we use QEMU over Docker/WSL

---

*Created: 2025-01-20*  
*Maintained by: [Quality]*
