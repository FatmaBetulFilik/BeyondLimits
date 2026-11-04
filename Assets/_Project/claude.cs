using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class PipelineCheck
{
    [MenuItem("Tools/Check Pipeline")]
    static void Check()
    {
        var p = GraphicsSettings.currentRenderPipeline;
        Debug.Log(p == null ? "Built-in aktif" : "AKTIF PIPELINE: " + p.name, p);
    }
}