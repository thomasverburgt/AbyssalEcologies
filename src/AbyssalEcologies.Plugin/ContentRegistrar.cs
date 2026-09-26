using System;
using System.Collections.Generic;
using System.Linq;
using AbyssalEcologies.Core;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Handlers;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class ContentRegistrar
{
    private const int InstanceLogLimitPerContent = 3;
    private const float LandmarkProbeHeight = 180f;
    private const float LandmarkProbeDistance = 520f;
    private const float LandmarkSearchRadius = 144f;
    private const float LandmarkSurfaceOffset = 0.2f;
    private const float MaximumFoundationSiteSlope = 65f;
    private const float MaximumLandmarkAdjustment = 260f;
    private const float MinimumSupportHalfExtent = 2.5f;
    private const float MaximumSupportHalfExtent = 14f;
    private const int MinimumFoundationTerrainHits = 5;
    private const float MinimumFoundationThickness = 1.5f;
    private const float MaximumFoundationThickness = 18f;
    private const int FloraSiteRingSampleCount = 8;
    private const float FloraSiteRingRadius = 6f;
    private const float GlassKelpSeamountRadius = 132f;
    private const float GlassKelpSeamountVerticalRadius = 25f;
    private const float GlassKelpSeamountTopBelowCenter = 20f;
    private static readonly Vector2[] LandmarkSupportSamples =
    {
        new(0f, 0f),
        new(-1f, 0f),
        new(1f, 0f),
        new(0f, -1f),
        new(0f, 1f),
        new(-1f, -1f),
        new(-1f, 1f),
        new(1f, -1f),
        new(1f, 1f)
    };
    private static readonly HashSet<string> LandmarkContentIds = new(StringComparer.Ordinal)
    {
        "glass-arch",
        "thermal-spire",
        "nursery-heart"
    };
    private static readonly Dictionary<string, TechType> RegisteredTechTypes = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> LoggedInstanceCounts = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> InstantiationCounts = new(StringComparer.Ordinal);
    private static readonly List<WeakReference> LiveInstances = new();
    private static readonly Dictionary<string, LandmarkGroundingResult> LandmarkGroundingResults = new(StringComparer.Ordinal);
    private static readonly List<LandmarkSitePlan> LandmarkSitePlans = new();
    private static GameObject? _glassKelpSeamount;
    private static int _registeredPlacementCount;
    private static bool _useOriginalGlassfin = true;
    private static string _glassfinRegistrationDetail = "not registered";
    private static string _glassfinEggRegistrationDetail = "not registered";

    private static readonly ContentDefinition[] Definitions =
    {
        new("glassfin", "Glassfin", "A translucent filter-feeder drawn to crystalline kelp.", "Peeper", new Color(0.25f, 0.95f, 1f), new Color(0.1f, 0.65f, 1f)),
        new("cinder-ray", "Cinder Ray", "A heat-tolerant ray whose fins scatter ember-like light.", "RabbitRay", new Color(1f, 0.28f, 0.08f), new Color(1f, 0.12f, 0.02f)),
        new("lantern-skate", "Lantern Skate", "A gentle grazer that pulses with cold blue bioluminescence.", "Jellyray", new Color(0.35f, 0.45f, 1f), new Color(0.25f, 0.15f, 1f)),

        new("prism-kelp", "Prism Kelp", "A reflective kelp analogue growing in dense aerial gardens.", "Creepvine", new Color(0.2f, 0.9f, 0.85f), new Color(0.1f, 0.8f, 1f)),
        new("ember-fan", "Ember Fan", "A fan-shaped colony adapted to geothermal water.", "PurpleFan", new Color(1f, 0.22f, 0.03f), new Color(1f, 0.08f, 0.01f)),
        new("ghost-bloom", "Ghost Bloom", "A pale colony that shelters juvenile lantern skates.", "SmallFan", new Color(0.55f, 0.75f, 1f), new Color(0.2f, 0.45f, 1f)),

        new("glass-arch", "Glass Arch", "The mineralized heart of a Glass Kelp Garden.", "CoralShellPlate", new Color(0.2f, 0.9f, 1f), new Color(0.15f, 0.65f, 1f)),
        new("thermal-spire", "Thermal Spire", "A towering heat-bright colony marking an Ember Trench.", "MembrainTree", new Color(1f, 0.25f, 0.02f), new Color(1f, 0.08f, 0.01f)),
        new("nursery-heart", "Nursery Heart", "A vast bloom at the center of a Ghostlight Nursery.", "MembrainTree", new Color(0.45f, 0.65f, 1f), new Color(0.2f, 0.3f, 1f))
    };

    public static int RegisterDefinitions()
    {
        foreach (var definition in Definitions)
        {
            if (!Enum.TryParse(definition.SourceTechType, ignoreCase: false, out TechType sourceTechType))
            {
                Plugin.Log.LogWarning($"Skipping '{definition.DisplayName}': source TechType '{definition.SourceTechType}' is unavailable in this game build.");
                continue;
            }

            RegisterDefinition(definition, sourceTechType);
        }

        return RegisteredTechTypes.Count;
    }

    public static void ConfigureOriginalGlassfin(bool enabled) => _useOriginalGlassfin = enabled;

    public static string GetGlassfinStatus()
    {
        var registered = RegisteredTechTypes.ContainsKey("glassfin") ? "yes" : "no";
        var controllers = UnityEngine.Object.FindObjectsOfType<GlassfinPrototypeController>();
        var feeding = controllers.Count(controller => controller.IsFeeding);
        return $"AE Glassfin prototype: enabled={_useOriginalGlassfin}, registered={registered}, active={controllers.Length}, filterFeeding={feeding}, callsPlayed={GlassfinPrototypeController.CallCount}, assets={_glassfinRegistrationDetail}, egg={_glassfinEggRegistrationDetail}.";
    }

    public static SpawnRegistrationMetrics RegisterWorldSpawns(GeneratedWorld world)
    {
        LoggedInstanceCounts.Clear();
        InstantiationCounts.Clear();
        LiveInstances.Clear();
        LandmarkGroundingResults.Clear();
        LandmarkSitePlans.Clear();
        _registeredPlacementCount = 0;

        foreach (var region in world.Regions)
        {
            var landmark = region.Placements.FirstOrDefault(placement => placement.Kind == PlacementKind.Landmark);
            if (landmark == null)
                continue;

            var floraSites = region.Placements
                .Where(placement => placement.Kind == PlacementKind.Flora)
                .OrderBy(placement => HorizontalDistanceSquared(placement.Position, landmark.Position))
                .Take(8)
                .Select(placement => placement.Position)
                .ToArray();
            LandmarkSitePlans.Add(new LandmarkSitePlan(
                landmark.ContentId,
                landmark.Position,
                floraSites,
                PlannedLandmarkSpawnAnchor(landmark.ContentId, landmark.Position, floraSites)));
        }

        var placementsByContent = world.Regions
            .SelectMany(region => region.Placements)
            .GroupBy(placement => placement.ContentId)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var registeredContentTypeCount = 0;

        foreach (var pair in placementsByContent)
        {
            if (!RegisteredTechTypes.TryGetValue(pair.Key, out var techType))
            {
                Plugin.Log.LogWarning($"Manifest content '{pair.Key}' has no registered prefab definition; skipping {pair.Value.Length} placements.");
                continue;
            }

            var contentId = pair.Key;
            var spawnInfos = pair.Value.Select(placement => ToSpawnInfo(techType, contentId, placement)).ToList();
            CoordinatedSpawnsHandler.RegisterCoordinatedSpawns(spawnInfos);
            _registeredPlacementCount += pair.Value.Length;
            registeredContentTypeCount++;
            Plugin.Log.LogInfo($"Registered {pair.Value.Length} coordinated spawns for '{pair.Key}'.");
        }

        for (var index = 0; index < world.Regions.Count; index++)
        {
            var region = world.Regions[index];
            var commandName = $"ae{index + 1}";
            var landmark = region.Placements.FirstOrDefault(placement => placement.Kind == PlacementKind.Landmark);
            var fieldCheckPoint = landmark == null
                ? region.Center
                : FindLandmarkSitePlan(landmark.ContentId, landmark.Position)?.SpawnAnchor ?? region.Center;
            ConsoleCommandsHandler.AddGotoTeleportPosition(commandName, new Vector3(fieldCheckPoint.X, fieldCheckPoint.Y + 8f, fieldCheckPoint.Z));
            Plugin.Log.LogInfo($"Field-check teleport: 'goto {commandName}' -> {region.DisplayName} {fieldCheckPoint}.");
        }

        return new SpawnRegistrationMetrics(_registeredPlacementCount, registeredContentTypeCount);
    }

    public static RuntimeSpawnMetrics GetRuntimeMetrics()
    {
        var activeCount = 0;
        for (var index = LiveInstances.Count - 1; index >= 0; index--)
        {
            var reference = LiveInstances[index];
            var gameObject = reference.Target as GameObject;
            if (!reference.IsAlive || gameObject == null)
                LiveInstances.RemoveAt(index);
            else
                activeCount++;
        }

        return new RuntimeSpawnMetrics(
            _registeredPlacementCount,
            InstantiationCounts.Values.Sum(),
            activeCount,
            new Dictionary<string, int>(InstantiationCounts, StringComparer.Ordinal));
    }

    public static LandmarkGroundingMetrics GetLandmarkGroundingMetrics()
    {
        var results = new Dictionary<string, LandmarkGroundingResult>(LandmarkGroundingResults, StringComparer.Ordinal);
        return new LandmarkGroundingMetrics(
            results.Count,
            results.Values.Count(result => result.Grounded),
            results.Values.Count(result => !result.Grounded),
            results);
    }

    private static void RegisterDefinition(ContentDefinition definition, TechType sourceTechType)
    {
        var classId = $"AbyssalEcologies_{definition.Id.Replace('-', '_')}";
        var originalGlassfin = _useOriginalGlassfin && string.Equals(definition.Id, "glassfin", StringComparison.Ordinal);
        var prefab = originalGlassfin
            ? new CustomPrefab(classId, definition.DisplayName, definition.Description, GlassfinPrototype.Icon)
            : new CustomPrefab(classId, definition.DisplayName, definition.Description);
        var template = new CloneTemplate(prefab.Info, sourceTechType)
        {
            ModifyPrefab = gameObject =>
            {
                ApplyAppearance(gameObject, definition);
                if (!originalGlassfin)
                    return;

                if (GlassfinPrototype.TryReplaceVisuals(gameObject, out var detail))
                    _glassfinRegistrationDetail = detail;
                else
                    _glassfinRegistrationDetail = $"Peeper rollback visual active because original construction failed: {detail}";
            }
        };

        prefab.SetGameObject(template);
        if (originalGlassfin)
        {
            const string encyclopediaKey = "AbyssalEcologiesGlassfin";
            prefab.AddOnRegister(() =>
            {
                PDAHandler.AddEncyclopediaEntry(
                    encyclopediaKey,
                    "Lifeforms/Fauna/Herbivores",
                    "Glassfin",
                    "A small shoaling filter-feeder whose paired facial fans strain plankton from water moving through Prism Kelp. Its transparent mineral plates pulse while feeding. Assessment: edible only in an emergency; ecological value is greater than nutritional value.",
                    GlassfinPrototype.EncyclopediaTexture,
                    GlassfinPrototype.Icon,
                    PDAHandler.UnlockBasic,
                    null);
                PDAHandler.AddCustomScannerEntry(prefab.Info.TechType, 4f, false, encyclopediaKey);
            });
        }
        prefab.Register();
        RegisteredTechTypes.Add(definition.Id, prefab.Info.TechType);
        if (originalGlassfin)
            RegisterGlassfinEgg(prefab.Info.TechType);
        Plugin.Log.LogInfo(originalGlassfin
            ? $"Registered '{definition.Id}' with original procedural visuals/audio, scanner entry, and hatchable egg on the proven '{definition.SourceTechType}' gameplay shell."
            : $"Registered '{definition.Id}' definition from proven source TechType '{definition.SourceTechType}'.");
    }

    private static void RegisterGlassfinEgg(TechType glassfinTechType)
    {
        if (!Enum.TryParse("RabbitrayEgg", ignoreCase: false, out TechType sourceEggTechType))
        {
            _glassfinEggRegistrationDetail = "rollback: RabbitrayEgg source is unavailable";
            Plugin.Log.LogWarning("Glassfin egg registration skipped because the RabbitrayEgg TechType is unavailable in this game build.");
            return;
        }

        var info = PrefabInfo
            .WithTechType(
                "AbyssalEcologies_glassfin_egg",
                "Glassfin Egg",
                "A mineral-shelled egg with translucent anchoring veils.")
            .WithIcon(GlassfinPrototype.EggIcon);
        var egg = new CustomPrefab(info);
        var template = new EggTemplate(info, sourceEggTechType)
            .WithHatchingCreature(glassfinTechType)
            .WithHatchingTime(1.5f)
            .WithMass(2f)
            .WithMaxHealth(35f)
            .SetUndiscoveredTechType()
            .OnModifyPrefab(gameObject =>
            {
                if (GlassfinPrototype.TryReplaceEggVisuals(gameObject, out var detail))
                    _glassfinEggRegistrationDetail = detail;
                else
                    _glassfinEggRegistrationDetail = $"vanilla egg rollback visual active: {detail}";
            });
        egg.SetGameObject(template);
        egg.CreateCreatureEgg(1)
            .WithRequiredLargeAcuSize(1)
            .SetAcidImmune(true);
        egg.Register();
        Plugin.Log.LogInfo("Registered a distinct hatchable Glassfin egg with an original procedural shell on the proven Rabbit Ray egg gameplay shell.");
    }

    private static SpawnLocation ToSpawnLocation(string contentId, GeneratedPlacement placement)
    {
        var spawnPoint = LandmarkContentIds.Contains(contentId)
            ? FindLandmarkSitePlan(contentId, placement.Position)?.SpawnAnchor ?? placement.Position
            : placement.Position;
        var position = new Vector3(spawnPoint.X, spawnPoint.Y, spawnPoint.Z);
        var angles = new Vector3(placement.EulerAngles.X, placement.EulerAngles.Y, placement.EulerAngles.Z);
        var scale = Vector3.one * placement.Scale;
        return new SpawnLocation(position, angles, scale);
    }

    private static void ApplyAppearance(GameObject gameObject, ContentDefinition definition)
    {
        gameObject.name = definition.DisplayName;
        foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", definition.Tint);
                if (material.HasProperty("_GlowColor"))
                    material.SetColor("_GlowColor", definition.Glow);
                if (material.HasProperty("_GlowStrength"))
                    material.SetFloat("_GlowStrength", 1.4f);
                if (material.HasProperty("_GlowStrengthNight"))
                    material.SetFloat("_GlowStrengthNight", 2.2f);
            }
        }
    }

    private static SpawnInfo ToSpawnInfo(TechType techType, string contentId, GeneratedPlacement placement)
    {
        var location = ToSpawnLocation(contentId, placement);
        return new SpawnInfo(
            techType,
            location.Position,
            Quaternion.Euler(location.EulerAngles),
            location.Scale,
            gameObject => LogSuccessfulInstance(contentId, gameObject));
    }

    private static void LogSuccessfulInstance(string contentId, GameObject gameObject)
    {
        if (string.Equals(contentId, "glass-arch", StringComparison.Ordinal))
            CreateGlassKelpSeamount(gameObject, gameObject.transform.position);

        if (LandmarkContentIds.Contains(contentId))
        {
            var agent = gameObject.GetComponent<LandmarkGroundingAgent>() ?? gameObject.AddComponent<LandmarkGroundingAgent>();
            agent.Configure(contentId, gameObject.transform.position);
        }
        else if (string.Equals(contentId, "prism-kelp", StringComparison.Ordinal))
        {
            var agent = gameObject.GetComponent<SurfaceGroundingAgent>() ?? gameObject.AddComponent<SurfaceGroundingAgent>();
            agent.Configure(gameObject.transform.position);
        }

        InstantiationCounts.TryGetValue(contentId, out var totalCount);
        InstantiationCounts[contentId] = totalCount + 1;
        LiveInstances.Add(new WeakReference(gameObject));

        LoggedInstanceCounts.TryGetValue(contentId, out var count);
        if (count >= InstanceLogLimitPerContent)
            return;

        LoggedInstanceCounts[contentId] = count + 1;
        Plugin.Log.LogInfo($"Instantiated '{contentId}' at {gameObject.transform.position} (sample {count + 1}/{InstanceLogLimitPerContent}).");
    }

    internal static bool TryGroundLandmark(string contentId, GameObject gameObject, Vector3 original, out float adjustment, out string detail)
    {
        adjustment = 0f;
        detail = "no acceptable loaded terrain surface was found";
        var stableOffset = LandmarkStableOffset(contentId);
        var originPoint = new WorldPoint(original.x, original.y, original.z);
        var supportHalfExtents = GetLandmarkSupportHalfExtents(gameObject);

        foreach (var sample in LandmarkCandidateSites(contentId, originPoint, stableOffset))
        {
            if (WorldProtectionCatalog.TryFindExclusion(sample, 35f, out var areaId))
            {
                detail = $"nearest sample is inside protected area '{areaId}'";
                continue;
            }

            if (!TryFindFoundationSite(sample, original.y, supportHalfExtents, out var supportHeight, out var heightSpread, out var maximumSlope, out var terrainHits, out var supportFailure))
            {
                detail = supportFailure;
                continue;
            }

            var snapped = new Vector3(sample.X, supportHeight + LandmarkSurfaceOffset, sample.Z);
            adjustment = snapped.y - original.y;
            if (Mathf.Abs(adjustment) > MaximumLandmarkAdjustment)
            {
                detail = $"required vertical adjustment {adjustment:+0.0;-0.0;0.0} m exceeds {MaximumLandmarkAdjustment:0} m";
                continue;
            }

            if (TerrainPlacementResolver.IsProtectedBiome(snapped, out var biome))
            {
                detail = $"nearest loaded surface is in protected biome '{biome}'";
                continue;
            }

            if (TerrainPlacementResolver.HasProtectedWorldObject(snapped, 45f, out var protectedObject))
            {
                detail = $"nearest loaded surface is near protected world object '{protectedObject}'";
                continue;
            }

            var horizontalAdjustment = Mathf.Sqrt(
                ((snapped.x - original.x) * (snapped.x - original.x)) +
                ((snapped.z - original.z) * (snapped.z - original.z)));
            gameObject.transform.position = snapped;
            var foundationThickness = Mathf.Clamp(heightSpread + 1.5f, MinimumFoundationThickness, MaximumFoundationThickness);
            CreateLevelFoundation(gameObject, contentId, sample, supportHalfExtents, supportHeight, foundationThickness);
            detail = $"position=({snapped.x:0.0},{snapped.y:0.0},{snapped.z:0.0}), vertical={adjustment:+0.0;-0.0;0.0} m, horizontal={horizontalAdjustment:0.0} m, levelFoundation={supportHalfExtents.x:0.0}x{supportHalfExtents.y:0.0} m half-extents/{foundationThickness:0.0} m thick, terrainHits={terrainHits}/{LandmarkSupportSamples.Length}, terrainRelief={heightSpread:0.00} m, maxTerrainSlope={maximumSlope:0.0} degrees";
            return true;
        }

        return false;
    }

    private static IEnumerable<WorldPoint> LandmarkCandidateSites(string contentId, WorldPoint origin, int stableOffset)
    {
        yield return origin;

        var plan = LandmarkSitePlans
            .Where(item => string.Equals(item.ContentId, contentId, StringComparison.Ordinal))
            .OrderBy(item => HorizontalDistanceSquared(item.SpawnAnchor, origin))
            .FirstOrDefault();
        if (plan != null && HorizontalDistanceSquared(plan.SpawnAnchor, origin) < 1f)
        {
            var phase = (stableOffset & 255) * (Mathf.PI * 2f / 256f);
            foreach (var flora in plan.FloraSites)
            {
                for (var index = 0; index < FloraSiteRingSampleCount; index++)
                {
                    var angle = phase + (index * Mathf.PI * 2f / FloraSiteRingSampleCount);
                    yield return new WorldPoint(
                        flora.X + (Mathf.Cos(angle) * FloraSiteRingRadius),
                        origin.Y,
                        flora.Z + (Mathf.Sin(angle) * FloraSiteRingRadius));
                }
            }
        }

        foreach (var site in PlacementSearchPattern.Around(origin, stableOffset, LandmarkSearchRadius).Skip(1))
            yield return site;
    }

    private static LandmarkSitePlan? FindLandmarkSitePlan(string contentId, WorldPoint manifestPosition)
    {
        return LandmarkSitePlans
            .Where(item => string.Equals(item.ContentId, contentId, StringComparison.Ordinal))
            .OrderBy(item => HorizontalDistanceSquared(item.LandmarkPosition, manifestPosition))
            .FirstOrDefault();
    }

    private static WorldPoint PlannedLandmarkSpawnAnchor(string contentId, WorldPoint landmark, IReadOnlyList<WorldPoint> floraSites)
    {
        return landmark;
    }

    private static void CreateGlassKelpSeamount(GameObject landmark, Vector3 regionCenter)
    {
        if (_glassKelpSeamount != null)
            return;

        var seamount = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        seamount.name = "Abyssal Ecologies Glass Kelp Seamount";
        seamount.layer = 30;
        seamount.transform.position = new Vector3(
            regionCenter.x,
            regionCenter.y - GlassKelpSeamountTopBelowCenter - GlassKelpSeamountVerticalRadius,
            regionCenter.z);
        seamount.transform.rotation = Quaternion.identity;
        seamount.transform.localScale = new Vector3(
            GlassKelpSeamountRadius * 2f,
            GlassKelpSeamountVerticalRadius * 2f,
            GlassKelpSeamountRadius * 2f);

        var meshFilter = seamount.GetComponent<MeshFilter>();
        var sphereCollider = seamount.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.enabled = false;
            UnityEngine.Object.Destroy(sphereCollider);
        }
        if (meshFilter != null)
        {
            var meshCollider = seamount.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
        }

        var sourceRenderer = landmark.GetComponentInChildren<Renderer>(true);
        var seamountRenderer = seamount.GetComponent<Renderer>();
        if (sourceRenderer != null && sourceRenderer.sharedMaterial != null && seamountRenderer != null)
        {
            seamountRenderer.material = new Material(sourceRenderer.sharedMaterial);
            var material = seamountRenderer.material;
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", new Color(0.07f, 0.14f, 0.17f, 1f));
            if (material.HasProperty("_GlowColor"))
                material.SetColor("_GlowColor", new Color(0.02f, 0.16f, 0.2f, 1f));
            if (material.HasProperty("_GlowStrength"))
                material.SetFloat("_GlowStrength", 0.2f);
        }

        _glassKelpSeamount = seamount;
        Plugin.Log.LogInfo($"Created bounded Glass Kelp seamount at ({regionCenter.x:0.0},{regionCenter.y - GlassKelpSeamountTopBelowCenter:0.0},{regionCenter.z:0.0}) with radius {GlassKelpSeamountRadius:0} m.");
    }

    private static float HorizontalDistanceSquared(WorldPoint first, WorldPoint second)
    {
        var dx = first.X - second.X;
        var dz = first.Z - second.Z;
        return (dx * dx) + (dz * dz);
    }

    private static bool TryFindFoundationSite(
        WorldPoint candidate,
        float originalY,
        Vector2 supportHalfExtents,
        out float supportHeight,
        out float heightSpread,
        out float maximumSlope,
        out int terrainHits,
        out string failure)
    {
        supportHeight = float.MinValue;
        var minimumHeight = float.MaxValue;
        maximumSlope = 0f;
        heightSpread = 0f;
        terrainHits = 0;
        failure = "support footprint has no loaded terrain";

        for (var index = 0; index < LandmarkSupportSamples.Length; index++)
        {
            var supportSample = LandmarkSupportSamples[index];
            var x = candidate.X + (supportSample.x * supportHalfExtents.x);
            var z = candidate.Z + (supportSample.y * supportHalfExtents.y);
            var origin = new Vector3(x, originalY + LandmarkProbeHeight, z);
            if (!Physics.Raycast(origin, Vector3.down, out var hit, LandmarkProbeDistance, Voxeland.GetTerrainLayerMask(), QueryTriggerInteraction.Ignore))
            {
                if (index == 0)
                {
                    failure = "foundation center has no loaded terrain";
                    return false;
                }

                continue;
            }

            var slope = Vector3.Angle(hit.normal, Vector3.up);
            maximumSlope = Mathf.Max(maximumSlope, slope);
            if (slope > MaximumFoundationSiteSlope)
            {
                failure = $"foundation terrain slope {slope:0.0} exceeds {MaximumFoundationSiteSlope:0} degrees";
                return false;
            }

            terrainHits++;
            supportHeight = Mathf.Max(supportHeight, hit.point.y);
            minimumHeight = Mathf.Min(minimumHeight, hit.point.y);
        }

        heightSpread = supportHeight - minimumHeight;
        if (terrainHits < MinimumFoundationTerrainHits)
        {
            failure = $"foundation footprint has only {terrainHits}/{LandmarkSupportSamples.Length} loaded terrain samples; requires {MinimumFoundationTerrainHits}";
            return false;
        }

        return true;
    }

    private static void CreateLevelFoundation(
        GameObject landmark,
        string contentId,
        WorldPoint site,
        Vector2 supportHalfExtents,
        float topHeight,
        float thickness)
    {
        var foundation = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        foundation.name = $"Abyssal Ecologies {contentId} Level Foundation";
        foundation.layer = landmark.layer;
        foundation.transform.position = new Vector3(site.X, topHeight - (thickness * 0.5f), site.Z);
        foundation.transform.rotation = Quaternion.identity;
        foundation.transform.localScale = new Vector3(
            supportHalfExtents.x * 2.2f,
            thickness * 0.5f,
            supportHalfExtents.y * 2.2f);

        var sourceRenderer = landmark.GetComponentInChildren<Renderer>(true);
        var foundationRenderer = foundation.GetComponent<Renderer>();
        if (sourceRenderer != null && sourceRenderer.sharedMaterial != null && foundationRenderer != null)
        {
            foundationRenderer.material = new Material(sourceRenderer.sharedMaterial);
            var material = foundationRenderer.material;
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", new Color(0.12f, 0.16f, 0.18f, 1f));
            if (material.HasProperty("_GlowColor"))
                material.SetColor("_GlowColor", new Color(0.04f, 0.12f, 0.16f, 1f));
            if (material.HasProperty("_GlowStrength"))
                material.SetFloat("_GlowStrength", 0.35f);
        }

        foundation.transform.SetParent(landmark.transform, true);
    }

    private static Vector2 GetLandmarkSupportHalfExtents(GameObject gameObject)
    {
        if (!TryGetCombinedBounds(gameObject.GetComponentsInChildren<Collider>(true), out var bounds))
            TryGetCombinedBounds(gameObject.GetComponentsInChildren<Renderer>(true), out bounds);

        return new Vector2(
            Mathf.Clamp(bounds.extents.x * 0.8f, MinimumSupportHalfExtent, MaximumSupportHalfExtent),
            Mathf.Clamp(bounds.extents.z * 0.8f, MinimumSupportHalfExtent, MaximumSupportHalfExtent));
    }

    private static bool TryGetCombinedBounds<T>(IEnumerable<T> components, out Bounds combined) where T : Component
    {
        combined = default;
        var found = false;
        foreach (var component in components)
        {
            Bounds bounds;
            if (component is Collider collider)
            {
                if (!collider.enabled)
                    continue;
                bounds = collider.bounds;
            }
            else if (component is Renderer renderer)
            {
                if (!renderer.enabled)
                    continue;
                bounds = renderer.bounds;
            }
            else
            {
                continue;
            }

            if (!found)
            {
                combined = bounds;
                found = true;
            }
            else
            {
                combined.Encapsulate(bounds);
            }
        }

        return found;
    }

    internal static void RecordLandmarkGrounding(string contentId, bool grounded, float adjustment, string detail)
    {
        var result = new LandmarkGroundingResult(grounded, adjustment, detail);
        LandmarkGroundingResults[contentId] = result;
        if (grounded)
            Plugin.Log.LogInfo($"Grounded landmark '{contentId}' by {adjustment:+0.0;-0.0;0.0} m: {detail}.");
        else
            Plugin.Log.LogWarning($"Landmark grounding failed for '{contentId}': {detail}; leaving its manifest position unchanged.");
    }

    private static int LandmarkStableOffset(string contentId) => contentId switch
    {
        "glass-arch" => 101,
        "thermal-spire" => 211,
        "nursery-heart" => 307,
        _ => 0
    };

    private sealed class LandmarkSitePlan
    {
        public LandmarkSitePlan(string contentId, WorldPoint landmarkPosition, IReadOnlyList<WorldPoint> floraSites, WorldPoint spawnAnchor)
        {
            ContentId = contentId;
            LandmarkPosition = landmarkPosition;
            FloraSites = floraSites;
            SpawnAnchor = spawnAnchor;
        }

        public string ContentId { get; }
        public WorldPoint LandmarkPosition { get; }
        public IReadOnlyList<WorldPoint> FloraSites { get; }
        public WorldPoint SpawnAnchor { get; }
    }

    internal sealed class SpawnRegistrationMetrics
    {
        public SpawnRegistrationMetrics(int placementCount, int contentTypeCount)
        {
            PlacementCount = placementCount;
            ContentTypeCount = contentTypeCount;
        }

        public int PlacementCount { get; }
        public int ContentTypeCount { get; }
    }

    internal sealed class RuntimeSpawnMetrics
    {
        public RuntimeSpawnMetrics(int registeredPlacementCount, int instantiationCallbacks, int activeInstanceCount, IReadOnlyDictionary<string, int> instantiationCounts)
        {
            RegisteredPlacementCount = registeredPlacementCount;
            InstantiationCallbacks = instantiationCallbacks;
            ActiveInstanceCount = activeInstanceCount;
            InstantiationCounts = instantiationCounts;
        }

        public int RegisteredPlacementCount { get; }
        public int InstantiationCallbacks { get; }
        public int ActiveInstanceCount { get; }
        public IReadOnlyDictionary<string, int> InstantiationCounts { get; }
    }

    internal sealed class LandmarkGroundingMetrics
    {
        public LandmarkGroundingMetrics(int attemptedCount, int groundedCount, int failedCount, IReadOnlyDictionary<string, LandmarkGroundingResult> results)
        {
            AttemptedCount = attemptedCount;
            GroundedCount = groundedCount;
            FailedCount = failedCount;
            Results = results;
        }

        public int AttemptedCount { get; }
        public int GroundedCount { get; }
        public int FailedCount { get; }
        public IReadOnlyDictionary<string, LandmarkGroundingResult> Results { get; }
    }

    internal sealed class LandmarkGroundingResult
    {
        public LandmarkGroundingResult(bool grounded, float verticalAdjustment, string detail)
        {
            Grounded = grounded;
            VerticalAdjustment = verticalAdjustment;
            Detail = detail;
        }

        public bool Grounded { get; }
        public float VerticalAdjustment { get; }
        public string Detail { get; }
    }

    private sealed class ContentDefinition
    {
        public ContentDefinition(string id, string displayName, string description, string sourceTechType, Color tint, Color glow)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            SourceTechType = sourceTechType;
            Tint = tint;
            Glow = glow;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string SourceTechType { get; }
        public Color Tint { get; }
        public Color Glow { get; }
    }
}
