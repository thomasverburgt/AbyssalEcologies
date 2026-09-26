using System;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class CinderRayPrototype
{
    private static Sprite? _icon;
    private static Sprite? _eggIcon;
    private static Texture2D? _encyclopediaTexture;
    private static AudioClip? _thermalCall;

    public static Sprite Icon => _icon ??= CreateIcon(false);
    public static Sprite EggIcon => _eggIcon ??= CreateIcon(true);
    public static Texture2D EncyclopediaTexture => _encyclopediaTexture ??= CreateEncyclopediaTexture();

    public static bool TryReplaceVisuals(GameObject creature, out string detail)
    {
        try
        {
            const string modelName = "Abyssal Ecologies Original Cinder Ray Model";
            if (creature.transform.Find(modelName) != null)
            {
                detail = "original Cinder Ray model was already present";
                return true;
            }

            var inheritedRenderers = creature.GetComponentsInChildren<Renderer>(true);
            var inheritedMaterial = inheritedRenderers.Length == 0 ? null : inheritedRenderers[0].sharedMaterial;
            var root = new GameObject(modelName).transform;
            root.SetParent(creature.transform, false);
            var bodyMaterial = CreateMaterial(inheritedMaterial, new Color(0.25f, 0.05f, 0.025f, 1f), new Color(1f, 0.12f, 0.015f, 1f));
            var membraneMaterial = CreateMaterial(inheritedMaterial, new Color(0.7f, 0.12f, 0.025f, 0.9f), new Color(1f, 0.28f, 0.02f, 1f));

            var body = CreatePart("Cinder Ray Body", root, CreateBodyMesh(), bodyMaterial);
            var leftWing = CreateBone("Left Wing Bone", root, new Vector3(-0.32f, 0f, 0f));
            var leftRenderer = CreatePart("Left Thermal Wing", leftWing, CreateWingMesh(false), membraneMaterial).Renderer;
            var rightWing = CreateBone("Right Wing Bone", root, new Vector3(0.32f, 0f, 0f));
            var rightRenderer = CreatePart("Right Thermal Wing", rightWing, CreateWingMesh(true), membraneMaterial).Renderer;
            var tail = CreateBone("Tail Bone", root, new Vector3(0f, 0f, -0.75f));
            var tailRenderer = CreatePart("Cinder Ray Tail", tail, CreateTailMesh(), membraneMaterial).Renderer;

            foreach (var renderer in inheritedRenderers)
                renderer.enabled = false;

            var audio = creature.GetComponent<AudioSource>() ?? creature.AddComponent<AudioSource>();
            audio.clip = _thermalCall ??= CreateThermalCall();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 1f;
            audio.volume = 0.11f;
            audio.minDistance = 2f;
            audio.maxDistance = 28f;
            var controller = creature.GetComponent<CinderRayPrototypeController>() ?? creature.AddComponent<CinderRayPrototypeController>();
            controller.Configure(leftWing, rightWing, tail, new[] { body.Renderer, leftRenderer, rightRenderer, tailRenderer }, audio);
            detail = "procedural manta model with three-bone articulated rig, thermal animation, generated texture/icon, and synthesized call";
            return true;
        }
        catch (Exception exception)
        {
            detail = exception.Message;
            Plugin.Log.LogError($"Original Cinder Ray construction failed; retaining the Rabbit Ray rollback visual: {exception}");
            return false;
        }
    }

    public static bool TryReplaceEggVisuals(GameObject egg, out string detail)
    {
        try
        {
            const string modelName = "Abyssal Ecologies Original Cinder Ray Egg Model";
            var inheritedRenderers = egg.GetComponentsInChildren<Renderer>(true);
            var inheritedMaterial = inheritedRenderers.Length == 0 ? null : inheritedRenderers[0].sharedMaterial;
            var root = new GameObject(modelName).transform;
            root.SetParent(egg.transform, false);
            root.localScale = new Vector3(0.42f, 0.58f, 0.42f);
            CreatePart("Cinder Ray Egg Shell", root, CreateOctahedronMesh(), CreateMaterial(inheritedMaterial, new Color(0.34f, 0.04f, 0.015f, 1f), new Color(1f, 0.2f, 0.01f, 1f)));
            foreach (var renderer in inheritedRenderers)
                renderer.enabled = false;
            detail = "faceted ember shell with a pulsing thermal material";
            return true;
        }
        catch (Exception exception)
        {
            detail = exception.Message;
            Plugin.Log.LogError($"Original Cinder Ray egg construction failed; retaining the vanilla egg rollback visual: {exception}");
            return false;
        }
    }

    private static Transform CreateBone(string name, Transform parent, Vector3 position)
    {
        var bone = new GameObject(name).transform;
        bone.SetParent(parent, false);
        bone.localPosition = position;
        return bone;
    }

    private static Part CreatePart(string name, Transform parent, Mesh mesh, Material material)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return new Part(renderer);
    }

    private static Material CreateMaterial(Material? inherited, Color color, Color glow)
    {
        var material = inherited == null ? new Material(Shader.Find("Standard")) : new Material(inherited);
        material.name = "Abyssal Ecologies Cinder Ray Material";
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_GlowColor")) material.SetColor("_GlowColor", glow);
        if (material.HasProperty("_GlowStrength")) material.SetFloat("_GlowStrength", 1.5f);
        if (material.HasProperty("_GlowStrengthNight")) material.SetFloat("_GlowStrengthNight", 2.4f);
        return material;
    }

    private static Mesh CreateBodyMesh()
    {
        var vertices = new[]
        {
            new Vector3(0f, 0.18f, 0.95f), new Vector3(-0.42f, 0f, 0.25f), new Vector3(0f, -0.12f, -0.82f), new Vector3(0.42f, 0f, 0.25f),
            new Vector3(0f, -0.16f, 0.88f), new Vector3(-0.36f, -0.08f, 0.2f), new Vector3(0f, -0.2f, -0.72f), new Vector3(0.36f, -0.08f, 0.2f)
        };
        var triangles = new[] { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,5, 0,5,1, 1,5,6, 1,6,2, 2,6,7, 2,7,3, 3,7,4, 3,4,0 };
        return CreateMesh("Abyssal Ecologies Cinder Ray Body Mesh", vertices, triangles);
    }

    private static Mesh CreateWingMesh(bool right)
    {
        var sign = right ? 1f : -1f;
        var vertices = new[]
        {
            new Vector3(0f,0f,0.45f), new Vector3(sign * 1.5f,0f,0.05f), new Vector3(sign * 1.05f,0f,-0.75f), new Vector3(0f,0f,-0.55f),
            new Vector3(0f,-0.07f,0.45f), new Vector3(sign * 1.5f,-0.07f,0.05f), new Vector3(sign * 1.05f,-0.07f,-0.75f), new Vector3(0f,-0.07f,-0.55f)
        };
        var triangles = new[] { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,5, 0,5,1, 1,5,6, 1,6,2, 2,6,7, 2,7,3, 3,7,4, 3,4,0 };
        return CreateMesh("Abyssal Ecologies Cinder Ray Wing Mesh", vertices, triangles);
    }

    private static Mesh CreateTailMesh()
    {
        var vertices = new[] { new Vector3(-0.07f,0f,0f), new Vector3(0.07f,0f,0f), new Vector3(0.025f,0f,-1.65f), new Vector3(-0.025f,0f,-1.65f), new Vector3(0f,0.08f,-1.2f) };
        var triangles = new[] { 0,1,2, 0,2,3, 0,4,1, 1,4,2, 2,4,3, 3,4,0 };
        return CreateMesh("Abyssal Ecologies Cinder Ray Tail Mesh", vertices, triangles);
    }

    private static Mesh CreateOctahedronMesh()
    {
        var vertices = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        var triangles = new[] { 0,4,3, 0,2,4, 0,5,2, 0,3,5, 1,3,4, 1,4,2, 1,2,5, 1,5,3 };
        return CreateMesh("Abyssal Ecologies Cinder Ray Egg Mesh", vertices, triangles);
    }

    private static Mesh CreateMesh(string name, Vector3[] vertices, int[] triangles)
    {
        var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Sprite CreateIcon(bool egg)
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = egg ? "Cinder Ray Egg Icon" : "Cinder Ray Icon" };
        for (var y = 0; y < 64; y++)
        for (var x = 0; x < 64; x++)
        {
            var dx = Mathf.Abs(x - 32);
            var dy = Mathf.Abs(y - 34);
            var shape = egg ? ((x - 32f) * (x - 32f) / 225f) + ((y - 32f) * (y - 32f) / 484f) <= 1f : dy < 5 + (28 - dx) * 0.45f && dx < 29;
            texture.SetPixel(x, y, shape ? new Color(0.95f, 0.18f + y / 512f, 0.02f, 0.96f) : new Color(0f,0f,0f,0f));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f,0f,64f,64f), new Vector2(0.5f,0.5f), 100f);
    }

    private static Texture2D CreateEncyclopediaTexture()
    {
        var texture = new Texture2D(256, 128, TextureFormat.RGBA32, false) { name = "Cinder Ray Encyclopedia" };
        for (var y = 0; y < 128; y++)
        for (var x = 0; x < 256; x++)
        {
            var heat = Mathf.Max(0f, 1f - Vector2.Distance(new Vector2(x,y), new Vector2(150f,64f)) / 120f);
            texture.SetPixel(x, y, new Color(0.08f + heat * 0.22f, 0.015f + heat * 0.04f, 0.01f, 1f));
        }
        texture.Apply();
        return texture;
    }

    private static AudioClip CreateThermalCall()
    {
        const int sampleRate = 22050;
        var samples = new float[(int)(sampleRate * 0.9f)];
        for (var i = 0; i < samples.Length; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = Mathf.Sin(Mathf.PI * i / samples.Length);
            samples[i] = (Mathf.Sin(2f * Mathf.PI * 105f * t) + Mathf.Sin(2f * Mathf.PI * 210f * t) * 0.35f) * envelope * 0.12f;
        }
        var clip = AudioClip.Create("Abyssal Ecologies Cinder Ray Thermal Call", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private readonly struct Part
    {
        public Part(Renderer renderer) => Renderer = renderer;
        public Renderer Renderer { get; }
    }
}

internal sealed class CinderRayPrototypeController : MonoBehaviour
{
    private Transform? _leftWing;
    private Transform? _rightWing;
    private Transform? _tail;
    private Renderer[] _renderers = Array.Empty<Renderer>();
    private AudioSource? _audio;
    private float _phase;
    private float _nextCall;

    public static int CallCount { get; private set; }

    public void Configure(Transform leftWing, Transform rightWing, Transform tail, Renderer[] renderers, AudioSource audio)
    {
        _leftWing = leftWing;
        _rightWing = rightWing;
        _tail = tail;
        _renderers = renderers;
        _audio = audio;
        _phase = Mathf.Abs(GetInstanceID() % 101) / 101f * Mathf.PI * 2f;
        _nextCall = Time.time + 9f + Mathf.Abs(GetInstanceID() % 9);
    }

    private void Update()
    {
        if (_leftWing == null || _rightWing == null || _tail == null)
            return;
        var t = Time.time * 2.6f + _phase;
        var flap = Mathf.Sin(t) * 19f;
        _leftWing.localRotation = Quaternion.Euler(0f, 0f, flap);
        _rightWing.localRotation = Quaternion.Euler(0f, 0f, -flap);
        _tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 1.35f) * 20f, 0f);
        var pulse = 1.15f + (Mathf.Sin(t * 0.55f) + 1f) * 0.55f;
        foreach (var renderer in _renderers)
        foreach (var material in renderer.materials)
            if (material.HasProperty("_GlowStrength")) material.SetFloat("_GlowStrength", pulse);
        if (_audio != null && !_audio.isPlaying && Time.time >= _nextCall)
        {
            _audio.Play();
            CallCount++;
            _nextCall = Time.time + 15f + Mathf.Abs(GetInstanceID() % 8);
        }
    }
}
