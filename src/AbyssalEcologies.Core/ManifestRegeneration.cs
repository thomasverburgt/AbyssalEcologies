using System;
using System.IO;
using System.Linq;

namespace AbyssalEcologies.Core;

public sealed class ManifestRegenerationResult
{
    public ManifestRegenerationResult(string manifestPath, string backupPath, int seed, int regionCount, int placementCount)
    {
        ManifestPath = manifestPath;
        BackupPath = backupPath;
        Seed = seed;
        RegionCount = regionCount;
        PlacementCount = placementCount;
    }

    public string ManifestPath { get; }
    public string BackupPath { get; }
    public int Seed { get; }
    public int RegionCount { get; }
    public int PlacementCount { get; }
}

public static class ManifestRegeneration
{
    public const string ConfirmationPhrase = "CONFIRM_DISPOSABLE_SAVE_REGENERATION";

    public static ManifestRegenerationResult Stage(
        string manifestPath,
        WorldManifest currentManifest,
        WorldManifest replacementManifest,
        string confirmation,
        DateTime utcNow)
    {
        if (confirmation != ConfirmationPhrase)
            throw new InvalidOperationException($"Regeneration requires the exact confirmation phrase {ConfirmationPhrase}.");
        if (string.IsNullOrWhiteSpace(manifestPath))
            throw new ArgumentException("Manifest path is required.", nameof(manifestPath));
        if (currentManifest == null) throw new ArgumentNullException(nameof(currentManifest));
        if (replacementManifest == null) throw new ArgumentNullException(nameof(replacementManifest));

        currentManifest.Validate();
        replacementManifest.Validate();
        if (currentManifest.Seed == replacementManifest.Seed)
            throw new InvalidOperationException("Regeneration requires a seed different from the active manifest.");
        var currentJson = WorldManifestSerializer.Serialize(currentManifest);
        var replacementJson = WorldManifestSerializer.Serialize(replacementManifest);
        if (currentJson == replacementJson)
            throw new InvalidOperationException("The requested settings reproduce the active manifest exactly; choose a different seed.");

        var fullManifestPath = Path.GetFullPath(manifestPath);
        var directory = Path.GetDirectoryName(fullManifestPath)
            ?? throw new InvalidDataException("The manifest path has no parent directory.");
        Directory.CreateDirectory(directory);

        var backupPath = NextBackupPath(fullManifestPath, utcNow);
        if (File.Exists(fullManifestPath))
            File.Copy(fullManifestPath, backupPath, overwrite: false);
        else
            File.WriteAllText(backupPath, currentJson);

        var temporaryPath = fullManifestPath + ".regeneration.tmp";
        try
        {
            File.WriteAllText(temporaryPath, replacementJson);
            if (File.Exists(fullManifestPath))
                File.Replace(temporaryPath, fullManifestPath, null);
            else
                File.Move(temporaryPath, fullManifestPath);
        }
        catch
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            throw;
        }

        var world = replacementManifest.ToGeneratedWorld();
        return new ManifestRegenerationResult(
            fullManifestPath,
            backupPath,
            world.Seed,
            world.Regions.Count,
            world.Regions.Sum(region => region.Placements.Count));
    }

    private static string NextBackupPath(string manifestPath, DateTime utcNow)
    {
        var directory = Path.GetDirectoryName(manifestPath)!;
        var stem = Path.GetFileNameWithoutExtension(manifestPath);
        var extension = Path.GetExtension(manifestPath);
        var timestamp = utcNow.ToUniversalTime().ToString("yyyyMMddTHHmmssfffZ");
        var candidate = Path.Combine(directory, $"{stem}.pre-regeneration-{timestamp}{extension}");
        for (var suffix = 1; File.Exists(candidate); suffix++)
            candidate = Path.Combine(directory, $"{stem}.pre-regeneration-{timestamp}-{suffix}{extension}");
        return candidate;
    }
}
