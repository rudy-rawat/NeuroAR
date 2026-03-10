#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AddLoginGuardToScenes
{
    [MenuItem("Tools/AR Anatomy/Add LoginGuard to Start-Scene")]
    public static void AddGuard()
    {
        string scenePath = "Assets/Scenes/Start-Scene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Only add if not already present
        var existing = Object.FindFirstObjectByType<LoginGuard>();
        if (existing != null)
        {
            Debug.Log("[AddLoginGuard] LoginGuard already present in Start-Scene.");
        }
        else
        {
            var go = new GameObject("LoginGuard");
            go.AddComponent<LoginGuard>();
            Debug.Log("[AddLoginGuard] LoginGuard added to Start-Scene.");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AddLoginGuard] Start-Scene saved.");
    }
}
#endif
