using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

public static class SplatDiagnose
{
    [MenuItem("Tools/Image Blaster/Diagnose Splat Setup")]
    public static void Diagnose()
    {
        var urp = UniversalRenderPipeline.asset;
        Debug.Log($"[Diag] URP asset: {urp?.name}");
        var fList = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
        var list = (ScriptableRendererData[])fList.GetValue(urp);
        foreach (var rd in list)
        {
            Debug.Log($"[Diag]  Renderer: {rd.name} ({rd.GetType().Name}) features={rd.rendererFeatures.Count}");
            for (int i = 0; i < rd.rendererFeatures.Count; i++)
            {
                var f = rd.rendererFeatures[i];
                Debug.Log($"[Diag]    [{i}] {f?.name} type={f?.GetType().FullName} active={(f as ScriptableRendererFeature)?.isActive}");
            }
        }

        // Splat go inspection
        var go = GameObject.Find("GaussianSplatWorld");
        if (go == null) { Debug.LogError("[Diag] GaussianSplatWorld missing"); return; }
        Debug.Log($"[Diag] GaussianSplatWorld pos={go.transform.position} active={go.activeInHierarchy}");

        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "GaussianSplatting");
        var t = asm.GetType("GaussianSplatting.Runtime.GaussianSplatRenderer");
        var comp = go.GetComponent(t);
        Debug.Log($"[Diag] Renderer component: present={comp != null} enabled={(comp as Behaviour)?.enabled}");
        var fAsset = t.GetField("m_Asset", BindingFlags.Public | BindingFlags.Instance);
        var asset = fAsset.GetValue(comp);
        Debug.Log($"[Diag] m_Asset: {asset}");
        if (asset != null)
        {
            var assetType = asset.GetType();
            foreach (var fld in assetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var n = fld.Name.ToLower();
                if (n.Contains("bound") || n.Contains("count") || n.Contains("splat") || n.Contains("min") || n.Contains("max"))
                {
                    try { Debug.Log($"[Diag]  asset.{fld.Name}={fld.GetValue(asset)}"); } catch (Exception e) { Debug.Log($"[Diag]  asset.{fld.Name}=ERR {e.Message}"); }
                }
            }
        }

        var cam = GameObject.Find("DebugViewpointCamera");
        Debug.Log($"[Diag] DebugCam pos={cam?.transform.position} fov={cam?.GetComponent<Camera>()?.fieldOfView}");
    }
}
