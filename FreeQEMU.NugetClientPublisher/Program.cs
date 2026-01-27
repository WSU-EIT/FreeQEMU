using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace FreeQEMU.NugetClientPublisher;

internal class Program
{
    private static NuGetConfig _config = new();
    private static bool _dryRun = true;
    private static readonly HttpClient _httpClient = new();

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           FreeQEMU NuGet Package Publisher                   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<Program>(optional: true)
            .AddCommandLine(args)
            .Build();

        configuration.GetSection("NuGet").Bind(_config);

        // Allow command line overrides
        if (args.Length > 0 && args[0] == "--version" && args.Length > 1)
        {
            _config.Version = args[1];
        }

        try
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("                       CONFIGURATION                           ");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"  Package ID:    {_config.PackageId}");
            Console.WriteLine($"  Version:       {_config.Version}");
            Console.WriteLine($"  Configuration: {_config.Configuration}");
            Console.WriteLine($"  Source:        {_config.Source}");
            Console.WriteLine($"  Project:       {_config.ProjectPath}");
            Console.WriteLine($"  API Key:       {(string.IsNullOrWhiteSpace(_config.ApiKey) ? "❌ NOT CONFIGURED" : "✓ Configured (hidden)")}");
            Console.WriteLine();

            // Validate project path
            var projectPath = ResolveProjectPath();
            if (projectPath == null)
            {
                return 1;
            }

            // Show menu
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                DisplayModeHeader();
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  1. View current configuration - READ ONLY");
                Console.WriteLine("  2. Verify project builds successfully - READ ONLY");
                Console.WriteLine("  3. Pack NuGet package (build .nupkg)");
                Console.WriteLine("  4. Push to NuGet.org");
                Console.WriteLine("  5. Full publish (Clean → Build → Pack → Push)");
                Console.WriteLine();
                Console.WriteLine("  L. Lookup versions from NuGet.org - READ ONLY");
                Console.WriteLine("  T. Trim/Unlist old versions from NuGet.org");
                Console.WriteLine("  V. Change version number");
                Console.WriteLine("  D. Toggle DRY RUN mode");
                Console.WriteLine("  H. Help - Show documentation");
                Console.WriteLine("  0. Exit");
                Console.WriteLine();
                Console.Write("Select option: ");

                var key = Console.ReadKey();
                Console.WriteLine();
                Console.WriteLine();

                switch (char.ToUpper(key.KeyChar))
                {
                    case '1':
                        await ViewConfiguration();
                        break;
                    case '2':
                        await VerifyBuild();
                        break;
                    case '3':
                        await PackNuGet();
                        break;
                    case '4':
                        await PushToNuGet();
                        break;
                    case '5':
                        await FullPublish();
                        break;
                    case 'L':
                        await LookupNuGetVersions();
                        break;
                    case 'T':
                        await TrimOldVersions();
                        break;
                    case 'V':
                        ChangeVersion();
                        break;
                    case 'D':
                        ToggleDryRunMode();
                        break;
                    case 'H':
                        ShowHelp();
                        break;
                    case '0':
                        Console.WriteLine("Exiting...");
                        return 0;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
            Console.WriteLine();
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return 1;
        }
    }

    #region Menu Display Helpers

    private static void DisplayModeHeader()
    {
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("              MENU - 🔒 DRY RUN MODE (No writes)              ");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("              MENU - ⚠️  LIVE MODE (Will publish!)             ");
            Console.ResetColor();
        }
    }

    private static void ToggleDryRunMode()
    {
        _dryRun = !_dryRun;
        Console.WriteLine();
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🔒 DRY RUN MODE ENABLED - No packages will be pushed.");
            Console.WriteLine("   Operations will show what WOULD happen.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("⚠️  LIVE MODE ENABLED - Packages WILL be pushed to NuGet.org!");
            Console.WriteLine("   Are you sure? Press 'D' again to switch back to Dry Run.");
            Console.ResetColor();
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                           HELP                                ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  WHAT IS THIS TOOL?");
        Console.ResetColor();
        Console.WriteLine("  ──────────────────────────────────────────────────────────────");
        Console.WriteLine("  A command-line tool for managing NuGet package publishing");
        Console.WriteLine("  for the FreeQEMU package to NuGet.org.");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  MENU OPTIONS");
        Console.ResetColor();
        Console.WriteLine("  ──────────────────────────────────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("  READ-ONLY (Safe):");
        Console.WriteLine("    1  View configuration    - Shows current settings and paths");
        Console.WriteLine("    2  Verify build          - Tests if project compiles");
        Console.WriteLine("    L  Lookup versions       - Shows all versions on NuGet.org");
        Console.WriteLine("    H  Help                  - This screen");
        Console.WriteLine();
        Console.WriteLine("  WRITE OPERATIONS (Respects Dry Run Mode):");
        Console.WriteLine("    3  Pack                  - Build and create .nupkg file");
        Console.WriteLine("    4  Push                  - Upload .nupkg to NuGet.org");
        Console.WriteLine("    5  Full publish          - Clean → Build → Pack → Push");
        Console.WriteLine("    T  Trim versions         - Unlist old versions from NuGet.org");
        Console.WriteLine();
        Console.WriteLine("  CONFIGURATION:");
        Console.WriteLine("    V  Change version        - Set version for this session");
        Console.WriteLine("    D  Toggle dry run        - Switch between DRY RUN and LIVE mode");
        Console.WriteLine("    0  Exit                  - Quit the application");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  DRY RUN MODE");
        Console.ResetColor();
        Console.WriteLine("  ──────────────────────────────────────────────────────────────");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  • Tool starts in DRY RUN mode (safe)");
        Console.ResetColor();
        Console.WriteLine("  • Shows what WOULD happen without making changes");
        Console.WriteLine("  • Press D to toggle to LIVE mode when ready");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  • LIVE mode will actually push to NuGet.org!");
        Console.ResetColor();
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  SEMANTIC VERSIONING (MAJOR.MINOR.PATCH)");
        Console.ResetColor();
        Console.WriteLine("  ──────────────────────────────────────────────────────────────");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  MAJOR (X.0.0) - Full breaking changes");
        Console.WriteLine("                  Existing code WILL break");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  MINOR (0.X.0) - Limited breaking changes");
        Console.WriteLine("                  New features, some code MAY need updates");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  PATCH (0.0.X) - Non-breaking changes");
        Console.WriteLine("                  Bug fixes, existing code will NOT break");
        Console.ResetColor();
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  FIRST-TIME SETUP");
        Console.ResetColor();
        Console.WriteLine("  ──────────────────────────────────────────────────────────────");
        Console.WriteLine("  1. Get API key from: https://www.nuget.org/account/apikeys");
        Console.WriteLine("  2. Run these commands in the project folder:");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("     dotnet user-secrets init");
        Console.WriteLine("     dotnet user-secrets set \"NuGet:ApiKey\" \"your-key-here\"");
        Console.ResetColor();
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  TYPICAL WORKFLOW");
        Console.ResetColor();
        Console.WriteLine("  ──────────────────────────────────────────────────────────────");
        Console.WriteLine("  1. Press L to check current version on NuGet.org");
        Console.WriteLine("  2. Press V to set a new version (or accept suggested)");
        Console.WriteLine("  3. Press 5 to do full publish (in DRY RUN first!)");
        Console.WriteLine("  4. Press D to switch to LIVE mode");
        Console.WriteLine("  5. Press 5 again to actually publish");
        Console.WriteLine("  6. Optionally press T to trim old versions");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  CONFIGURATION FILE");
        Console.ResetColor();
        Console.WriteLine("  ──────────────────────────────────────────────────────────────");
        Console.WriteLine("  Settings are in: appsettings.json");
        Console.WriteLine("  API key should be in: user-secrets (not appsettings.json!)");
        Console.WriteLine();
        Console.WriteLine("  See README.md for full documentation.");
        Console.WriteLine();
        
        Console.Write("  Press any key to return to menu...");
        Console.ReadKey();
    }

    private static void ChangeVersion()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                      CHANGE VERSION                           ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine($"  Current version: {_config.Version}");
        Console.WriteLine();
        Console.Write("  Enter new version (e.g., 1.0.1): ");
        var newVersion = Console.ReadLine()?.Trim();

        if (!string.IsNullOrWhiteSpace(newVersion))
        {
            _config.Version = newVersion;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Version updated to: {_config.Version}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Version unchanged.");
            Console.ResetColor();
        }
    }

    #endregion

    #region NuGet Version Lookup & Trim

    private static async Task<List<NuGetVersionInfo>> FetchNuGetVersionsAsync()
    {
        var versions = new List<NuGetVersionInfo>();
        
        // NuGet V3 API - Get package registration
        var registrationUrl = $"https://api.nuget.org/v3/registration5-semver1/{_config.PackageId.ToLowerInvariant()}/index.json";
        
        var response = await _httpClient.GetAsync(registrationUrl);
        
        if (!response.IsSuccessStatusCode)
        {
            return versions;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("items", out var items))
        {
            foreach (var page in items.EnumerateArray())
            {
                if (page.TryGetProperty("items", out var pageItems))
                {
                    foreach (var item in pageItems.EnumerateArray())
                    {
                        var versionInfo = ExtractVersionInfo(item);
                        if (versionInfo != null)
                        {
                            versions.Add(versionInfo);
                        }
                    }
                }
            }
        }

        return versions.OrderByDescending(v => v.Version).ToList();
    }

    private static NuGetVersionInfo? ExtractVersionInfo(JsonElement item)
    {
        try
        {
            if (!item.TryGetProperty("catalogEntry", out var catalogEntry))
                return null;

            var versionStr = catalogEntry.GetProperty("version").GetString();
            if (string.IsNullOrEmpty(versionStr))
                return null;

            var version = ParseVersion(versionStr);
            if (version == null)
                return null;

            DateTime? published = null;
            if (catalogEntry.TryGetProperty("published", out var publishedElement))
            {
                var publishedStr = publishedElement.GetString();
                if (DateTime.TryParse(publishedStr, out var parsedDate))
                    published = parsedDate;
            }

            bool listed = true;
            if (catalogEntry.TryGetProperty("listed", out var listedElement))
                listed = listedElement.GetBoolean();

            return new NuGetVersionInfo
            {
                Version = version,
                VersionString = versionStr,
                Published = published,
                Listed = listed
            };
        }
        catch
        {
            return null;
        }
    }

    private static Version? ParseVersion(string versionStr)
    {
        var cleanVersion = versionStr.Split('-')[0]; // Remove prerelease suffix
        if (Version.TryParse(cleanVersion, out var version))
            return version;
        return null;
    }

    private static string SuggestNextVersion(Version currentVersion)
    {
        return $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build + 1}";
    }

    private static async Task LookupNuGetVersions()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                 LOOKUP NUGET.ORG VERSIONS                     ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine($"  Package: {_config.PackageId}");
        Console.WriteLine();
        Console.WriteLine("  Fetching from NuGet.org...");

        try
        {
            var versions = await FetchNuGetVersionsAsync();

            if (versions.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine($"  ⚠ Package '{_config.PackageId}' not found or has no versions.");
                Console.WriteLine("    This could mean it hasn't been published yet.");
                Console.ResetColor();
                return;
            }

            var latestVersion = versions.First();
            var listedVersions = versions.Where(v => v.Listed).ToList();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Latest version on NuGet.org: {latestVersion.VersionString}");
            Console.ResetColor();
            Console.WriteLine($"    Total versions: {versions.Count} ({listedVersions.Count} listed, {versions.Count - listedVersions.Count} unlisted)");

            // Compare with configured version
            Console.WriteLine();
            var configuredVersion = ParseVersion(_config.Version);
            if (_config.Version == latestVersion.VersionString)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⚠ Your configured version ({_config.Version}) matches the latest!");
                Console.WriteLine("    You may need to increment the version before publishing.");
                Console.ResetColor();
            }
            else if (configuredVersion != null && configuredVersion > latestVersion.Version)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ✓ Your configured version ({_config.Version}) is NEWER than latest.");
                Console.WriteLine("    Ready to publish!");
                Console.ResetColor();
            }
            else if (configuredVersion != null && configuredVersion < latestVersion.Version)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ⚠ Your configured version ({_config.Version}) is OLDER than latest!");
                Console.WriteLine("    You should update your version number.");
                Console.ResetColor();
            }

            // Group versions by Major.Minor
            var versionGroups = versions
                .GroupBy(v => $"{v.Version.Major}.{v.Version.Minor}")
                .OrderByDescending(g => ParseVersion(g.Key + ".0"))
                .ToList();

            Console.WriteLine();
            Console.WriteLine("  ┌──────────────────┬──────────────────────────┬─────────┐");
            Console.WriteLine("  │ Version          │ Published                │ Status  │");
            Console.WriteLine("  ├──────────────────┼──────────────────────────┼─────────┤");

            foreach (var v in versions.Take(15))
            {
                var publishedStr = v.Published?.ToString("yyyy-MM-dd HH:mm") ?? "Unknown";
                var statusStr = v.Listed ? "Listed" : "Unlisted";
                var statusColor = v.Listed ? ConsoleColor.Green : ConsoleColor.DarkGray;
                
                Console.Write($"  │ {v.VersionString.PadRight(16)} │ {publishedStr.PadRight(24)} │ ");
                Console.ForegroundColor = statusColor;
                Console.Write($"{statusStr.PadRight(7)}");
                Console.ResetColor();
                Console.WriteLine(" │");
            }

            if (versions.Count > 15)
            {
                Console.WriteLine($"  │ ... and {(versions.Count - 15).ToString().PadLeft(3)} more │                          │         │");
            }

            Console.WriteLine("  └──────────────────┴──────────────────────────┴─────────┘");

            // Show version groups summary
            Console.WriteLine();
            Console.WriteLine("  VERSION GROUPS (Major.Minor):");
            foreach (var group in versionGroups.Take(5))
            {
                var groupVersions = group.ToList();
                var listedCount = groupVersions.Count(v => v.Listed);
                Console.WriteLine($"    {group.Key}.x: {groupVersions.Count} versions ({listedCount} listed)");
            }

            // Suggest next version
            Console.WriteLine();
            var suggestedVersion = SuggestNextVersion(latestVersion.Version);
            Console.WriteLine($"  Suggested next version: {suggestedVersion}");
            Console.WriteLine();
            Console.Write("  Would you like to set this as your version? (y/N): ");
            var key = Console.ReadKey();
            Console.WriteLine();

            if (char.ToUpper(key.KeyChar) == 'Y')
            {
                _config.Version = suggestedVersion;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Version updated to: {_config.Version}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine($"  View on NuGet.org: https://www.nuget.org/packages/{_config.PackageId}");
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Network error: {ex.Message}");
            Console.ResetColor();
        }
        catch (JsonException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Failed to parse NuGet response: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task TrimOldVersions()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("           TRIM OLD VERSIONS (DRY RUN - Preview only)          ");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("                   TRIM OLD VERSIONS                           ");
            Console.ResetColor();
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Validate API key
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ ERROR: NuGet API key not configured!");
            Console.WriteLine("    Cannot unlist versions without an API key.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"  Package: {_config.PackageId}");
        Console.WriteLine();
        Console.WriteLine("  Fetching versions from NuGet.org...");

        try
        {
            var versions = await FetchNuGetVersionsAsync();
            var listedVersions = versions.Where(v => v.Listed).ToList();

            if (listedVersions.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine("  ⚠ No listed versions found.");
                Console.ResetColor();
                return;
            }

            // Group by Major.Minor
            var versionGroups = listedVersions
                .GroupBy(v => $"{v.Version.Major}.{v.Version.Minor}")
                .OrderByDescending(g => ParseVersion(g.Key + ".0"))
                .ToList();

            Console.WriteLine();
            Console.WriteLine($"  Found {listedVersions.Count} listed versions in {versionGroups.Count} groups:");
            Console.WriteLine();

            foreach (var group in versionGroups)
            {
                var groupVersions = group.OrderByDescending(v => v.Version).ToList();
                Console.WriteLine($"    {group.Key}.x: {groupVersions.Count} versions");
                foreach (var v in groupVersions.Take(5))
                {
                    Console.WriteLine($"      - {v.VersionString}");
                }
                if (groupVersions.Count > 5)
                {
                    Console.WriteLine($"      ... and {groupVersions.Count - 5} more");
                }
            }

            Console.WriteLine();
            Console.WriteLine("  ═══════════════════════════════════════════════════════════");
            Console.WriteLine("  TRIM OPTIONS:");
            Console.WriteLine("  ═══════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("  How many versions to KEEP per Major.Minor group?");
            Console.WriteLine("    (e.g., keeping 3 means 1.0.5, 1.0.4, 1.0.3 stay listed,");
            Console.WriteLine("     1.0.2, 1.0.1, 1.0.0 get unlisted)");
            Console.WriteLine();
            Console.WriteLine("  Enter number to keep (1-10), or 0 to cancel: ");
            Console.Write("  Keep: ");
            
            var input = Console.ReadLine()?.Trim();
            if (!int.TryParse(input, out var keepCount) || keepCount < 1 || keepCount > 10)
            {
                Console.WriteLine("  Cancelled.");
                return;
            }

            // Calculate what will be unlisted
            var toUnlist = new List<NuGetVersionInfo>();
            foreach (var group in versionGroups)
            {
                var groupVersions = group.OrderByDescending(v => v.Version).ToList();
                toUnlist.AddRange(groupVersions.Skip(keepCount));
            }

            if (toUnlist.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine($"  ✓ No versions to unlist! All groups have {keepCount} or fewer versions.");
                Console.ResetColor();
                return;
            }

            // Show what will be unlisted
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  The following {toUnlist.Count} versions will be UNLISTED:");
            Console.ResetColor();
            Console.WriteLine();

            foreach (var group in versionGroups)
            {
                var groupVersions = group.OrderByDescending(v => v.Version).ToList();
                var groupToUnlist = groupVersions.Skip(keepCount).ToList();
                
                if (groupToUnlist.Count > 0)
                {
                    Console.WriteLine($"    {group.Key}.x:");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"      KEEP:   ");
                    Console.WriteLine(string.Join(", ", groupVersions.Take(keepCount).Select(v => v.VersionString)));
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"      UNLIST: ");
                    Console.WriteLine(string.Join(", ", groupToUnlist.Select(v => v.VersionString)));
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ⚠ NOTE: Unlisting hides packages from search but does NOT delete them.");
            Console.WriteLine("    Existing projects depending on these versions can still restore them.");
            Console.ResetColor();

            if (_dryRun)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  [DRY RUN] Would unlist the above versions.");
                Console.WriteLine("  ✓ Dry run complete - no changes made.");
                Console.ResetColor();
                return;
            }

            // Confirm
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"  ⚠ Unlist {toUnlist.Count} versions? This cannot be easily undone! (y/N): ");
            Console.ResetColor();
            var confirm = Console.ReadKey();
            Console.WriteLine();

            if (char.ToUpper(confirm.KeyChar) != 'Y')
            {
                Console.WriteLine("  Cancelled.");
                return;
            }

            // Unlist each version
            Console.WriteLine();
            Console.WriteLine("  Unlisting versions...");
            Console.WriteLine();
            
            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            foreach (var version in toUnlist)
            {
                Console.Write($"    {version.VersionString}... ");
                
                var (success, error) = await UnlistVersionAsync(version.VersionString);
                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Unlisted");
                    Console.ResetColor();
                    successCount++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ Failed");
                    Console.ResetColor();
                    failCount++;
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        errors.Add($"{version.VersionString}: {error}");
                    }
                }
            }

            Console.WriteLine();
            if (failCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Successfully unlisted {successCount} versions!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Completed: {successCount} unlisted, {failCount} failed.");
                Console.ResetColor();
                
                if (errors.Count > 0)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  ERRORS:");
                    foreach (var err in errors.Take(5)) // Show first 5 errors
                    {
                        // Truncate long error messages
                        var shortErr = err.Length > 100 ? err.Substring(0, 100) + "..." : err;
                        Console.WriteLine($"    {shortErr}");
                    }
                    Console.ResetColor();
                    
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  POSSIBLE CAUSES:");
                    Console.WriteLine("    • API key may not have 'Unlist' permission for this package");
                    Console.WriteLine("    • API key may have expired");
                    Console.WriteLine("    • Package may already be unlisted");
                    Console.WriteLine("    • NuGet.org may be experiencing issues");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task<(bool Success, string? Error)> UnlistVersionAsync(string version)
    {
        try
        {
            // NuGet.org uses "https://api.nuget.org/v3/index.json" for push but needs
            // the package source for delete operations.
            // NOTE: dotnet nuget delete often warns when using V3 source as it redirects to V2.
            var deleteSource = "https://api.nuget.org/v3/index.json";
            
            // Use dotnet nuget delete to unlist (doesn't actually delete on nuget.org, just unlists)
            var args = $"nuget delete {_config.PackageId} {version} --source {deleteSource} --api-key {_config.ApiKey} --non-interactive";
            
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return (false, "Failed to start process");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;
            
            // Allow exit code 0 (success)
            if (process.ExitCode == 0)
            {
                return (true, null);
            }

            // Combine output and error for analysis
            var fullOutput = $"{output}\n{error}".Trim();

            // Check for specific success scenarios that look like failures
            // 1. Package already unlisted/deleted often returns exit code 1 with a specific message
            if (fullOutput.Contains("does not exist", StringComparison.OrdinalIgnoreCase) || 
                fullOutput.Contains("404", StringComparison.OrdinalIgnoreCase) ||
                fullOutput.Contains("already unlisted", StringComparison.OrdinalIgnoreCase))
            {
                return (true, "Package was likely already unlisted.");
            }

            return (false, fullOutput);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Validates that the configured version is newer than what's on NuGet.org.
    /// Returns true if version is valid (newer), false if it would fail.
    /// </summary>
    private static async Task<bool> ValidateVersionAsync(bool showFullError = true)
    {
        Console.WriteLine("  Checking version against NuGet.org...");
        
        try
        {
            var versions = await FetchNuGetVersionsAsync();
            
            if (versions.Count == 0)
            {
                // No versions on NuGet yet - any version is fine
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No existing versions on NuGet.org - ready to publish first version!");
                Console.ResetColor();
                return true;
            }

            var latestVersion = versions.First();
            var configuredVersion = ParseVersion(_config.Version);

            if (configuredVersion == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ ERROR: Could not parse configured version: {_config.Version}");
                Console.ResetColor();
                return false;
            }

            // Check if our version is newer
            if (configuredVersion > latestVersion.Version)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Version {_config.Version} is newer than latest ({latestVersion.VersionString}) - ready to publish!");
                Console.ResetColor();
                return true;
            }

            // Version is not newer - show error
            var suggestedVersion = SuggestNextVersion(latestVersion.Version);
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║                    VERSION ERROR                           ║");
            Console.WriteLine("  ╚════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"  Your version:   {_config.Version}");
            Console.WriteLine($"  Latest on NuGet: {latestVersion.VersionString}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Your version must be GREATER than {latestVersion.VersionString}");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  Suggested next version: {suggestedVersion}");
            Console.ResetColor();

            if (showFullError)
            {
                Console.WriteLine();
                Console.WriteLine("  ┌────────────────────────────────────────────────────────────┐");
                Console.WriteLine("  │              SEMANTIC VERSIONING GUIDE                     │");
                Console.WriteLine("  ├────────────────────────────────────────────────────────────┤");
                Console.WriteLine("  │                                                            │");
                Console.WriteLine("  │  Version format: MAJOR.MINOR.PATCH (e.g., 1.2.3)           │");
                Console.WriteLine("  │                                                            │");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  │  MAJOR (X.0.0) - Full breaking changes                     │");
                Console.WriteLine("  │    • Incompatible API changes                              │");
                Console.WriteLine("  │    • Existing code WILL break                              │");
                Console.ResetColor();
                Console.WriteLine("  │                                                            │");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  │  MINOR (0.X.0) - Limited breaking changes                  │");
                Console.WriteLine("  │    • New features added                                    │");
                Console.WriteLine("  │    • Some existing code MAY need updates                   │");
                Console.ResetColor();
                Console.WriteLine("  │                                                            │");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  │  PATCH (0.0.X) - Non-breaking changes                      │");
                Console.WriteLine("  │    • Bug fixes, minor improvements                         │");
                Console.WriteLine("  │    • Existing code will NOT break                          │");
                Console.ResetColor();
                Console.WriteLine("  │                                                            │");
                Console.WriteLine("  └────────────────────────────────────────────────────────────┘");
            }

            Console.WriteLine();
            Console.Write("  Would you like to update to the suggested version? (y/N): ");
            var key = Console.ReadKey();
            Console.WriteLine();

            if (char.ToUpper(key.KeyChar) == 'Y')
            {
                _config.Version = suggestedVersion;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Version updated to: {_config.Version}");
                Console.ResetColor();
                return true; // Now valid
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ Could not verify version against NuGet.org: {ex.Message}");
            Console.WriteLine("    Proceeding anyway - NuGet.org will reject if version exists.");
            Console.ResetColor();
            return true; // Let NuGet handle the error
        }
    }

    #endregion

    #region Menu Actions

    private static async Task ViewConfiguration()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                    CURRENT CONFIGURATION                      ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        var projectPath = ResolveProjectPath();

        Console.WriteLine("  ┌──────────────────────┬──────────────────────────────────────────────┐");
        Console.WriteLine("  │ Setting              │ Value                                        │");
        Console.WriteLine("  ├──────────────────────┼──────────────────────────────────────────────┤");
        Console.WriteLine($"  │ Package ID           │ {_config.PackageId.PadRight(44)} │");
        Console.WriteLine($"  │ Version              │ {_config.Version.PadRight(44)} │");
        Console.WriteLine($"  │ Configuration        │ {_config.Configuration.PadRight(44)} │");
        Console.WriteLine($"  │ Source               │ {TruncateString(_config.Source, 44).PadRight(44)} │");
        Console.WriteLine($"  │ Skip Duplicate       │ {_config.SkipDuplicate.ToString().PadRight(44)} │");
        Console.WriteLine($"  │ Include Symbols      │ {_config.IncludeSymbols.ToString().PadRight(44)} │");
        Console.WriteLine("  ├──────────────────────┼──────────────────────────────────────────────┤");

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  │ API Key              │ {"❌ NOT CONFIGURED".PadRight(44)} │");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  │ API Key              │ {"✓ Configured (hidden)".PadRight(44)} │");
            Console.ResetColor();
        }

        Console.WriteLine("  └──────────────────────┴──────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("  PROJECT PATH:");
        if (projectPath != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    ✓ {projectPath}");
            Console.ResetColor();

            // Check if package already exists
            var projectDir = Path.GetDirectoryName(projectPath)!;
            var outputDir = Path.Combine(projectDir, "bin", _config.Configuration);
            var packageFileName = $"{_config.PackageId}.{_config.Version}.nupkg";
            var existingPackage = FindPackage(outputDir, packageFileName);
            
            if (existingPackage != null)
            {
                Console.WriteLine();
                Console.WriteLine("  EXISTING PACKAGE:");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"    ⚠ {existingPackage}");
                Console.WriteLine($"      Size: {new FileInfo(existingPackage).Length / 1024.0:F1} KB");
                Console.WriteLine($"      Modified: {File.GetLastWriteTime(existingPackage):yyyy-MM-dd HH:mm:ss}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"    ✗ Project not found: {_config.ProjectPath}");
            Console.ResetColor();
        }

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ⚠ API KEY NOT SET!");
            Console.WriteLine("    Run: dotnet user-secrets set \"NuGet:ApiKey\" \"your-key-here\"");
            Console.ResetColor();
        }

        await Task.CompletedTask;
    }

    private static async Task VerifyBuild()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                      VERIFY BUILD                             ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        var projectPath = ResolveProjectPath();
        if (projectPath == null) return;

        var projectDir = Path.GetDirectoryName(projectPath)!;

        Console.WriteLine("  Step 1: Restoring packages...");
        var (restoreSuccess, _) = await RunCommandAsync("dotnet", $"restore \"{projectPath}\"", projectDir);
        if (!restoreSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Restore failed");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Restore completed");
        Console.ResetColor();

        Console.WriteLine();
        Console.WriteLine("  Step 2: Building project...");
        var (buildSuccess, _) = await RunCommandAsync("dotnet", $"build \"{projectPath}\" -c {_config.Configuration} --no-restore", projectDir);
        if (!buildSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Build failed");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Build completed successfully!");
        Console.ResetColor();
    }

    private static async Task PackNuGet()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("              PACK NUGET (DRY RUN - Preview only)              ");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("                      PACK NUGET PACKAGE                       ");
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        var projectPath = ResolveProjectPath();
        if (projectPath == null) return;

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var outputDir = Path.Combine(projectDir, "bin", _config.Configuration);

        Console.WriteLine($"  Package: {_config.PackageId}");
        Console.WriteLine($"  Version: {_config.Version}");
        Console.WriteLine();

        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  [DRY RUN] Would execute:");
            Console.WriteLine($"    dotnet clean \"{projectPath}\" -c {_config.Configuration}");
            Console.WriteLine($"    dotnet restore \"{projectPath}\"");
            Console.WriteLine($"    dotnet pack \"{projectPath}\" -c {_config.Configuration} -p:Version={_config.Version}");
            Console.WriteLine();
            Console.WriteLine("  ✓ Dry run complete - no changes made.");
            Console.ResetColor();
            return;
        }

        // Clean
        Console.WriteLine("  Step 1: Cleaning...");
        await RunCommandAsync("dotnet", $"clean \"{projectPath}\" -c {_config.Configuration}", projectDir);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Clean completed");
        Console.ResetColor();

        // Restore
        Console.WriteLine();
        Console.WriteLine("  Step 2: Restoring...");
        var (restoreSuccess, _) = await RunCommandAsync("dotnet", $"restore \"{projectPath}\"", projectDir);
        if (!restoreSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Restore failed");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Restore completed");
        Console.ResetColor();

        // Build
        Console.WriteLine();
        Console.WriteLine("  Step 3: Building...");
        var (buildSuccess, _) = await RunCommandAsync("dotnet", $"build \"{projectPath}\" -c {_config.Configuration} --no-restore", projectDir);
        if (!buildSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Build failed");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Build completed");
        Console.ResetColor();

        // Pack
        Console.WriteLine();
        Console.WriteLine("  Step 4: Packing...");
        var packArgs = $"pack \"{projectPath}\" -c {_config.Configuration} -p:Version={_config.Version} --no-build";
        if (_config.IncludeSymbols)
        {
            packArgs += " --include-symbols -p:SymbolPackageFormat=snupkg";
        }
        var (packSuccess, _) = await RunCommandAsync("dotnet", packArgs, projectDir, hideOutput: false);
        if (!packSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Pack failed");
            Console.ResetColor();
            return;
        }

        // Show result
        var packageFileName = $"{_config.PackageId}.{_config.Version}.nupkg";
        var packagePath = FindPackage(outputDir, packageFileName);
        if (packagePath != null)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Package created: {packagePath}");
            Console.WriteLine($"    Size: {new FileInfo(packagePath).Length / 1024.0:F1} KB");
            Console.ResetColor();

            // Analyze package contents to diagnose potential duplicate file issues
            AnalyzePackageContents(packagePath);
        }
    }

    private static async Task PushToNuGet()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("              PUSH TO NUGET (DRY RUN - Preview only)           ");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("                    PUSH TO NUGET.ORG                          ");
            Console.ResetColor();
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Validate API key
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ ERROR: NuGet API key not configured!");
            Console.WriteLine();
            Console.WriteLine("  Please set the API key using user secrets:");
            Console.WriteLine("    dotnet user-secrets set \"NuGet:ApiKey\" \"your-api-key-here\"");
            Console.ResetColor();
            return;
        }

        // Validate version against NuGet.org FIRST
        if (!await ValidateVersionAsync(showFullError: true))
        {
            return;
        }

        var projectPath = ResolveProjectPath();
        if (projectPath == null) return;

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var outputDir = Path.Combine(projectDir, "bin", _config.Configuration);
        var packageFileName = $"{_config.PackageId}.{_config.Version}.nupkg";
        var packagePath = FindPackage(outputDir, packageFileName);

        if (packagePath == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Package not found: {packageFileName}");
            Console.WriteLine("    Run option 3 (Pack) first to create the package.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"  Package: {packagePath}");
        Console.WriteLine($"  Size: {new FileInfo(packagePath).Length / 1024.0:F1} KB");
        Console.WriteLine($"  Destination: {_config.Source}");
        Console.WriteLine();

        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  [DRY RUN] Would execute:");
            Console.WriteLine($"    dotnet nuget push \"{packagePath}\"");
            Console.WriteLine($"      --api-key ***API-KEY***");
            Console.WriteLine($"      --source {_config.Source}");
            if (_config.SkipDuplicate) Console.WriteLine($"      --skip-duplicate");
            Console.WriteLine();
            Console.WriteLine("  ✓ Dry run complete - no packages pushed.");
            Console.ResetColor();
            return;
        }

        // Confirm before pushing
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  ⚠ This will publish to NuGet.org. Continue? (y/N): ");
        Console.ResetColor();
        var confirm = Console.ReadKey();
        Console.WriteLine();

        if (char.ToUpper(confirm.KeyChar) != 'Y')
        {
            Console.WriteLine("  Cancelled.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("  Pushing to NuGet.org...");

        var pushArgs = $"nuget push \"{packagePath}\" --api-key {_config.ApiKey} --source {_config.Source}";
        if (_config.SkipDuplicate)
        {
            pushArgs += " --skip-duplicate";
        }

        var (pushSuccess, pushOutput) = await RunCommandAsync("dotnet", pushArgs, projectDir, hideOutput: false);
        
        bool wasDuplicate = false;
        if (pushSuccess)
        {
            // With --skip-duplicate, exit code is 0 (success) but output indicates conflict
            if (pushOutput.Contains("already exists", StringComparison.OrdinalIgnoreCase) || 
                pushOutput.Contains("Conflict", StringComparison.OrdinalIgnoreCase))
            {
                wasDuplicate = true;
            }
        }

        if (!pushSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Push failed");
            Console.ResetColor();
            return;
        }

        if (wasDuplicate)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  SKIPPED (DUPLICATE)                         ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"  ⚠️ Version {_config.Version} already exists on NuGet.org!");
            Console.WriteLine("     The package was NOT published because it conflicts with an existing version.");
            Console.WriteLine();
            Console.WriteLine("  POSSIBLE CAUSES:");
            Console.WriteLine("    1. Version was pushed previously (check unlisted versions)");
            Console.WriteLine("    2. Validation is still processing");
            Console.WriteLine();
            Console.WriteLine("  SOLUTION:");
            Console.WriteLine("    • Increment the version number (e.g., to 1.0.4)");
            Console.WriteLine("    • Use option 'V' in the menu to change version");
            return;
        }

        // Push symbols if they exist
        if (_config.IncludeSymbols)
        {
            var symbolsFileName = $"{_config.PackageId}.{_config.Version}.snupkg";
            var symbolsPath = FindPackage(outputDir, symbolsFileName);
            if (symbolsPath != null)
            {
                Console.WriteLine();
                Console.WriteLine("  Pushing symbols package...");
                var symbolsPushArgs = $"nuget push \"{symbolsPath}\" --api-key {_config.ApiKey} --source {_config.Source}";
                if (_config.SkipDuplicate)
                {
                    symbolsPushArgs += " --skip-duplicate";
                }
                await RunCommandAsync("dotnet", symbolsPushArgs, projectDir, hideOutput: false);
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         SUCCESS!                             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"  Package {_config.PackageId} v{_config.Version} published to NuGet.org!");
        Console.WriteLine($"  View at: https://www.nuget.org/packages/{_config.PackageId}/{_config.Version}");
        Console.WriteLine();
        Console.WriteLine("  Note: It may take a few minutes for the package to be indexed.");
    }

    private static async Task FullPublish()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("           FULL PUBLISH (DRY RUN - Preview only)               ");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("                      FULL PUBLISH                             ");
            Console.ResetColor();
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine($"  This will: Clean → Build → Pack → Push");
        Console.WriteLine($"  Package: {_config.PackageId} v{_config.Version}");
        Console.WriteLine();

        // Validate API key first
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ ERROR: NuGet API key not configured!");
            Console.ResetColor();
            return;
        }

        // Validate version against NuGet.org BEFORE doing any work
        if (!await ValidateVersionAsync(showFullError: true))
        {
            return;
        }

        if (!_dryRun)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  ⚠ This will publish to NuGet.org. Continue? (y/N): ");
            Console.ResetColor();
            var confirm = Console.ReadKey();
            Console.WriteLine();

            if (char.ToUpper(confirm.KeyChar) != 'Y')
            {
                Console.WriteLine("  Cancelled.");
                return;
            }
            Console.WriteLine();
        }

        var projectPath = ResolveProjectPath();
        if (projectPath == null) return;

        var projectDir = Path.GetDirectoryName(projectPath)!;
        var outputDir = Path.Combine(projectDir, "bin", _config.Configuration);

        if (_dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  [DRY RUN] Would execute the following steps:");
            Console.WriteLine();
            Console.WriteLine($"    1. dotnet clean \"{projectPath}\" -c {_config.Configuration}");
            Console.WriteLine($"    2. dotnet restore \"{projectPath}\"");
            Console.WriteLine($"    3. dotnet build \"{projectPath}\" -c {_config.Configuration}");
            Console.WriteLine($"    4. dotnet pack \"{projectPath}\" -c {_config.Configuration} -p:Version={_config.Version} --no-build");
            Console.WriteLine($"    5. dotnet nuget push ... --api-key ***API-KEY*** --source {_config.Source}");
            Console.WriteLine();
            Console.WriteLine("  ✓ Dry run complete - no changes made.");
            Console.ResetColor();
            return;
        }

        // Step 1: Clean
        Console.WriteLine("  Step 1/5: Cleaning...");
        await RunCommandAsync("dotnet", $"clean \"{projectPath}\" -c {_config.Configuration}", projectDir);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Clean completed");
        Console.ResetColor();

        // Step 2: Restore
        Console.WriteLine();
        Console.WriteLine("  Step 2/5: Restoring...");
        var (restoreSuccess, _) = await RunCommandAsync("dotnet", $"restore \"{projectPath}\"", projectDir);
        if (!restoreSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Restore failed - aborting");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Restore completed");
        Console.ResetColor();

        // Step 3: Build
        Console.WriteLine();
        Console.WriteLine("  Step 3/5: Building...");
        var (buildSuccess, _) = await RunCommandAsync("dotnet", $"build \"{projectPath}\" -c {_config.Configuration} --no-restore", projectDir);
        if (!buildSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Build failed - aborting");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Build completed");
        Console.ResetColor();

        // Step 4: Pack
        Console.WriteLine();
        Console.WriteLine("  Step 4/5: Packing...");
        var packArgs = $"pack \"{projectPath}\" -c {_config.Configuration} -p:Version={_config.Version} --no-build";
        if (_config.IncludeSymbols)
        {
            packArgs += " --include-symbols -p:SymbolPackageFormat=snupkg";
        }
        var (packSuccess, _) = await RunCommandAsync("dotnet", packArgs, projectDir, hideOutput: false);
        if (!packSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Pack failed - aborting");
            Console.ResetColor();
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Pack completed");
        Console.ResetColor();

        // Find package
        var packageFileName = $"{_config.PackageId}.{_config.Version}.nupkg";
        var packagePath = FindPackage(outputDir, packageFileName);
        if (packagePath == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Package not found: {packageFileName}");
            Console.ResetColor();
            return;
        }

        // Analyze package contents to diagnose potential duplicate file issues
        AnalyzePackageContents(packagePath);

        // Step 5: Push
        Console.WriteLine();
        Console.WriteLine("  Step 5/5: Pushing to NuGet.org...");
        var pushArgs = $"nuget push \"{packagePath}\" --api-key {_config.ApiKey} --source {_config.Source}";
        if (_config.SkipDuplicate)
        {
            pushArgs += " --skip-duplicate";
        }
        
        var (pushSuccess, pushOutput) = await RunCommandAsync("dotnet", pushArgs, projectDir, hideOutput: false);
        
        bool wasDuplicate = false;
        if (pushSuccess)
        {
            // With --skip-duplicate, exit code is 0 (success) but output indicates conflict
            if (pushOutput.Contains("already exists", StringComparison.OrdinalIgnoreCase) || 
                pushOutput.Contains("Conflict", StringComparison.OrdinalIgnoreCase))
            {
                wasDuplicate = true;
            }
        }

        if (!pushSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Push failed");
            Console.ResetColor();
            return;
        }

        if (wasDuplicate)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  SKIPPED (DUPLICATE)                         ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"  ⚠️ Version {_config.Version} already exists on NuGet.org!");
            Console.WriteLine("     The package was NOT published because it conflicts with an existing version.");
            Console.WriteLine();
            Console.WriteLine("  POSSIBLE CAUSES:");
            Console.WriteLine("    1. Version was pushed previously (check unlisted versions)");
            Console.WriteLine("    2. Validation is still processing");
            Console.WriteLine();
            Console.WriteLine("  SOLUTION:");
            Console.WriteLine("    • Increment the version number (e.g., to 1.0.4)");
            Console.WriteLine("    • Use option 'V' in the menu to change version");
            return;
        }

        // Push symbols
        if (_config.IncludeSymbols)
        {
            var symbolsFileName = $"{_config.PackageId}.{_config.Version}.snupkg";
            var symbolsPath = FindPackage(outputDir, symbolsFileName);
            if (symbolsPath != null)
            {
                Console.WriteLine("  Pushing symbols...");
                var symbolsPushArgs = $"nuget push \"{symbolsPath}\" --api-key {_config.ApiKey} --source {_config.Source}";
                if (_config.SkipDuplicate) symbolsPushArgs += " --skip-duplicate";
                await RunCommandAsync("dotnet", symbolsPushArgs, projectDir, hideOutput: false);
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         SUCCESS!                             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"  Package {_config.PackageId} v{_config.Version} published to NuGet.org!");
        Console.WriteLine($"  View at: https://www.nuget.org/packages/{_config.PackageId}/{_config.Version}");
        Console.WriteLine();
        Console.WriteLine("  Note: It may take a few minutes for the package to be indexed.");
    }

    #endregion

    #region Helper Methods

    private static string? _solutionRoot = null;

    /// <summary>
    /// Expands environment variables in a path string.
    /// Supports both %VAR% (Windows) and $VAR or ${VAR} (Unix) syntax.
    /// </summary>
    private static string ExpandEnvironmentVariables(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        
        // Use built-in Windows expansion for %VAR% syntax
        return Environment.ExpandEnvironmentVariables(path);
    }

    /// <summary>
    /// Gets the solution root directory - either from config or by auto-detection.
    /// Supports environment variables like %USERPROFILE%, %HOME%, etc.
    /// </summary>
    private static string? GetSolutionRoot()
    {
        if (_solutionRoot != null) return _solutionRoot;

        // First, check if explicitly configured
        if (!string.IsNullOrWhiteSpace(_config.SolutionRoot))
        {
            // Expand environment variables in the path
            var expandedPath = ExpandEnvironmentVariables(_config.SolutionRoot);
            
            // Handle relative paths (relative to the executable directory)
            if (!Path.IsPathRooted(expandedPath))
            {
                expandedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedPath));
            }
            
            if (Directory.Exists(expandedPath))
            {
                _solutionRoot = expandedPath;
                return _solutionRoot;
            }
        }

        // Try to auto-detect by walking up from current directory
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Any())
            {
                _solutionRoot = dir.FullName;
                return _solutionRoot;
            }
            dir = dir.Parent;
        }

        // If running from bin\Debug\net10.0, go up 4 levels to solution root
        var currentDir = Directory.GetCurrentDirectory();
        if (currentDir.Contains(Path.Combine("bin", "Debug")) || currentDir.Contains(Path.Combine("bin", "Release")))
        {
            // bin\Debug\net10.0 -> project -> solution
            var candidate = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", ".."));
            if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.sln").Any())
            {
                _solutionRoot = candidate;
                return _solutionRoot;
            }
        }

        return null;
    }

    private static string? ResolveProjectPath()
    {
        var solutionRoot = GetSolutionRoot();
        
        
        if (solutionRoot == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ ERROR: Could not determine solution root!");
            Console.WriteLine();
            Console.WriteLine("  Please set 'SolutionRoot' in appsettings.json to your solution folder:");
            Console.WriteLine("    \"SolutionRoot\": \"C:\\\\Users\\\\yourname\\\\source\\\\repos\\\\exploratorydocker\"");
            Console.ResetColor();
            return null;
        }

        Console.WriteLine($"  Solution root: {solutionRoot}");
        
        var projectPath = Path.GetFullPath(Path.Combine(solutionRoot, _config.ProjectPath));

        if (!File.Exists(projectPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ ERROR: Project file not found: {projectPath}");
            Console.WriteLine();
            Console.WriteLine("  Check that 'ProjectPath' in appsettings.json is correct.");
            Console.WriteLine($"    Current value: {_config.ProjectPath}");
            Console.ResetColor();
            return null;
        }
        
        Console.WriteLine($"  Project path:  {projectPath}");
        return projectPath;
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }

    private static string? FindPackage(string directory, string fileName)
    {
        var searchDirs = new[] { directory, Path.Combine(directory, "net10.0") };

        foreach (var dir in searchDirs)
        {
            if (Directory.Exists(dir))
            {
                var path = Path.Combine(dir, fileName);
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        if (Directory.Exists(directory))
        {
            var files = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return files[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Analyzes the contents of a .nupkg file to help diagnose duplicate file issues.
    /// The .nupkg is just a ZIP file, so we can inspect its contents.
    /// </summary>
    private static void AnalyzePackageContents(string packagePath)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                    PACKAGE CONTENTS ANALYSIS                    ");
        Console.WriteLine("  ═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(packagePath);
            
            // Group entries by folder
            var entriesByFolder = archive.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name)) // Skip directory entries
                .GroupBy(e => Path.GetDirectoryName(e.FullName)?.Replace('\\', '/') ?? "(root)")
                .OrderBy(g => g.Key)
                .ToList();

            Console.WriteLine($"  Total files in package: {archive.Entries.Count(e => !string.IsNullOrEmpty(e.Name))}");
            Console.WriteLine($"  Total folders: {entriesByFolder.Count}");
            Console.WriteLine();

            // Check for duplicates - files with same name in same folder
            var duplicates = new List<(string folder, string fileName, int count)>();
            
            foreach (var group in entriesByFolder)
            {
                var fileNames = group.Select(e => e.Name.ToLowerInvariant()).ToList();
                var duplicateNames = fileNames.GroupBy(n => n).Where(g => g.Count() > 1).ToList();
                
                foreach (var dup in duplicateNames)
                {
                    duplicates.Add((group.Key, dup.Key, dup.Count()));
                }
            }

            if (duplicates.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ╔════════════════════════════════════════════════════════════╗");
                Console.WriteLine("  ║              ⚠️  DUPLICATE FILES DETECTED!                 ║");
                Console.WriteLine("  ╚════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine();

                foreach (var (folder, fileName, count) in duplicates)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  DUPLICATE: '{fileName}' appears {count} times in folder '{folder}'");
                    Console.ResetColor();
                    
                    // Show the actual entries
                    var matchingEntries = archive.Entries
                        .Where(e => e.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase) &&
                                   (Path.GetDirectoryName(e.FullName)?.Replace('\\', '/') ?? "(root)") == folder)
                        .ToList();
                    
                    foreach (var entry in matchingEntries)
                    {
                        Console.WriteLine($"    → {entry.FullName} ({entry.Length:N0} bytes)");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No duplicate files detected within same folders.");
                Console.ResetColor();
                Console.WriteLine();
            }

            // Show all contents grouped by folder
            Console.WriteLine("  PACKAGE STRUCTURE:");
            Console.WriteLine("  ──────────────────────────────────────────────────────────────");
            
            foreach (var group in entriesByFolder)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  📁 {group.Key}/");
                Console.ResetColor();
                
                foreach (var entry in group.OrderBy(e => e.Name))
                {
                    var sizeStr = entry.Length > 1024 * 1024 
                        ? $"{entry.Length / 1024.0 / 1024.0:F1} MB"
                        : entry.Length > 1024 
                            ? $"{entry.Length / 1024.0:F1} KB"
                            : $"{entry.Length} B";
                    
                    Console.WriteLine($"      {entry.Name} ({sizeStr})");
                }
            }

            Console.WriteLine();
            Console.WriteLine("  ──────────────────────────────────────────────────────────────");
            
            // Show lib folder contents specifically (common source of issues)
            var libEntries = archive.Entries
                .Where(e => e.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) ||
                           e.FullName.StartsWith("lib\\", StringComparison.OrdinalIgnoreCase))
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .ToList();

            if (libEntries.Any())
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  LIB FOLDER CONTENTS (main package assemblies):");
                Console.ResetColor();
                
                var libByTfm = libEntries
                    .GroupBy(e => Path.GetDirectoryName(e.FullName)?.Replace('\\', '/') ?? "lib")
                    .OrderBy(g => g.Key);

                foreach (var tfmGroup in libByTfm)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    📁 {tfmGroup.Key}/");
                    Console.ResetColor();
                    
                    foreach (var entry in tfmGroup.OrderBy(e => e.Name))
                    {
                        Console.WriteLine($"        {entry.Name}");
                    }
                }
            }

            // Show contentFiles folder specifically
            var contentEntries = archive.Entries
                .Where(e => e.FullName.StartsWith("contentFiles/", StringComparison.OrdinalIgnoreCase) ||
                           e.FullName.StartsWith("contentFiles\\", StringComparison.OrdinalIgnoreCase) ||
                           e.FullName.StartsWith("content/", StringComparison.OrdinalIgnoreCase) ||
                           e.FullName.StartsWith("content\\", StringComparison.OrdinalIgnoreCase))
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .ToList();

            if (contentEntries.Any())
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  CONTENT FILES (potential source of duplicates):");
                Console.ResetColor();
                
                foreach (var entry in contentEntries.OrderBy(e => e.FullName))
                {
                    Console.WriteLine($"    {entry.FullName}");
                }
            }

        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Failed to analyze package: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    private static async Task<(bool Success, string Output)> RunCommandAsync(string command, string arguments, string workingDirectory, bool hideOutput = true)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    Failed to start process: {command}");
                Console.ResetColor();
                return (false, "Failed to start process");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;
            var combinedOutput = (output + Environment.NewLine + error).Trim();

            // Show output if not hiding, or if command failed
            bool showOutput = !hideOutput || process.ExitCode != 0;
            
            if (showOutput && !string.IsNullOrWhiteSpace(output))
            {
                foreach (var line in output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    // Highlight errors in red
                    if (line.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"    {line.TrimEnd()}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"    {line.TrimEnd()}");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var line in error.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    Console.WriteLine($"    {line.TrimEnd()}");
                }
                Console.ResetColor();
            }

            return (process.ExitCode == 0, combinedOutput);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"    Error: {ex.Message}");
            Console.ResetColor();
            return (false, ex.Message);
        }
    }

    #endregion
}

/// <summary>
/// Configuration for NuGet publishing
/// </summary>
public class NuGetConfig
{
    public string ApiKey { get; set; } = "";
    public string Source { get; set; } = "https://api.nuget.org/v3/index.json";
    public string PackageId { get; set; } = "FreeQEMU";
    public string Version { get; set; } = "0.1.0";
    public string SolutionRoot { get; set; } = "";
    public string ProjectPath { get; set; } = "FreeQEMU\\FreeQEMU.csproj";
    public string Configuration { get; set; } = "Release";
    public bool SkipDuplicate { get; set; } = true;
    public bool IncludeSymbols { get; set; } = true;
}

/// <summary>
/// Version information from NuGet.org API
/// </summary>
public class NuGetVersionInfo
{
    public Version Version { get; set; } = new Version(0, 0, 0);
    public string VersionString { get; set; } = "";
    public DateTime? Published { get; set; }
    public bool Listed { get; set; } = true;
}
