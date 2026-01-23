using FreeQEMU;
using Microsoft.Extensions.Configuration;

namespace FreeQEMU.HelloWorldExample;

internal class Program
{
    static async Task Main(string[] args)
    {
        bool verbose = true;
        string projectName = "HelloWorldTest";
        string testName = "HelloWorldTest"; // Name for this test run's output folder
        
        // Load configuration
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        
        string solutionRoot = Environment.ExpandEnvironmentVariables(
            config["SolutionRoot"] 
            ?? throw new Exception("SolutionRoot not configured in appsettings.json"));
        
        // Set up output paths relative to the project source folder (not bin)
        string projectSourceDir = Path.Combine(solutionRoot, "FreeQEMU.HelloWorldExample");
        string runsDir = Path.Combine(projectSourceDir, "runs", testName, "latest");
        
        // Clean and create output directory
        if (Directory.Exists(runsDir)) {
            Directory.Delete(runsDir, recursive: true);
        }
        Directory.CreateDirectory(runsDir);

        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           FreeQEMU Build & Run Test                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Output directory: {runsDir}");
        Console.WriteLine();

        // Find the HelloWorldTest project (relative to solution root)
        string projectPath = Path.Combine(solutionRoot, projectName);
        
        if (!Directory.Exists(projectPath)) {
            Console.WriteLine($"ERROR: Project not found at {projectPath}");
            return;
        }
        
        Console.WriteLine($"Project to build: {projectPath}");
        Console.WriteLine();

        // Create a Linux VM with .NET 10 pre-installed
        await using var vm = new LinuxVm(VmPreset.DotNet10);
        vm.Configuration.QuietMode = !verbose;

        // Progress reporter
        IProgress<VmSetupProgress>? progress = verbose 
            ? new Progress<VmSetupProgress>(p => Console.WriteLine($"  [{p.Stage}] {p.Message}"))
            : null;

        // Step 1: Ensure VM is ready
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 1: Starting VM");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        await vm.EnsureReadyAsync(progress);
        Console.WriteLine("  ✓ VM is ready\n");

        // Step 2: Upload project
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 2: Uploading project to VM");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        string remoteProjectPath = $"/root/{projectName}";
        
        await vm.UploadFolderAsync(projectPath, remoteProjectPath, 
            verbose ? new Progress<FileTransferProgress>(p => 
                Console.WriteLine($"    Uploading: {p.CurrentFile} ({p.FilesTransferred}/{p.TotalFiles})")) 
            : null);
        Console.WriteLine($"  ✓ Project uploaded to {remoteProjectPath}\n");

        // Step 3: Restore packages
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 3: Restoring NuGet packages");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        CommandResult restoreResult = await vm.ExecuteAsync(
            $"cd {remoteProjectPath} && dotnet restore",
            line => Console.WriteLine($"    {line}"),
            line => Console.WriteLine($"    ERR: {line}"),
            timeoutSeconds: 120);
        
        if (!restoreResult.Success) {
            Console.WriteLine($"  ✗ Restore failed with exit code {restoreResult.ExitCode}");
            return;
        }
        Console.WriteLine("  ✓ Restore completed\n");

