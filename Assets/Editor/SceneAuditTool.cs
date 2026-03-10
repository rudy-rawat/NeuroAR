using UnityEngine;
using UnityEditor;

public class SceneAuditTool
{
    [MenuItem("Tools/AR Anatomy/Audit Scene State")]
    public static void Run()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            Debug.Log($"ROOT: {root.name}");
            foreach (Transform child in root.transform)
            {
                var components = child.GetComponents<Component>();
                string missingFlag = "";
                foreach (var c in components) if (c == null) missingFlag = " [MISSING SCRIPT]";
                Debug.Log($"  CHILD: {child.name} active={child.gameObject.activeSelf}{missingFlag}");
                foreach (Transform grandchild in child)
                {
                    var gcomps = grandchild.GetComponents<Component>();
                    string gmissing = "";
                    foreach (var gc in gcomps) if (gc == null) gmissing = " [MISSING SCRIPT]";
                    Debug.Log($"    GRANDCHILD: {grandchild.name} active={grandchild.gameObject.activeSelf}{gmissing}");
                }
            }
        }

        // Explicitly search for narration objects including inactive
        Debug.Log("--- Narration Objects (including inactive) ---");
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in allTransforms)
        {
            if (t.name.Contains("Narration") || t.name.Contains("narration"))
            {
                string parentName = t.parent != null ? t.parent.name : "ROOT";
                Debug.Log($"  FOUND: {t.name} | parent={parentName} | active={t.gameObject.activeSelf} | layer={t.gameObject.layer}");
            }
        }
    }
}

