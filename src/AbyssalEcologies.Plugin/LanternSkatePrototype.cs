using System;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class LanternSkatePrototype
{
    private static Sprite? _icon;
    private static Sprite? _eggIcon;
    private static Texture2D? _encyclopediaTexture;
    private static AudioClip? _call;

    public static Sprite Icon => _icon ??= CreateIcon(false);
    public static Sprite EggIcon => _eggIcon ??= CreateIcon(true);
    public static Texture2D EncyclopediaTexture => _encyclopediaTexture ??= CreateEncyclopediaTexture();

    public static bool TryReplaceVisuals(GameObject creature, out string detail)
    {
        try
        {
            const string modelName = "Abyssal Ecologies Original Lantern Skate Model";
            if (creature.transform.Find(modelName) != null)
            {
                detail = "original Lantern Skate model was already present";
                return true;
            }

            var inheritedRenderers = creature.GetComponentsInChildren<Renderer>(true);
            var inheritedMaterial = inheritedRenderers.Length == 0 ? null : inheritedRenderers[0].sharedMaterial;
            var root = new GameObject(modelName).transform;
            root.SetParent(creature.transform, false);
            var coreMaterial = CreateMaterial(inheritedMaterial, new Color(0.06f, 0.16f, 0.28f, 1f), new Color(0.18f, 0.75f, 1f, 1f));
            var veilMaterial = CreateMaterial(inheritedMaterial, new Color(0.18f, 0.5f, 0.72f, 0.76f), new Color(0.35f, 0.95f, 1f, 1f));

            var core = CreatePart("Lantern Skate Core", root, CreateCoreMesh(), coreMaterial).Renderer;
            var leftWing = CreateBone("Left Veil Bone", root, new Vector3(-0.18f, 0f, 0f));
            var left = CreatePart("Left Lantern Veil", leftWing, CreateWingMesh(false), veilMaterial).Renderer;
            var rightWing = CreateBone("Right Veil Bone", root, new Vector3(0.18f, 0f, 0f));
            var right = CreatePart("Right Lantern Veil", rightWing, CreateWingMesh(true), veilMaterial).Renderer;
            var leftRibbon = CreateBone("Left Ribbon Bone", root, new Vector3(-0.14f, 0f, -0.62f));
            var leftTail = CreatePart("Left Light Ribbon", leftRibbon, CreateRibbonMesh(false), veilMaterial).Renderer;
            var rightRibbon = CreateBone("Right Ribbon Bone", root, new Vector3(0.14f, 0f, -0.62f));
            var rightTail = CreatePart("Right Light Ribbon", rightRibbon, CreateRibbonMesh(true), veilMaterial).Renderer;

            foreach (var renderer in inheritedRenderers)
                renderer.enabled = false;

            var audio = creature.GetComponent<AudioSource>() ?? creature.AddComponent<AudioSource>();
            audio.clip = _call ??= CreateCall();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 1f;
            audio.volume = 0.09f;
            audio.minDistance = 2f;
            audio.maxDistance = 30f;
            var controller = creature.GetComponent<LanternSkatePrototypeController>() ?? creature.AddComponent<LanternSkatePrototypeController>();
            controller.Configure(leftWing, rightWing, leftRibbon, rightRibbon, new[] { core, left, right, leftTail, rightTail }, audio);
            detail = "procedural kite model with four-bone veil/ribbon rig, cold-light animation, generated texture/icon, and synthesized call";
            return true;
        }
        catch (Exception exception)
        {
            detail = exception.Message;
            Plugin.Log.LogError($"Original Lantern Skate construction failed; retaining the Jellyray rollback visual: {exception}");
            return false;
        }
    }

    public static bool TryReplaceEggVisuals(GameObject egg, out string detail)
    {
        try
        {
            var inheritedRenderers = egg.GetComponentsInChildren<Renderer>(true);
            var inheritedMaterial = inheritedRenderers.Length == 0 ? null : inheritedRenderers[0].sharedMaterial;
            var root = new GameObject("Abyssal Ecologies Original Lantern Skate Egg Model").transform;
            root.SetParent(egg.transform, false);
            root.localScale = new Vector3(0.44f, 0.58f, 0.44f);
            CreatePart("Lantern Skate Egg Lantern", root, CreateEggMesh(), CreateMaterial(inheritedMaterial, new Color(0.04f, 0.18f, 0.3f, 1f), new Color(0.25f, 0.9f, 1f, 1f)));
            foreach (var renderer in inheritedRenderers)
                renderer.enabled = false;
            detail = "ribbed blue lantern shell with a cold bioluminescent pulse";
            return true;
        }
        catch (Exception exception)
        {
            detail = exception.Message;
            Plugin.Log.LogError($"Original Lantern Skate egg construction failed; retaining the vanilla egg rollback visual: {exception}");
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
        var part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return new Part(renderer);
    }

    private static Material CreateMaterial(Material? inherited, Color color, Color glow)
    {
        var material = inherited == null ? new Material(Shader.Find("Standard")) : new Material(inherited);
        material.name = "Abyssal Ecologies Lantern Skate Material";
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_GlowColor")) material.SetColor("_GlowColor", glow);
        if (material.HasProperty("_GlowStrength")) material.SetFloat("_GlowStrength", 1.8f);
        if (material.HasProperty("_GlowStrengthNight")) material.SetFloat("_GlowStrengthNight", 2.8f);
        return material;
    }

    private static Mesh CreateCoreMesh() => CreateMesh("Lantern Skate Core Mesh",
        new[] { new Vector3(0f,.18f,.75f), new Vector3(-.42f,0f,.05f), new Vector3(0f,-.12f,-.68f), new Vector3(.42f,0f,.05f), new Vector3(0f,-.2f,.1f) },
        new[] { 0,1,4, 0,4,3, 1,2,4, 3,4,2, 0,3,2, 0,2,1 });

    private static Mesh CreateWingMesh(bool right)
    {
        var s = right ? 1f : -1f;
        return CreateMesh("Lantern Skate Veil Mesh",
            new[] { new Vector3(0f,0f,.45f), new Vector3(s*1.3f,0f,.05f), new Vector3(s*.72f,0f,-.7f), new Vector3(0f,0f,-.48f), new Vector3(s*.55f,-.08f,-.05f) },
            new[] { 0,1,4, 1,2,4, 2,3,4, 3,0,4, 0,3,2, 0,2,1 });
    }

    private static Mesh CreateRibbonMesh(bool right)
    {
        var s = right ? 1f : -1f;
        return CreateMesh("Lantern Skate Ribbon Mesh",
            new[] { new Vector3(0f,0f,0f), new Vector3(s*.12f,0f,-.55f), new Vector3(0f,0f,-1.55f), new Vector3(-s*.09f,0f,-.55f) },
            new[] { 0,1,2, 0,2,3, 0,2,1, 0,3,2 });
    }

    private static Mesh CreateEggMesh() => CreateMesh("Lantern Skate Egg Mesh",
        new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back, new Vector3(.7f,0f,.7f), new Vector3(-.7f,0f,-.7f) },
        new[] { 0,4,3, 0,2,4, 0,5,2, 0,3,5, 1,3,4, 1,4,2, 1,2,5, 1,5,3, 0,6,1, 0,1,7 });

    private static Mesh CreateMesh(string name, Vector3[] vertices, int[] triangles)
    {
        var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Sprite CreateIcon(bool egg)
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = egg ? "Lantern Skate Egg Icon" : "Lantern Skate Icon" };
        for (var y = 0; y < 64; y++)
        for (var x = 0; x < 64; x++)
        {
            var dx = Mathf.Abs(x - 32f);
            var dy = Mathf.Abs(y - 31f);
            var shape = egg ? dx / 15f + dy / 24f <= 1f : dx / 29f + dy / 18f <= 1f || (y < 31 && dx < 3f);
            texture.SetPixel(x, y, shape ? new Color(.22f,.82f,1f,.96f) : new Color(0f,0f,0f,0f));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f,0f,64f,64f), new Vector2(.5f,.5f), 100f);
    }

    private static Texture2D CreateEncyclopediaTexture()
    {
        var texture = new Texture2D(256, 128, TextureFormat.RGBA32, false) { name = "Lantern Skate Encyclopedia" };
        for (var y = 0; y < 128; y++)
        for (var x = 0; x < 256; x++)
        {
            var glow = Mathf.Max(0f, 1f - Vector2.Distance(new Vector2(x,y), new Vector2(132f,62f)) / 105f);
            texture.SetPixel(x, y, new Color(.01f + glow*.05f, .04f + glow*.18f, .1f + glow*.28f, 1f));
        }
        texture.Apply();
        return texture;
    }

    private static AudioClip CreateCall()
    {
        const int rate = 22050;
        var samples = new float[(int)(rate * 1.1f)];
        for (var i = 0; i < samples.Length; i++)
        {
            var t = i / (float)rate;
            var envelope = Mathf.Sin(Mathf.PI * i / samples.Length);
            samples[i] = (Mathf.Sin(2f*Mathf.PI*(180f + 45f*t)*t) + Mathf.Sin(2f*Mathf.PI*360f*t)*.22f) * envelope * .09f;
        }
        var clip = AudioClip.Create("Abyssal Ecologies Lantern Skate Call", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private readonly struct Part
    {
        public Part(Renderer renderer) => Renderer = renderer;
        public Renderer Renderer { get; }
    }
}

internal sealed class LanternSkatePrototypeController : MonoBehaviour
{
    private Transform? _leftWing;
    private Transform? _rightWing;
    private Transform? _leftRibbon;
    private Transform? _rightRibbon;
    private Renderer[] _renderers = Array.Empty<Renderer>();
    private AudioSource? _audio;
    private float _phase;
    private float _nextCall;
    public static int CallCount { get; private set; }

    public void Configure(Transform leftWing, Transform rightWing, Transform leftRibbon, Transform rightRibbon, Renderer[] renderers, AudioSource audio)
    {
        _leftWing = leftWing; _rightWing = rightWing; _leftRibbon = leftRibbon; _rightRibbon = rightRibbon;
        _renderers = renderers; _audio = audio;
        _phase = Mathf.Abs(GetInstanceID() % 113) / 113f * Mathf.PI * 2f;
        _nextCall = Time.time + 10f + Mathf.Abs(GetInstanceID() % 11);
    }

    private void Update()
    {
        if (_leftWing == null || _rightWing == null || _leftRibbon == null || _rightRibbon == null) return;
        var t = Time.time * 1.9f + _phase;
        var drift = Mathf.Sin(t) * 15f;
        _leftWing.localRotation = Quaternion.Euler(0f,0f,drift);
        _rightWing.localRotation = Quaternion.Euler(0f,0f,-drift);
        _leftRibbon.localRotation = Quaternion.Euler(0f,Mathf.Sin(t*1.35f)*24f,0f);
        _rightRibbon.localRotation = Quaternion.Euler(0f,Mathf.Sin(t*1.35f + .8f)*-24f,0f);
        var pulse = 1.35f + (Mathf.Sin(t*.42f) + 1f) * .75f;
        foreach (var renderer in _renderers)
        foreach (var material in renderer.materials)
            if (material.HasProperty("_GlowStrength")) material.SetFloat("_GlowStrength", pulse);
        if (_audio != null && !_audio.isPlaying && Time.time >= _nextCall)
        {
            _audio.Play(); CallCount++; _nextCall = Time.time + 18f + Mathf.Abs(GetInstanceID() % 10);
        }
    }
}
