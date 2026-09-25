using System;
using System.IO;
using System.Threading.Tasks;
using AbyssalEcologies.Core;
using Nautilus.Json;
using Nautilus.Json.Attributes;

namespace AbyssalEcologies.Plugin;

[FileName("AbyssalEcologies")]
internal sealed class AbyssalEcologiesSaveData : SaveDataCache
{
    public WorldManifest? Manifest { get; set; }

    public override Task LoadAsync(bool createFileIfNotExist = true)
    {
        Manifest = null;
        if (!File.Exists(JsonFilePath))
            return Task.CompletedTask;

        Manifest = WorldManifestSerializer.Deserialize(File.ReadAllText(JsonFilePath));
        return Task.CompletedTask;
    }

    public override Task SaveAsync()
    {
        if (Manifest == null)
            return Task.CompletedTask;

        var directory = Path.GetDirectoryName(JsonFilePath)
            ?? throw new InvalidDataException("The manifest path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = JsonFilePath + ".tmp";
        File.WriteAllText(temporaryPath, WorldManifestSerializer.Serialize(Manifest));
        if (File.Exists(JsonFilePath))
            File.Replace(temporaryPath, JsonFilePath, null);
        else
            File.Move(temporaryPath, JsonFilePath);

        return Task.CompletedTask;
    }

    public ManifestRegenerationResult StageRegeneration(WorldManifest replacement, string confirmation, string durableBackupDirectory)
    {
        if (Manifest == null)
            throw new InvalidOperationException("No active manifest is available to preserve.");

        var result = ManifestRegeneration.Stage(JsonFilePath, durableBackupDirectory, Manifest, replacement, confirmation, DateTime.UtcNow);
        Manifest = replacement;
        return result;
    }
}
