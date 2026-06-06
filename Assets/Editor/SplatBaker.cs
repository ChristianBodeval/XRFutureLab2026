using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

public static class SplatBaker
{
    public static void BakeWorld150k()
    {
        string spzAbs = Path.Combine(Application.dataPath, "_ProjectName/ImageBlaster/livingroom/world/world-150k.spz");
        string outFolder = "Assets/_ProjectName/ImageBlaster/livingroom/world/SplatAsset";
        if (!File.Exists(spzAbs)) { Debug.LogError("SPZ missing: " + spzAbs); return; }
        Directory.CreateDirectory(outFolder);

        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "GaussianSplattingEditor");
        if (asm == null) { Debug.LogError("Editor assembly missing"); return; }
        var creatorType = asm.GetType("GaussianSplatting.Editor.GaussianSplatAssetCreator");
        if (creatorType == null) { Debug.LogError("Creator type missing"); return; }

        // Open the window via the menu item to ensure proper initialization
        EditorApplication.ExecuteMenuItem("Tools/Gaussian Splats/Create GaussianSplatAsset");
        var windows = Resources.FindObjectsOfTypeAll(creatorType);
        if (windows.Length == 0) { Debug.LogError("Window not open"); return; }
        var win = windows[0];

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        creatorType.GetField("m_InputFile", flags).SetValue(win, spzAbs);
        creatorType.GetField("m_OutputFolder", flags).SetValue(win, outFolder);
        var fImport = creatorType.GetField("m_ImportCameras", flags);
        if (fImport != null) fImport.SetValue(win, true);
        var fPrev = creatorType.GetField("m_PrevFilePath", flags);
        if (fPrev != null) fPrev.SetValue(win, spzAbs);

        // Quality: Medium
        var dq = creatorType.GetNestedType("DataQuality", BindingFlags.NonPublic);
        var med = Enum.Parse(dq, "Medium");
        creatorType.GetField("m_Quality", flags).SetValue(win, med);
        creatorType.GetMethod("ApplyQualityLevel", flags).Invoke(win, null);

        Debug.Log($"[SplatBaker] Baking SPZ → {outFolder}/world-150k.asset");
        creatorType.GetMethod("CreateAsset", flags).Invoke(win, null);

        AssetDatabase.Refresh();
        string assetPath = $"{outFolder}/world-150k.asset";
        var produced = AssetDatabase.LoadMainAssetAtPath(assetPath);
        Debug.Log($"[SplatBaker] Produced at {assetPath}: {(produced != null ? produced.GetType().Name : "MISSING")}");

        ((EditorWindow)win).Close();
    }

    [MenuItem("Tools/Image Blaster/Bake World 150k SPZ")]
    public static void BakeMenu() => BakeWorld150k();
}
