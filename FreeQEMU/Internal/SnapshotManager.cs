using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FreeQEMU.Internal;

/// <summary>
/// Manages VM snapshots using qemu-img and QMP monitor commands.
/// </summary>
internal sealed class SnapshotManager : ISnapshotManager
{
    private readonly VmConfiguration _config;
    private readonly IQemuProcessManager? _processManager;
    
    // Updated regex to handle various qemu-img snapshot output formats:
    // - ID can be a number or "--"
    // - Size can be "1.95 GiB", "2048 MiB", "512 MB", etc.
    private static readonly Regex _snapshotLineRegex = new(
        @"^\s*(?<id>\d+|--)\s+(?<tag>\S+)\s+(?<size>[\d.]+\s*\w+)\s+(?<date>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})",
        RegexOptions.Compiled);

    public SnapshotManager(VmConfiguration config)
        : this(config, null)
    {
    }

    internal SnapshotManager(VmConfiguration config, IQemuProcessManager? processManager)
    {
        _config = config;
        _processManager = processManager;
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken)
    {
        var snapshots = await ListAsync(cancellationToken);
        return snapshots.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<SnapshotInfo>> ListAsync(CancellationToken cancellationToken)
    {
        var qemuImgPath = Path.Combine(_config.QemuDirectory, "qemu-img.exe");
        if (!File.Exists(qemuImgPath))
            return [];

        if (!File.Exists(_config.BaseImagePath))
            return [];

        var psi = new ProcessStartInfo
        {
            FileName = qemuImgPath,
            Arguments = $"snapshot -l \"{_config.BaseImagePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null) return [];

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return ParseSnapshotList(output);
        }
        catch
        {
            return [];
        }
    }

    public async Task SaveAsync(string name, CancellationToken cancellationToken)
    {
        if (_processManager == null || !_processManager.IsRunning)
            throw new SnapshotException("Cannot save snapshot: VM is not running");

        Console.WriteLine($"  [Snapshot] Sending savevm command for '{name}'...");
        var response = await _processManager.SendMonitorCommandAsync($"savevm {name}", cancellationToken);
        Console.WriteLine($"  [Snapshot] savevm response: {response ?? "(empty)"}");

        // Check for error in response
        if (!string.IsNullOrEmpty(response) && response.Contains("Error", StringComparison.OrdinalIgnoreCase))
        {
            throw new SnapshotException($"Failed to save snapshot '{name}': {response}");
        }

        // Wait for snapshot to flush to disk - savevm can take a while for large VMs
        Console.WriteLine($"  [Snapshot] Waiting for snapshot to complete...");
        await Task.Delay(5000, cancellationToken);

        // Verify using QMP info snapshots command (works while VM is running)
        Console.WriteLine($"  [Snapshot] Verifying snapshot via QMP...");
        var infoResponse = await _processManager.SendMonitorCommandAsync("info snapshots", cancellationToken);
        Console.WriteLine($"  [Snapshot] info snapshots response: {infoResponse ?? "(empty)"}");

        if (string.IsNullOrEmpty(infoResponse) || !infoResponse.Contains(name))
        {
            throw new SnapshotException($"Failed to verify snapshot '{name}' was saved. Info response: {infoResponse}");
        }

        Console.WriteLine($"  [Snapshot] Snapshot '{name}' saved successfully!");
    }

    public async Task RestoreAsync(string name, CancellationToken cancellationToken)
    {
        if (_processManager == null || !_processManager.IsRunning)
            throw new SnapshotException("Cannot restore snapshot: VM is not running");

        var exists = await ExistsAsync(name, cancellationToken);
        if (!exists)
            throw new SnapshotException($"Snapshot '{name}' not found");

        var response = await _processManager.SendMonitorCommandAsync($"loadvm {name}", cancellationToken);

        if (!string.IsNullOrEmpty(response) && response.Contains("Error", StringComparison.OrdinalIgnoreCase))
        {
            throw new SnapshotException($"Failed to restore snapshot '{name}': {response}");
        }
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        var qemuImgPath = Path.Combine(_config.QemuDirectory, "qemu-img.exe");
        if (!File.Exists(qemuImgPath))
            throw new SnapshotException("qemu-img not found");

        var psi = new ProcessStartInfo
        {
            FileName = qemuImgPath,
            Arguments = $"snapshot -d {name} \"{_config.BaseImagePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
                throw new SnapshotException("Failed to start qemu-img");

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                throw new SnapshotException($"Failed to delete snapshot: {error}");
            }
        }
        catch (SnapshotException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SnapshotException($"Failed to delete snapshot: {ex.Message}", ex);
        }
    }

    private static List<SnapshotInfo> ParseSnapshotList(string output)
    {
        var snapshots = new List<SnapshotInfo>();

        foreach (var line in output.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.Contains("ID") && line.Contains("TAG")) continue; // Header

            var match = _snapshotLineRegex.Match(line);
            if (match.Success)
            {
                var dateStr = match.Groups["date"].Value.Trim();
                DateTime? createdAt = null;
                
                if (DateTime.TryParse(dateStr, out var parsed))
                    createdAt = parsed;

                snapshots.Add(new SnapshotInfo
                {
                    Id = match.Groups["id"].Value.Trim(),
                    Name = match.Groups["tag"].Value.Trim(),
                    VmSize = match.Groups["size"].Value.Trim(),
                    CreatedAt = createdAt
                });
            }
        }

        return snapshots;
    }
}
