#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ShaderFixer : MonoBehaviour
{
    [MenuItem("Tools/Convert URP Materials to Standard")]
    static void ConvertAllMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat.shader.name.Contains("Universal Render Pipeline"))
            {
                Debug.Log("Converting: " + mat.name);
                mat.shader = Shader.Find("Standard");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All URP materials converted to Standard.");
    }
}
#endif

