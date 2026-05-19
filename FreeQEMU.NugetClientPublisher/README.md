# FreeQEMU NuGet Client Publisher

A command-line tool for managing FreeQEMU NuGet package publishing to NuGet.org.

## Features

- 🔒 **Dry Run Mode** - Preview operations before executing
- 📊 **Version Management** - Lookup existing versions, suggest next version
- 🚀 **One-Click Publish** - Clean → Build → Pack → Push in one step
- 🧹 **Version Trimming** - Unlist old versions to keep NuGet clean
- 🔐 **Secure API Keys** - Uses .NET User Secrets (never in source control)

## Setup

### 1. Get NuGet API Key

1. Go to https://www.nuget.org/account/apikeys
2. Create a new key with "Push" scope for "FreeQEMU" package
3. Copy the key (you can only see it once!)

### 2. Store API Key Securely

```bash
cd FreeQEMU.NugetClientPublisher
dotnet user-secrets init  # If not already done
dotnet user-secrets set "NuGet:ApiKey" "your-api-key-here"
```

### 3. Configure Settings

Edit `appsettings.json`:

```json
{
  "NuGet": {
    "PackageId": "FreeQEMU",
    "Version": "2.0.0",
    "SolutionRoot": "%USERPROFILE%\\source\\repos\\WSU-EIT\\FreeQEMU",
    "ProjectPath": "FreeQEMU\\FreeQEMU.csproj"
  }
}
```

## Usage

```bash
cd FreeQEMU.NugetClientPublisher
dotnet run
```

## Menu Options

```
═══════════════════════════════════════════════════════════════
              MENU - 🔒 DRY RUN MODE (No writes)              
═══════════════════════════════════════════════════════════════
  1. View current configuration - READ ONLY
  2. Verify project builds successfully - READ ONLY
  3. Pack NuGet package (build .nupkg)
  4. Push to NuGet.org
  5. Full publish (Clean → Build → Pack → Push)

  L. Lookup versions from NuGet.org - READ ONLY
  T. Trim/Unlist old versions from NuGet.org
  V. Change version number
  D. Toggle DRY RUN mode
  H. Help - Show documentation
  0. Exit
```

## Typical Workflow

1. **L** - Check current version on NuGet.org
2. **V** - Set new version (or accept suggested)
3. **5** - Run full publish in DRY RUN mode (verify)
4. **D** - Toggle to LIVE mode
5. **5** - Run full publish for real
6. **T** - (Optional) Trim old versions

## Version Guidelines

The tool enforces semantic versioning:

| Change Type | Version Bump | When to Use |
|-------------|--------------|-------------|
| **MAJOR** (X.0.0) | Breaking changes | API changes that break existing code |
| **MINOR** (0.X.0) | New features | New features, minor breaking changes |
| **PATCH** (0.0.X) | Bug fixes | Bug fixes, no breaking changes |

## Safety Features

### Dry Run Mode

The tool starts in **DRY RUN** mode by default:
- Shows what WOULD happen
- No packages are pushed
- No versions are unlisted
- Toggle with 'D' to go live

### Version Validation

Before publishing, the tool:
- Fetches all versions from NuGet.org
- Validates your version is newer
- Suggests the next patch version
- Prevents accidental duplicate publishing

### Duplicate Detection

Even with `--skip-duplicate`, the tool:
- Detects when a version already exists
- Shows a clear warning message
- Prompts you to increment version

## Trimming Old Versions

The **T** option unlists old versions:

1. Fetches all versions from NuGet.org
2. Groups by Major.Minor (e.g., 1.0.x, 1.1.x)
3. Asks how many to KEEP per group
4. Unlists the rest

**Note**: Unlisting hides packages from search but doesn't delete them. Existing projects can still restore unlisted versions.

## Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Configuration (paths, version, settings) |
| `secrets.json` | API key (in User Secrets, not checked in) |

## Environment Variables

The `SolutionRoot` setting supports environment variables:

```json
"SolutionRoot": "%USERPROFILE%\\source\\repos\\WSU-EIT\\FreeQEMU"
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "API Key not configured" | Run `dotnet user-secrets set "NuGet:ApiKey" "..."` |
| "Version already exists" | Use 'L' to check, 'V' to set new version |
| "Could not determine solution root" | Set `SolutionRoot` in appsettings.json |
| Push fails silently | Check API key has "Push" permission for the package |

## See Also

- [FreeQEMU README](../FreeQEMU/README.md) - Package documentation
- [NuGet.org Package](https://www.nuget.org/packages/FreeQEMU) - Published package

Part of the FreeQEMU solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
