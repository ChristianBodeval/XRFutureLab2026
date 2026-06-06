using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

public static class SplatSceneSetup
{
    [MenuItem("Tools/Image Blaster/Place Splat In Scene")]
    public static void PlaceSplat()
    {
        // 1) Load baked asset
        string assetPath = "Assets/_ProjectName/ImageBlaster/livingroom/world/SplatAsset/world-150k.asset";
        var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (asset == null) { Debug.LogError("Splat asset missing at " + assetPath); return; }
        Debug.Log($"[SplatSceneSetup] Asset loaded: {asset.GetType().FullName}");

        // 2) Find the GaussianSplatRenderer type and create a GameObject with it
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "GaussianSplatting");
        var rendererType = asm.GetType("GaussianSplatting.Runtime.GaussianSplatRenderer");
        if (rendererType == null) { Debug.LogError("Renderer type missing"); return; }

        // Clean up old one
        var existing = GameObject.Find("GaussianSplatWorld");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

        var go = new GameObject("GaussianSplatWorld");
        Undo.RegisterCreatedObjectUndo(go, "Create splat world");
        var renderer = go.AddComponent(rendererType);

        // Assign asset via reflection: field is `public GaussianSplatAsset m_Asset`
        var fAsset = rendererType.GetField("m_Asset", BindingFlags.Public | BindingFlags.Instance);
        if (fAsset == null) { Debug.LogError("m_Asset field missing"); return; }
        fAsset.SetValue(renderer, asset);
        Debug.Log("[SplatSceneSetup] Renderer asset assigned");

        // Position at origin (the splat is in its own coords; we'll adjust after seeing it)
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // 3) Add GaussianSplatURPFeature to the active URP renderer if not present
        var urpAsset = UniversalRenderPipeline.asset;
        if (urpAsset != null)
        {
            // Reflection: pipelineAsset.m_RendererDataList[0] is a UniversalRendererData
            var fRendererDataList = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (ScriptableRendererData[])fRendererDataList.GetValue(urpAsset);
            if (list != null && list.Length > 0)
            {
                var rData = list[0];
                bool already = rData.rendererFeatures.Any(f => f != null && f.GetType().Name == "GaussianSplatURPFeature");
                if (!already)
                {
                    var featureType = asm.GetType("GaussianSplatting.Runtime.GaussianSplatURPFeature");
                    var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(featureType);
                    feature.name = "GaussianSplatURPFeature";
                    rData.rendererFeatures.Add(feature);
                    AssetDatabase.AddObjectToAsset(feature, rData);
                    // Notify renderer to rebuild
                    var setDirty = typeof(ScriptableRendererData).GetMethod("SetDirty", BindingFlags.NonPublic | BindingFlags.Instance);
                    setDirty?.Invoke(rData, null);
                    EditorUtility.SetDirty(rData);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[SplatSceneSetup] Added GaussianSplatURPFeature to renderer: " + rData.name);
                }
                else
                {
                    Debug.Log("[SplatSceneSetup] GaussianSplatURPFeature already present");
                }
            }
            else
            {
                Debug.LogWarning("[SplatSceneSetup] No URP renderer data found");
            }
        }
        else
        {
            Debug.LogWarning("[SplatSceneSetup] No URP asset active");
        }

        // 4) Disable the skybox & sofa/TV so we can see splat clearly
        // (Actually keep them for now; user can toggle)

        // 5) Save scene
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[SplatSceneSetup] Done. GaussianSplatWorld id=" + go.GetInstanceID());
    }
}
