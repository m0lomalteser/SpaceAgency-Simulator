using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class RocketAssembly
{
    public static readonly string[] SlotOrder = { "P", "U", "T", "E", "B" };

    // Normalized visual heights so oversized OBJ meshes stack consistently.
    private static readonly Dictionary<string, float> TargetHeights = new Dictionary<string, float>
    {
        { "E", 1.15f },
        { "T", 2.60f },
        { "U", 1.55f },
        { "P", 1.35f },
        { "B", 2.40f }
    };

    public static string SavePath => Path.Combine(Application.persistentDataPath, "rocket_build.json");

    public static RocketSaveData LoadSave()
    {
        string path = SavePath;
        if (!File.Exists(path))
        {
            string fallback = Path.Combine(Application.dataPath, "rocket_build.json");
            if (!File.Exists(fallback)) return null;
            path = fallback;
        }

        try
        {
            return JsonUtility.FromJson<RocketSaveData>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    public static PartRegistry LoadRegistry(PartRegistry preferred = null)
    {
        if (preferred != null) return preferred;

        PartRegistry registry = Resources.Load<PartRegistry>("PartRegistry");
#if UNITY_EDITOR
        if (registry == null)
        {
            registry = UnityEditor.AssetDatabase.LoadAssetAtPath<PartRegistry>("Assets/PartRegister/PartRegistry.asset");
        }
#endif
        return registry;
    }

    public static List<RocketPartData> ResolveParts(PartRegistry registry, RocketSaveData save)
    {
        var result = new List<RocketPartData>();
        if (registry == null) return result;

        if (save != null && save.parts != null && save.parts.Count > 0)
        {
            foreach (string slot in SlotOrder)
            {
                SavedPartData saved = save.parts.Find(p => p != null && p.slot == slot);
                if (saved == null) continue;
                RocketPartData part = registry.GetPartById(saved.id);
                if (part != null) result.Add(part);
            }
            return result;
        }

        foreach (string slot in SlotOrder)
        {
            List<RocketPartData> list = registry.GetPartsBySlot(slot);
            if (list != null && list.Count > 0) result.Add(list[0]);
        }
        return result;
    }

    public static float BuildStackedRocket(Transform root, IList<RocketPartData> parts, out float totalHeight)
    {
        totalHeight = 0f;
        if (root == null) return 0f;

        ClearChildren(root);

        float currentY = 0f;
        string[] stackSlots = { "E", "T", "U", "P" };

        for (int s = 0; s < stackSlots.Length; s++)
        {
            RocketPartData part = FindBySlot(parts, stackSlots[s]);
            if (part == null || part.modelPrefab == null) continue;

            GameObject instance = Object.Instantiate(part.modelPrefab, root);
            NormalizeAndPlace(instance, root, stackSlots[s], currentY, out float height);
            currentY += height;
        }

        RocketPartData booster = FindBySlot(parts, "B");
        if (booster != null && booster.modelPrefab != null)
        {
            GameObject boosterObj = Object.Instantiate(booster.modelPrefab, root);
            NormalizeAndPlace(boosterObj, root, "B", 0f, out _);
        }

        totalHeight = currentY;
        return currentY;
    }

    public static void PlaceOnPad(Transform rocketRoot, Transform pad, float padHeightFraction = 0.28f)
    {
        if (rocketRoot == null || pad == null) return;

        Bounds padBounds = CalculateBounds(pad.gameObject);
        Bounds rocketBounds = CalculateBounds(rocketRoot.gameObject);
        if (rocketBounds.size.y < 0.001f) return;

        float targetHeight = Mathf.Max(8f, padBounds.size.y * padHeightFraction);
        float scale = targetHeight / rocketBounds.size.y;
        rocketRoot.localScale = Vector3.one * scale;

        rocketBounds = CalculateBounds(rocketRoot.gameObject);
        Vector3 padTop = new Vector3(padBounds.center.x, padBounds.max.y, padBounds.center.z);
        Vector3 offset = padTop - new Vector3(rocketBounds.center.x, rocketBounds.min.y, rocketBounds.center.z);
        rocketRoot.position += offset;
    }

    private static RocketPartData FindBySlot(IList<RocketPartData> parts, string slot)
    {
        if (parts == null) return null;
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null && parts[i].slot == slot) return parts[i];
        }
        return null;
    }

    private static void NormalizeAndPlace(GameObject instance, Transform root, string slot, float targetBottomY, out float height)
    {
        height = 0f;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        float rawHeight = Mathf.Max(0.01f, b.size.y);
        float target = TargetHeights.TryGetValue(slot, out float h) ? h : 1.5f;
        float scale = target / rawHeight;
        instance.transform.localScale = instance.transform.localScale * scale;

        renderers = instance.GetComponentsInChildren<Renderer>();
        b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = root.InverseTransformPoint(b.center);
        Vector3 localMin = root.InverseTransformPoint(b.min);
        height = b.size.y;
        instance.transform.localPosition = new Vector3(-localCenter.x, targetBottomY - localMin.y, -localCenter.z);
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    public static Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
