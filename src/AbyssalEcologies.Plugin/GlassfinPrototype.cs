using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class GlassfinPrototype
{
    private const string ModelName = "Abyssal Ecologies Original Glassfin Model";
    private static Texture2D? _skinTexture;
    private static Texture2D? _encyclopediaTexture;
    private static Sprite? _icon;
    private static Sprite? _eggIcon;
    private static AudioClip? _filterCall;

    public static Sprite Icon => _icon ??= CreateIcon();
    public static Sprite EggIcon => _eggIcon ??= CreateEggIcon();
    public static Texture2D EncyclopediaTexture => _encyclopediaTexture ??= CreateEncyclopediaTexture();

    public static bool TryReplaceVisuals(GameObject creature, out string detail)
    {
        try
        {
            if (creature.transform.Find(ModelName) != null)
            {
                detail = "original Glassfin model was already present";
                return true;
            }

            var inheritedRenderers = creature.GetComponentsInChildren<Renderer>(true);
            var inheritedMaterial = inheritedRenderers.Length == 0 ? null : inheritedRenderers[0].sharedMaterial;
            var model = BuildModel(creature.transform, inheritedMaterial);
            foreach (var renderer in inheritedRenderers)
                renderer.enabled = false;

            var controller = creature.GetComponent<GlassfinPrototypeController>() ?? creature.AddComponent<GlassfinPrototypeController>();
            controller.Configure(
                model.Tail,
                model.LeftFin,
                model.RightFin,
                model.LeftFan,
                model.RightFan,
                model.GlowRenderers,
                AddFilterCall(creature));
            detail = $"procedural model with {model.VertexCount} vertices, five-bone articulated rig, generated texture, animation, and filter call";
            return true;
        }
        catch (Exception exception)
        {
            detail = exception.Message;
            Plugin.Log.LogError($"Original Glassfin visual construction failed; retaining the Peeper rollback visual: {exception}");
            return false;
        }
    }

    public static bool TryReplaceEggVisuals(GameObject egg, out string detail)
    {
        try
        {
            const string eggModelName = "Abyssal Ecologies Original Glassfin Egg Model";
            if (egg.transform.Find(eggModelName) != null)
            {
                detail = "original Glassfin egg model was already present";
                return true;
            }

            var inheritedRenderers = egg.GetComponentsInChildren<Renderer>(true);
            var inheritedMaterial = inheritedRenderers.Length == 0 ? null : inheritedRenderers[0].sharedMaterial;
            var root = new GameObject(eggModelName).transform;
            root.SetParent(egg.transform, false);
            var shellMaterial = CreateMaterial(inheritedMaterial, new Color(0.08f, 0.58f, 0.66f, 0.96f), new Color(0.08f, 0.9f, 1f, 1f));
            var veilMaterial = CreateMaterial(inheritedMaterial, new Color(0.18f, 0.9f, 0.86f, 0.78f), new Color(0.18f, 1f, 1f, 1f));
            var shell = CreateMeshPart("Glassfin Egg Shell", root, CreateEllipsoidMesh(12, 9, 0.38f, 0.52f, 0.38f), shellMaterial);
            shell.Transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (var index = 0; index < 3; index++)
            {
                var veil = CreateMeshPart($"Glassfin Egg Veil {index + 1}", root, CreateFinMesh(0.22f, 0.3f, 0.025f, false), veilMaterial);
                veil.Transform.localPosition = new Vector3(0f, -0.08f, 0f);
                veil.Transform.localRotation = Quaternion.Euler(0f, index * 120f, 70f);
            }

            foreach (var renderer in inheritedRenderers)
                renderer.enabled = false;
            detail = "procedural mineral shell with three translucent anchoring veils";
            return true;
        }
        catch (Exception exception)
        {
            detail = exception.Message;
            Plugin.Log.LogError($"Original Glassfin egg visual construction failed; retaining the vanilla egg rollback visual: {exception}");
            return false;
        }
    }

    private static GlassfinModel BuildModel(Transform parent, Material? inheritedMaterial)
    {
        var root = new GameObject(ModelName).transform;
        root.SetParent(parent, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;

        var bodyMaterial = CreateMaterial(inheritedMaterial, new Color(0.10f, 0.82f, 0.92f, 0.92f), new Color(0.05f, 0.75f, 1f, 1f));
        var finMaterial = CreateMaterial(inheritedMaterial, new Color(0.16f, 0.95f, 0.88f, 0.82f), new Color(0.15f, 0.9f, 1f, 1f));
        var eyeMaterial = CreateMaterial(inheritedMaterial, new Color(0.01f, 0.04f, 0.07f, 1f), new Color(0.1f, 0.8f, 1f, 1f));

        var renderers = new List<Renderer>();
        var vertexCount = 0;
        var body = CreateMeshPart("Body", root, CreateEllipsoidMesh(12, 8, 0.55f, 0.36f, 1.05f), bodyMaterial);
        renderers.Add(body.Renderer);
        vertexCount += body.VertexCount;

        var tail = CreateBone("Tail Bone", root, new Vector3(0f, 0f, -0.92f));
        var tailPart = CreateMeshPart("Tail Veil", tail, CreateFinMesh(0.62f, 0.62f, 0.08f, true), finMaterial);
        tailPart.Transform.localPosition = new Vector3(0f, 0f, -0.34f);
        renderers.Add(tailPart.Renderer);
        vertexCount += tailPart.VertexCount;

        var leftFin = CreateBone("Left Fin Bone", root, new Vector3(-0.43f, -0.02f, 0.05f));
        var leftPart = CreateMeshPart("Left Fin", leftFin, CreateFinMesh(0.52f, 0.42f, 0.05f, false), finMaterial);
        leftPart.Transform.localRotation = Quaternion.Euler(0f, -18f, 12f);
        renderers.Add(leftPart.Renderer);
        vertexCount += leftPart.VertexCount;

        var rightFin = CreateBone("Right Fin Bone", root, new Vector3(0.43f, -0.02f, 0.05f));
        var rightPart = CreateMeshPart("Right Fin", rightFin, CreateFinMesh(0.52f, 0.42f, 0.05f, false), finMaterial);
        rightPart.Transform.localScale = new Vector3(-1f, 1f, 1f);
        rightPart.Transform.localRotation = Quaternion.Euler(0f, 18f, -12f);
        renderers.Add(rightPart.Renderer);
        vertexCount += rightPart.VertexCount;

        var leftFan = CreateBone("Left Filter Fan Bone", root, new Vector3(-0.27f, -0.02f, 0.78f));
        var leftFanPart = CreateMeshPart("Left Filter Fan", leftFan, CreateFinMesh(0.24f, 0.36f, 0.035f, false), finMaterial);
        leftFanPart.Transform.localRotation = Quaternion.Euler(-12f, -35f, 70f);
        renderers.Add(leftFanPart.Renderer);
        vertexCount += leftFanPart.VertexCount;

        var rightFan = CreateBone("Right Filter Fan Bone", root, new Vector3(0.27f, -0.02f, 0.78f));
        var rightFanPart = CreateMeshPart("Right Filter Fan", rightFan, CreateFinMesh(0.24f, 0.36f, 0.035f, false), finMaterial);
        rightFanPart.Transform.localScale = new Vector3(-1f, 1f, 1f);
        rightFanPart.Transform.localRotation = Quaternion.Euler(-12f, 35f, -70f);
        renderers.Add(rightFanPart.Renderer);
        vertexCount += rightFanPart.VertexCount;

        vertexCount += AddEye(root, new Vector3(-0.32f, 0.13f, 0.68f), eyeMaterial, renderers);
        vertexCount += AddEye(root, new Vector3(0.32f, 0.13f, 0.68f), eyeMaterial, renderers);

        return new GlassfinModel(tail, leftFin, rightFin, leftFan, rightFan, renderers.ToArray(), vertexCount);
    }

    private static int AddEye(Transform parent, Vector3 position, Material material, ICollection<Renderer> renderers)
    {
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Glassfin Eye";
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = position;
        eye.transform.localScale = new Vector3(0.11f, 0.11f, 0.07f);
        var collider = eye.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.Destroy(collider);
        var renderer = eye.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderers.Add(renderer);
        var filter = eye.GetComponent<MeshFilter>();
        return filter == null || filter.sharedMesh == null ? 0 : filter.sharedMesh.vertexCount;
    }

    private static Transform CreateBone(string name, Transform parent, Vector3 position)
    {
        var bone = new GameObject(name).transform;
        bone.SetParent(parent, false);
        bone.localPosition = position;
        return bone;
    }

    private static MeshPart CreateMeshPart(string name, Transform parent, Mesh mesh, Material material)
    {
        var part = new GameObject(name);
        part.transform.SetParent(parent, false);
        var filter = part.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return new MeshPart(part.transform, renderer, mesh.vertexCount);
    }

    private static Material CreateMaterial(Material? inherited, Color tint, Color glow)
    {
        var material = inherited == null
            ? new Material(Shader.Find("Standard"))
            : new Material(inherited);
        material.name = "Abyssal Ecologies Glassfin Material";
        if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
        if (material.HasProperty("_GlowColor")) material.SetColor("_GlowColor", glow);
        if (material.HasProperty("_GlowStrength")) material.SetFloat("_GlowStrength", 1.35f);
        if (material.HasProperty("_GlowStrengthNight")) material.SetFloat("_GlowStrengthNight", 2.1f);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", _skinTexture ??= CreateSkinTexture());
        return material;
    }

    private static Mesh CreateEllipsoidMesh(int segments, int rings, float radiusX, float radiusY, float radiusZ)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (var ring = 0; ring <= rings; ring++)
        {
            var v = ring / (float)rings;
            var phi = Mathf.PI * v;
            for (var segment = 0; segment <= segments; segment++)
            {
                var u = segment / (float)segments;
                var theta = Mathf.PI * 2f * u;
                var normal = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                vertices.Add(new Vector3(normal.x * radiusX, normal.y * radiusY, normal.z * radiusZ));
                normals.Add(new Vector3(normal.x / radiusX, normal.y / radiusY, normal.z / radiusZ).normalized);
                uv.Add(new Vector2(u, v));
            }
        }

        for (var ring = 0; ring < rings; ring++)
        {
            for (var segment = 0; segment < segments; segment++)
            {
                var current = ring * (segments + 1) + segment;
                var next = current + segments + 1;
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);
                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }

        var mesh = new Mesh { name = "Abyssal Ecologies Glassfin Body Mesh" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateFinMesh(float width, float length, float thickness, bool vertical)
    {
        var w = width;
        var l = length;
        var t = thickness;
        var vertices = vertical
            ? new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, w, -l), new Vector3(0f, -w, -l), new Vector3(t, 0f, 0f), new Vector3(t, w, -l), new Vector3(t, -w, -l) }
            : new[] { new Vector3(0f, 0f, 0f), new Vector3(-w, 0f, -l), new Vector3(-w * 0.3f, 0f, l * 0.15f), new Vector3(0f, t, 0f), new Vector3(-w, t, -l), new Vector3(-w * 0.3f, t, l * 0.15f) };
        var triangles = new[] { 0, 1, 2, 3, 5, 4, 0, 3, 4, 0, 4, 1, 1, 4, 5, 1, 5, 2, 2, 5, 3, 2, 3, 0 };
        var mesh = new Mesh { name = "Abyssal Ecologies Glassfin Fin Mesh", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Texture2D CreateSkinTexture()
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Abyssal Ecologies Glassfin Skin" };
        for (var y = 0; y < texture.height; y++)
        for (var x = 0; x < texture.width; x++)
        {
            var band = ((x + (y / 2)) / 7) % 2 == 0;
            var edge = Mathf.Abs((y / 63f) - 0.5f) * 2f;
            texture.SetPixel(x, y, band
                ? new Color(0.08f + edge * 0.08f, 0.72f, 0.82f, 0.92f)
                : new Color(0.16f, 0.92f, 0.88f, 0.82f));
        }
        texture.Apply();
        return texture;
    }

    private static Sprite CreateIcon()
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Abyssal Ecologies Glassfin Icon" };
        var clear = new Color(0f, 0f, 0f, 0f);
        for (var y = 0; y < 64; y++)
        for (var x = 0; x < 64; x++)
        {
            var nx = (x - 31.5f) / 24f;
            var ny = (y - 31.5f) / 13f;
            var body = (nx * nx) + (ny * ny) <= 1f;
            var tail = x < 14 && Mathf.Abs(y - 32) < (15 - x) * 0.8f;
            var fan = x > 45 && Mathf.Abs(y - 32) < 8 && ((x + y) % 3 != 0);
            texture.SetPixel(x, y, body || tail || fan
                ? new Color(0.12f, 0.9f, 0.92f, 0.95f)
                : clear);
        }
        texture.SetPixel(44, 36, new Color(0.02f, 0.08f, 0.12f, 1f));
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateEggIcon()
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Abyssal Ecologies Glassfin Egg Icon" };
        var clear = new Color(0f, 0f, 0f, 0f);
        for (var y = 0; y < 64; y++)
        for (var x = 0; x < 64; x++)
        {
            var nx = (x - 31.5f) / 17f;
            var ny = (y - 31.5f) / 23f;
            var shell = (nx * nx) + (ny * ny) <= 1f;
            var stripe = ((x + y) / 6) % 2 == 0;
            texture.SetPixel(x, y, shell
                ? stripe ? new Color(0.1f, 0.82f, 0.86f, 0.95f) : new Color(0.06f, 0.46f, 0.58f, 0.95f)
                : clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Texture2D CreateEncyclopediaTexture()
    {
        var texture = new Texture2D(256, 128, TextureFormat.RGBA32, false) { name = "Abyssal Ecologies Glassfin Encyclopedia" };
        for (var y = 0; y < texture.height; y++)
        for (var x = 0; x < texture.width; x++)
        {
            var depth = y / (float)(texture.height - 1);
            var glow = Mathf.Max(0f, 1f - Vector2.Distance(new Vector2(x, y), new Vector2(150f, 64f)) / 110f);
            texture.SetPixel(x, y, new Color(0.01f + glow * 0.03f, 0.06f + depth * 0.05f, 0.1f + glow * 0.13f, 1f));
        }
        texture.Apply();
        return texture;
    }

    private static AudioSource AddFilterCall(GameObject creature)
    {
        var source = creature.GetComponent<AudioSource>() ?? creature.AddComponent<AudioSource>();
        source.clip = _filterCall ??= CreateFilterCall();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.volume = 0.12f;
        source.minDistance = 2f;
        source.maxDistance = 22f;
        return source;
    }

    private static AudioClip CreateFilterCall()
    {
        const int sampleRate = 22050;
        const float duration = 0.72f;
        var samples = new float[(int)(sampleRate * duration)];
        for (var index = 0; index < samples.Length; index++)
        {
            var t = index / (float)sampleRate;
            var envelope = Mathf.Sin(Mathf.PI * t / duration);
            var frequency = 420f + (180f * t / duration);
            samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.18f;
        }
        var clip = AudioClip.Create("Abyssal Ecologies Glassfin Filter Call", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private readonly struct MeshPart
    {
        public MeshPart(Transform transform, Renderer renderer, int vertexCount)
        {
            Transform = transform;
            Renderer = renderer;
            VertexCount = vertexCount;
        }

        public Transform Transform { get; }
        public Renderer Renderer { get; }
        public int VertexCount { get; }
    }

    private readonly struct GlassfinModel
    {
        public GlassfinModel(Transform tail, Transform leftFin, Transform rightFin, Transform leftFan, Transform rightFan, Renderer[] glowRenderers, int vertexCount)
        {
            Tail = tail;
            LeftFin = leftFin;
            RightFin = rightFin;
            LeftFan = leftFan;
            RightFan = rightFan;
            GlowRenderers = glowRenderers;
            VertexCount = vertexCount;
        }

        public Transform Tail { get; }
        public Transform LeftFin { get; }
        public Transform RightFin { get; }
        public Transform LeftFan { get; }
        public Transform RightFan { get; }
        public Renderer[] GlowRenderers { get; }
        public int VertexCount { get; }
    }
}

internal sealed class GlassfinPrototypeController : MonoBehaviour
{
    private Transform? _tail;
    private Transform? _leftFin;
    private Transform? _rightFin;
    private Transform? _leftFan;
    private Transform? _rightFan;
    private Renderer[] _renderers = Array.Empty<Renderer>();
    private AudioSource? _audioSource;
    private Rigidbody? _rigidbody;
    private float _phase;
    private float _nextCallTime;
    private bool _feeding;

    public static int CallCount { get; private set; }
    public bool IsFeeding => _feeding;

    public void Configure(Transform tail, Transform leftFin, Transform rightFin, Transform leftFan, Transform rightFan, Renderer[] renderers, AudioSource audioSource)
    {
        _tail = tail;
        _leftFin = leftFin;
        _rightFin = rightFin;
        _leftFan = leftFan;
        _rightFan = rightFan;
        _renderers = renderers;
        _audioSource = audioSource;
        _rigidbody = GetComponent<Rigidbody>();
        _phase = Mathf.Abs(GetInstanceID() % 97) / 97f * Mathf.PI * 2f;
        _nextCallTime = Time.time + 7f + Mathf.Abs(GetInstanceID() % 11);
    }

    private void OnDisable()
    {
        _feeding = false;
    }

    private void Update()
    {
        if (_tail == null || _leftFin == null || _rightFin == null || _leftFan == null || _rightFan == null)
            return;

        var speed = _rigidbody == null ? 0f : _rigidbody.velocity.magnitude;
        var feedingNow = speed < 1.15f;
        if (feedingNow != _feeding)
            _feeding = feedingNow;

        var t = Time.time * (feedingNow ? 2.2f : 4.6f) + _phase;
        _tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(t) * (feedingNow ? 12f : 28f), 0f);
        _leftFin.localRotation = Quaternion.Euler(0f, -18f, 12f + Mathf.Sin(t * 0.72f) * 18f);
        _rightFin.localRotation = Quaternion.Euler(0f, 18f, -12f - Mathf.Sin(t * 0.72f) * 18f);
        var fanAngle = feedingNow ? 28f + Mathf.Sin(t * 1.3f) * 12f : 8f;
        _leftFan.localRotation = Quaternion.Euler(-12f, -35f, 70f + fanAngle);
        _rightFan.localRotation = Quaternion.Euler(-12f, 35f, -70f - fanAngle);

        var pulse = 1.1f + ((Mathf.Sin(t * 0.8f) + 1f) * 0.35f);
        foreach (var renderer in _renderers)
        foreach (var material in renderer.materials)
            if (material.HasProperty("_GlowStrength"))
                material.SetFloat("_GlowStrength", pulse);

        if (feedingNow && _audioSource != null && !_audioSource.isPlaying && Time.time >= _nextCallTime)
        {
            _audioSource.Play();
            CallCount++;
            _nextCallTime = Time.time + 11f + Mathf.Abs(GetInstanceID() % 7);
        }
    }
}