        // Step 4: Build
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 4: Building project");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        CommandResult buildResult = await vm.ExecuteAsync(
            $"cd {remoteProjectPath} && dotnet build -c Release",
            line => Console.WriteLine($"    {line}"),
            line => Console.WriteLine($"    ERR: {line}"),
            timeoutSeconds: 120);
        
        if (!buildResult.Success) {
            Console.WriteLine($"  ✗ Build failed with exit code {buildResult.ExitCode}");
            return;
        }
        Console.WriteLine("  ✓ Build completed\n");

        // Step 5: Publish
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 5: Publishing project");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        string publishPath = "/root/publish";
        CommandResult publishResult = await vm.ExecuteAsync(
            $"cd {remoteProjectPath} && dotnet publish -c Release -o {publishPath}",
            line => Console.WriteLine($"    {line}"),
            line => Console.WriteLine($"    ERR: {line}"),
            timeoutSeconds: 120);
        
        if (!publishResult.Success) {
            Console.WriteLine($"  ✗ Publish failed with exit code {publishResult.ExitCode}");
            return;
        }
        Console.WriteLine("  ✓ Publish completed\n");

        // Step 6: Run the published app
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 6: Running published application");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        CommandResult runResult = await vm.ExecuteAsync(
            $"cd {publishPath} && dotnet {projectName}.dll",
            line => Console.WriteLine($"    OUTPUT: {line}"),
            line => Console.WriteLine($"    ERR: {line}"),
            timeoutSeconds: 60);
        
        Console.WriteLine();
        Console.WriteLine($"  Exit code: {runResult.ExitCode}");
        Console.WriteLine($"  Duration: {runResult.Duration.TotalMilliseconds:F0}ms");
        Console.WriteLine();

        // Step 7: Save console output to file
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 7: Saving run output");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        string outputLogPath = Path.Combine(runsDir, "output.txt");
        string logContent = $"""
            FreeQEMU Build & Run Results
            ============================
            Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
            Project: {projectName}
            
            === RUN OUTPUT ===
            {runResult.Output}
            
            === RUN ERRORS ===
            {runResult.Error}
            
            === SUMMARY ===
            Exit Code: {runResult.ExitCode}
            Success: {runResult.Success}
            Duration: {runResult.Duration.TotalMilliseconds:F0}ms
            """;
        
        await File.WriteAllTextAsync(outputLogPath, logContent);
        Console.WriteLine($"  ✓ Output saved to: {outputLogPath}\n");

        // Step 8: Create tarball of published output
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 8: Creating tarball of published files");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        string tarPath = "/root/published-output.tar.gz";
        CommandResult tarResult = await vm.ExecuteAsync(
            $"cd /root && tar -czvf {tarPath} -C {publishPath} .",
            line => Console.WriteLine($"    {line}"),
            timeoutSeconds: 60);
        
        if (!tarResult.Success) {
            Console.WriteLine($"  ✗ Tar creation failed");
        } else {
            Console.WriteLine($"  ✓ Tarball created at {tarPath}\n");
        }

        // Step 9: Download the tarball
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  STEP 9: Downloading published files");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        
        // Download the tar file using SCP by downloading to a temp folder then moving
        string downloadDir = Path.Combine(runsDir, "download-temp");
        Directory.CreateDirectory(downloadDir);
        
        // Copy tar to a folder structure we can download
        await vm.ExecuteAsync($"mkdir -p /root/export && cp {tarPath} /root/export/");
        
        await vm.DownloadFolderAsync("/root/export", downloadDir,
            verbose ? new Progress<FileTransferProgress>(p => 
                Console.WriteLine($"    Downloading: {p.CurrentFile}")) 
            : null);
        
        // Move the tar file to the runs/latest folder
        string downloadedTar = Path.Combine(downloadDir, "published-output.tar.gz");
        string finalTarPath = Path.Combine(runsDir, "published-output.tar.gz");
        
        if (File.Exists(downloadedTar)) {
            File.Move(downloadedTar, finalTarPath);
            Directory.Delete(downloadDir, recursive: true);
            Console.WriteLine($"  ✓ Downloaded to: {finalTarPath}\n");
        } else {
            Console.WriteLine($"  ✗ Download failed - tar file not found\n");
        }

        // Summary
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                       COMPLETE                               ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"  Output directory: {runsDir}");
        Console.WriteLine();
        Console.WriteLine("  Files created:");
        foreach (string file in Directory.GetFiles(runsDir)) {
            FileInfo fi = new(file);
            Console.WriteLine($"    - {fi.Name} ({fi.Length / 1024.0:F1} KB)");
        }
        Console.WriteLine();
        Console.WriteLine($"  Application output: {(runResult.Success ? "SUCCESS" : "FAILED")}");
        Console.WriteLine($"  Total run output:\n    {runResult.Output.Trim().Replace("\n", "\n    ")}");
    }
}
