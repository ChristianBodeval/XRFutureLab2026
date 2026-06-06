using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Reflection;

public static class SplatFrame
{
    [MenuItem("Tools/Image Blaster/Frame Splat With Camera")]
    public static void Frame()
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "GaussianSplatting");
        var rType = asm.GetType("GaussianSplatting.Runtime.GaussianSplatRenderer");
        var go = GameObject.Find("GaussianSplatWorld");
        if (go == null) { Debug.LogError("[SplatFrame] GaussianSplatWorld missing"); return; }
        var comp = go.GetComponent(rType);
        var fAsset = rType.GetField("m_Asset", BindingFlags.Public | BindingFlags.Instance);
        var asset = fAsset.GetValue(comp);
        if (asset == null) { Debug.LogError("[SplatFrame] m_Asset is null"); return; }
        var aType = asset.GetType();
        var bMin = (Vector3)aType.GetProperty("boundsMin").GetValue(asset);
        var bMax = (Vector3)aType.GetProperty("boundsMax").GetValue(asset);
        var count = (int)aType.GetProperty("splatCount").GetValue(asset);
        var center = (bMin + bMax) * 0.5f;
        var size = bMax - bMin;
        Debug.Log($"[SplatFrame] splatCount={count} min={bMin} max={bMax} center={center} size={size}");

        var cam = GameObject.Find("DebugViewpointCamera");
        if (cam == null) { Debug.LogError("[SplatFrame] DebugViewpointCamera missing"); return; }
        var c = cam.GetComponent<Camera>();
        float maxDim = Mathf.Max(size.x, size.y, size.z);
        float dist = maxDim * 1.5f;
        if (dist < 0.5f) dist = 2.0f;
        Vector3 camPos = center + new Vector3(0, size.y * 0.3f, -dist);
        cam.transform.position = camPos;
        cam.transform.LookAt(center);
        c.nearClipPlane = 0.01f;
        c.farClipPlane = Mathf.Max(50f, dist * 5f);
        Debug.Log($"[SplatFrame] DebugCam pos={camPos} dist={dist} far={c.farClipPlane}");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}
