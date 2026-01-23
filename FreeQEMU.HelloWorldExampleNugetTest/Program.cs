using FreeQEMU;
using Microsoft.Extensions.Configuration;

namespace FreeQEMU.HelloWorldExampleNugetTest;

/// <summary>
/// HelloWorld Example using FreeQEMU NuGet Package (v1.0.1)
/// 
/// This example demonstrates using FreeQEMU as a NuGet package reference
/// instead of a project reference. It:
/// - Creates a VM with .NET 10 SDK installed
/// - Uploads a .NET project to the VM
/// - Builds, publishes, and runs the project
/// - Downloads the published output as a tarball
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        bool verbose = true;
        string projectName = "HelloWorldTest";
        string testName = "HelloWorldTest";
        
        // Load configuration
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        
        string solutionRoot = Environment.ExpandEnvironmentVariables(
            config["SolutionRoot"] 
            ?? throw new Exception("SolutionRoot not configured in appsettings.json"));
        
        // Set up output paths
        string projectSourceDir = Path.Combine(solutionRoot, "FreeQEMU.HelloWorldExampleNugetTest");
        string runsDir = Path.Combine(projectSourceDir, "runs", testName, "latest");
        
        // Clean and create output directory
        if (Directory.Exists(runsDir)) {
            Directory.Delete(runsDir, recursive: true);
        }
        Directory.CreateDirectory(runsDir);

        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     FreeQEMU HelloWorld Example (NuGet Package Test)         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"  Using FreeQEMU NuGet package v1.0.1");
        Console.WriteLine($"  Output directory: {runsDir}");
        Console.WriteLine();

        // Find the HelloWorldTest project
        string projectPath = Path.Combine(solutionRoot, projectName);
        
        if (!Directory.Exists(projectPath)) {
            Console.WriteLine($"ERROR: Project not found at {projectPath}");
            return;
        }
        
        Console.WriteLine($"  Project to build: {projectPath}");
        Console.WriteLine();

        // Create VM with .NET 10 preset
        await using var vm = LinuxVm.Create()
            .WithPreset(VmPreset.DotNet10)
            .WithDiskSize(10)
            .Build();
        vm.Configuration.QuietMode = !verbose;
        
        IProgress<VmSetupProgress>? progress = verbose 
            ? new Progress<VmSetupProgress>(p => Console.WriteLine($"  [{p.Stage}] {p.Message}"))
            : null;

        // ═══════════════════════════════════════════════════════════════
        // STEP 1: Start VM
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 1: Starting VM with .NET 10 SDK");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        var startTime = DateTime.Now;
        await vm.EnsureReadyAsync(progress);
        var elapsed = DateTime.Now - startTime;
        
        Console.WriteLine($"  ✓ VM is ready (took {elapsed.TotalSeconds:F1}s)");
        Console.WriteLine();
        
        // ═══════════════════════════════════════════════════════════════
        // STEP 2: Verify .NET SDK
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 2: Verifying .NET SDK");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        CommandResult dotnetCheck = await vm.ExecuteAsync("dotnet --version");
        Console.WriteLine($"  .NET SDK version: {dotnetCheck.Output.Trim()}");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // STEP 3: Upload project
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
        // STEP 4: Build and publish
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 4: Building and publishing project");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        string remotePublishPath = "/root/publish";
        
        CommandResult buildResult = await vm.ExecuteAsync(
            $"cd {remoteProjectPath} && dotnet restore && dotnet publish -c Release -o {remotePublishPath}",
            line => Console.WriteLine($"    {line}"),
            line => Console.WriteLine($"    ERR: {line}"),
            timeoutSeconds: 300);
        
        if (!buildResult.Success) {
            Console.WriteLine($"  ✗ Build failed with exit code {buildResult.ExitCode}");
            return;
        }
        Console.WriteLine($"  ✓ Build completed");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // STEP 5: Run the application
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 5: Running the application");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        CommandResult runResult = await vm.ExecuteAsync(
            $"dotnet {remotePublishPath}/{projectName}.dll",
            line => Console.WriteLine($"    OUTPUT: {line}"),
            line => Console.WriteLine($"    ERR: {line}"),
            timeoutSeconds: 60);
        
        Console.WriteLine($"  Exit code: {runResult.ExitCode}");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // STEP 6: Create tarball
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 6: Creating tarball of published files");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        string tarPath = "/root/published-output.tar.gz";
        await vm.ExecuteAsync($"tar -czvf {tarPath} -C {remotePublishPath} .",
            line => Console.WriteLine($"    {line}"));
        Console.WriteLine($"  ✓ Tarball created");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // STEP 7: Download tarball
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 7: Downloading published files");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        string downloadDir = Path.Combine(runsDir, "download-temp");
        Directory.CreateDirectory(downloadDir);
        
        await vm.ExecuteAsync($"mkdir -p /root/export && cp {tarPath} /root/export/");
        
        await vm.DownloadFolderAsync("/root/export", downloadDir,
            verbose ? new Progress<FileTransferProgress>(p => 
                Console.WriteLine($"    Downloading: {p.CurrentFile}"))
            : null);
        
        string downloadedTar = Path.Combine(downloadDir, "published-output.tar.gz");
        string finalTarPath = Path.Combine(runsDir, "published-output.tar.gz");
        if (File.Exists(downloadedTar)) {
            File.Move(downloadedTar, finalTarPath, overwrite: true);
            Console.WriteLine($"  ✓ Downloaded: {finalTarPath}");
            
            // Extract
            string extractDir = Path.Combine(runsDir, "published");
            Directory.CreateDirectory(extractDir);
            var extractPsi = new System.Diagnostics.ProcessStartInfo {
                FileName = "tar",
                Arguments = $"-xzf \"{finalTarPath}\" -C \"{extractDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try {
                using var proc = System.Diagnostics.Process.Start(extractPsi);
                if (proc != null) {
                    await proc.WaitForExitAsync();
                    Console.WriteLine($"  ✓ Extracted to: {extractDir}");
                }
            } catch {
                Console.WriteLine($"  ⚠ Could not extract (tar not available on host)");
            }
        }
        
        if (Directory.Exists(downloadDir)) {
            Directory.Delete(downloadDir, recursive: true);
        }
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // STEP 8: Save output log
        // ═══════════════════════════════════════════════════════════════
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 8: Saving output log");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        string outputLogPath = Path.Combine(runsDir, "output.txt");
        string logContent = $"""
            FreeQEMU HelloWorld Example (NuGet Package Test)
            =================================================
            Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
            Project: {projectName}
            FreeQEMU Version: 1.0.1 (NuGet)
            
            === APPLICATION OUTPUT ===
            {runResult.Output}
            
            === SUMMARY ===
            Exit Code: {runResult.ExitCode}
            Success: {runResult.Success}
            """;
        
        await File.WriteAllTextAsync(outputLogPath, logContent);
        Console.WriteLine($"  ✓ Output saved to: {outputLogPath}");
        Console.WriteLine();

        // Done!
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        if (runResult.Success && runResult.Output.Contains("Hello, World!")) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ SUCCESS: HelloWorld example complete!");
            Console.WriteLine("  ✓ Application output 'Hello, World!' detected!");
            Console.ResetColor();
        } else {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ FAILED or unexpected output");
            Console.ResetColor();
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("  This example used FreeQEMU v1.0.1 from NuGet to:");
        Console.WriteLine("    1. Create a VM with .NET 10 SDK");
        Console.WriteLine("    2. Upload, build, publish, and run a .NET project");
        Console.WriteLine("    3. Download the published artifacts");
        Console.WriteLine();
    }
}
