using FreeQEMU;
using Microsoft.Extensions.Configuration;

namespace FreeQEMU.DockerExample;

/// <summary>
/// Docker Example - Build and run a .NET project using Docker containers.
/// 
/// This example demonstrates:
/// - Using VmPreset.DockerDotNet10 for a Docker-ready VM with .NET SDK pre-pulled
/// - Uploading a .NET project to the VM
/// - Using the pre-cached .NET 10 SDK Docker image to build/publish/run
/// 
/// This mirrors FreeQEMU.HelloWorldExample but uses Docker instead of
/// installing .NET directly in the VM.
/// 
/// The DockerDotNet10 preset pre-pulls the SDK image during snapshot creation,
/// so subsequent boots are ready to build immediately (no download wait).
/// </summary>
internal class Program
{
    // Use the .NET 10 nightly SDK image (preview - required for net10.0 projects)
    // See: https://github.com/dotnet/dotnet-docker/blob/main/documentation/scenarios/nightly-images.md
    // Note: Using full image (not Alpine) because .NET 10 Alpine images may not be available yet
    private const string DotNetSdkImage = "mcr.microsoft.com/dotnet/sdk:10.0";

    static async Task Main(string[] args)
    {
        bool verbose = true;
        string projectName = "HelloWorldTest";

        // Load configuration
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        string solutionRoot = Environment.ExpandEnvironmentVariables(
            config["SolutionRoot"] 
            ?? throw new Exception("SolutionRoot not configured in appsettings.json"));

        // Set up output paths
        string projectSourceDir = Path.Combine(solutionRoot, "FreeQEMU.DockerExample");
        string runsDir = Path.Combine(projectSourceDir, "runs", "docker-build", "latest");

        // Clean and create output directory
        if (Directory.Exists(runsDir)) {
            Directory.Delete(runsDir, recursive: true);
        }
        Directory.CreateDirectory(runsDir);

        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           FreeQEMU Docker Build Example                      ║");
        Console.WriteLine("║           Build .NET Project Using Docker Containers         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"  SDK Image: {DotNetSdkImage}");
        Console.WriteLine($"  Project:   {projectName}");
        Console.WriteLine($"  Output:    {runsDir}");
        Console.WriteLine();
        Console.WriteLine("  NOTE: If you see 'no space left on device', delete the cached");
        Console.WriteLine("        VM image in bin/Debug/net10.0/images/ and re-run.");
        Console.WriteLine("        The new 10GB disk will be created automatically.");
        Console.WriteLine();
        Console.WriteLine();

        // Find the HelloWorldTest project
        string projectPath = Path.Combine(solutionRoot, projectName);

        if (!Directory.Exists(projectPath)) {
            Console.WriteLine($"ERROR: Project not found at {projectPath}");
            return;
        }

        // Create VM with Docker and .NET 10 SDK image pre-pulled
        // The DockerDotNet10 preset includes:
        // - Docker Engine installed
        // - mcr.microsoft.com/dotnet/sdk:10.0 image pre-pulled
        // This means subsequent boots are ready to build immediately!
        await using var vm = LinuxVm.Create()
            .WithPreset(VmPreset.DockerDotNet10)
            .WithDiskSize(10)  // 10GB disk for Docker images
            .Build();
        vm.Configuration.QuietMode = !verbose;

        IProgress<VmSetupProgress>? progress = verbose
            ? new Progress<VmSetupProgress>(p => Console.WriteLine($"  [{p.Stage}] {p.Message}"))
            : null;

        // ═══════════════════════════════════════════════════════════════
        // STEP 1: Start Docker-enabled VM
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 1: Starting Docker-enabled VM");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var startTime = DateTime.Now;
        await vm.EnsureReadyAsync(progress);
        var elapsed = DateTime.Now - startTime;

        Console.WriteLine($"  ✓ VM is ready (took {elapsed.TotalSeconds:F1}s)");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // STEP 2: Verify Docker and check disk space
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 2: Verifying Docker and disk space");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        CommandResult dockerCheck = await vm.ExecuteAsync("docker --version");
        if (!dockerCheck.Success) {
            Console.WriteLine($"  ✗ Docker not available: {dockerCheck.Error}");
            return;
        }
        Console.WriteLine($"  {dockerCheck.Output.Trim()}");

        // Check available disk space
        CommandResult dfResult = await vm.ExecuteAsync("df -h / | tail -1 | awk '{print \"Disk: \" $4 \" available of \" $2}'");
        Console.WriteLine($"  {dfResult.Output.Trim()}");

        // Verify SDK image is available (pre-pulled by DockerDotNet10 preset)
        Console.WriteLine();
        CommandResult imagesCheck = await vm.ExecuteAsync($"docker images {DotNetSdkImage} --format '{{{{.Repository}}}}:{{{{.Tag}}}}'");
        if (imagesCheck.Output.Contains(DotNetSdkImage)) {
            Console.WriteLine($"  ✓ SDK image pre-cached: {DotNetSdkImage}");
        } else {
            Console.WriteLine($"  ⚠ SDK image not found, will pull on first use");
        }

        Console.WriteLine("  ✓ Docker is ready");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // STEP 3: Upload project to VM
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 3: Uploading project to VM");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        string remoteProjectPath = $"/root/{projectName}";

        await vm.UploadFolderAsync(projectPath, remoteProjectPath,
            verbose ? new Progress<FileTransferProgress>(p =>
                Console.WriteLine($"    Uploading: {p.CurrentFile} ({p.FilesTransferred}/{p.TotalFiles})"))
            : null);

        Console.WriteLine($"  ✓ Project uploaded to {remoteProjectPath}");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // STEP 4: Build, Publish, and Run in one Docker command
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 4: Build, Publish, and Run with Docker");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  Using pre-cached SDK image (no download needed!)");
        Console.WriteLine();

        // Do everything in one container run to minimize disk usage
        // The SDK image can both build and run the app
        string buildAndRunCommand = $"""
            docker run --rm \
                -v {remoteProjectPath}:/src \
                -w /src \
                {DotNetSdkImage} \
                sh -c "echo '=== Restoring packages ===' && \
                       dotnet restore && \
                       echo '=== Building project ===' && \
                       dotnet build -c Release && \
                       echo '=== Publishing project ===' && \
                       dotnet publish -c Release -o /app && \
                       echo '=== Running application ===' && \
                       dotnet /app/{projectName}.dll"
            """;

        Console.WriteLine("  Docker command output:");
        Console.WriteLine("  ─────────────────────────────────────────────────────────────");

        CommandResult runResult = await vm.ExecuteAsync(
            buildAndRunCommand,
                        line => Console.WriteLine($"    {line}"),
                        line => Console.WriteLine($"    ERR: {line}"),
                        timeoutSeconds: 300);

                    Console.WriteLine("  ─────────────────────────────────────────────────────────────");
                    Console.WriteLine($"  Exit code: {runResult.ExitCode}");
                    Console.WriteLine($"  Duration: {runResult.Duration.TotalMilliseconds:F0}ms");
                    Console.WriteLine();

                    // ═══════════════════════════════════════════════════════════════
                    // STEP 5: Extract published files from container
                    // ═══════════════════════════════════════════════════════════════
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine("  STEP 5: Extracting published files");
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");

                    // Run publish again but this time copy output to a host-mounted volume
                    string remotePublishPath = "/root/publish";
                    await vm.ExecuteAsync($"rm -rf {remotePublishPath} && mkdir -p {remotePublishPath}");

                    string publishOnlyCommand = $"""
                        docker run --rm \
                            -v {remoteProjectPath}:/src \
                            -v {remotePublishPath}:/publish \
                            -w /src \
                            {DotNetSdkImage} \
                            dotnet publish -c Release -o /publish --no-restore
                        """;

                    CommandResult publishResult = await vm.ExecuteAsync(
                        publishOnlyCommand,
                        line => Console.WriteLine($"    {line}"),
                        line => Console.WriteLine($"    ERR: {line}"),
                        timeoutSeconds: 120);

                    if (!publishResult.Success) {
                        Console.WriteLine($"  ⚠ Failed to extract published files");
                    } else {
                        // Show published files
                        Console.WriteLine("  Published files:");
                        CommandResult lsResult = await vm.ExecuteAsync($"ls -la {remotePublishPath}");
                        foreach (string line in lsResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
                            Console.WriteLine($"    {line}");
                        }
                    }
                    Console.WriteLine();

                    // ═══════════════════════════════════════════════════════════════
                    // STEP 6: Create and download tarball
                    // ═══════════════════════════════════════════════════════════════
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine("  STEP 6: Creating tarball of published files");
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");

                    string tarPath = "/root/published-output.tar.gz";
                    CommandResult tarResult = await vm.ExecuteAsync(
                        $"cd /root && tar -czvf {tarPath} -C {remotePublishPath} .",
                        line => Console.WriteLine($"    {line}"),
                        timeoutSeconds: 60);

                    if (!tarResult.Success) {
                        Console.WriteLine($"  ✗ Tar creation failed");
                    } else {
                        Console.WriteLine($"  ✓ Tarball created at {tarPath}");
                    }
                    Console.WriteLine();

                    // ═══════════════════════════════════════════════════════════════
                    // STEP 7: Download the tarball
                    // ═══════════════════════════════════════════════════════════════
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine("  STEP 7: Downloading published files");
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");

                    string downloadDir = Path.Combine(runsDir, "download-temp");
                    Directory.CreateDirectory(downloadDir);

                    // Copy tar to a folder structure we can download
                    await vm.ExecuteAsync($"mkdir -p /root/export && cp {tarPath} /root/export/");

                    await vm.DownloadFolderAsync("/root/export", downloadDir,
                        verbose ? new Progress<FileTransferProgress>(p =>
                            Console.WriteLine($"    Downloading: {p.CurrentFile} ({p.FilesTransferred}/{p.TotalFiles})"))
                        : null);

                    // Move the tar file to the output directory
                    string downloadedTar = Path.Combine(downloadDir, "published-output.tar.gz");
                    string finalTarPath = Path.Combine(runsDir, "published-output.tar.gz");
                    if (File.Exists(downloadedTar)) {
                        File.Move(downloadedTar, finalTarPath, overwrite: true);
                        Console.WriteLine($"  ✓ Downloaded: {finalTarPath}");

                        // Also extract it
                        string extractDir = Path.Combine(runsDir, "published");
                        Directory.CreateDirectory(extractDir);
                        Console.WriteLine($"  Extracting to: {extractDir}");

                        // Use tar to extract (PowerShell/Windows tar or fall back)
                        var extractPsi = new System.Diagnostics.ProcessStartInfo {
                            FileName = "tar",
                            Arguments = $"-xzf \"{finalTarPath}\" -C \"{extractDir}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardError = true
                        };
                        try {
                            using var extractProc = System.Diagnostics.Process.Start(extractPsi);
                            if (extractProc != null) {
                                await extractProc.WaitForExitAsync();
                                if (extractProc.ExitCode == 0) {
                                    Console.WriteLine($"  ✓ Extracted published files");
                                    // List extracted files
                                    foreach (var file in Directory.GetFiles(extractDir)) {
                                        Console.WriteLine($"    - {Path.GetFileName(file)}");
                                    }
                                }
                            }
                        } catch {
                            Console.WriteLine($"  ⚠ Could not extract (tar not available on host)");
                        }
                    } else {
                        Console.WriteLine($"  ✗ Download failed");
                    }

                    // Cleanup temp folder
                    if (Directory.Exists(downloadDir)) {
                        Directory.Delete(downloadDir, recursive: true);
                    }
                    Console.WriteLine();

                    // ═══════════════════════════════════════════════════════════════
                    // STEP 8: Save results
                    // ═══════════════════════════════════════════════════════════════
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine("  STEP 8: Saving run output");
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");

                    string outputLogPath = Path.Combine(runsDir, "output.txt");
                    string logContent = $"""
                        FreeQEMU Docker Build Results
                        =============================
                        Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                        Project: {projectName}
                        SDK Image: {DotNetSdkImage}
            
                        === FULL OUTPUT ===
                        {runResult.Output}
            
                        === ERRORS ===
                        {runResult.Error}
            
                        === SUMMARY ===
                        Exit Code: {runResult.ExitCode}
                        Success: {runResult.Success}
                        Duration: {runResult.Duration.TotalMilliseconds:F0}ms
                        """;

                    await File.WriteAllTextAsync(outputLogPath, logContent);
                    Console.WriteLine($"  ✓ Output saved to: {outputLogPath}");
                    Console.WriteLine();

                    // ═══════════════════════════════════════════════════════════════
                    // STEP 9: Show Docker state
                    // ═══════════════════════════════════════════════════════════════
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine("  STEP 9: Docker images on VM");
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");

                    CommandResult imagesResult = await vm.ExecuteAsync("docker images");
                    foreach (string line in imagesResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
                        Console.WriteLine($"    {line}");
                    }
                    Console.WriteLine();

                    // Show final disk space
                    dfResult = await vm.ExecuteAsync("df -h / | tail -1 | awk '{print \"Disk: \" $4 \" available of \" $2}'");
                    Console.WriteLine($"  {dfResult.Output.Trim()}");
                    Console.WriteLine();

                    // Done!
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    if (runResult.Success && runResult.Output.Contains("Hello, World!")) {
                        Console.WriteLine("  ✓ SUCCESS: Docker build example complete!");
                        Console.WriteLine("  ✓ Application output 'Hello, World!' detected!");
                    } else if (runResult.Success) {
                        Console.WriteLine("  ✓ SUCCESS: Docker build completed (check output above)");
                    } else {
                        Console.WriteLine("  ✗ FAILED: Build or run returned non-zero exit code");
                    }
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine();
                    Console.WriteLine("  What this demonstrated:");
                    Console.WriteLine("    1. Boot a VM with VmPreset.DockerDotNet10 (Docker + SDK image)");
                    Console.WriteLine("    2. SDK image is pre-cached in the snapshot (no download!)");
                    Console.WriteLine("    3. Build, publish, and run a .NET project in a container");
                    Console.WriteLine("    4. Download published artifacts as tarball");
                    Console.WriteLine("    5. All .NET tooling runs in Docker, not the host VM");
                    Console.WriteLine();
                }
            }
