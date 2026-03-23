using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartSceneRoadmapButtonSetup
{
    [MenuItem("AR-Anatomy/Add Roadmap Button to Start-Scene", false, 11)]
    public static void AddRoadmapButton()
    {
        string scenePath = "Assets/Scenes/Start-Scene.unity";
        
        // Ensure we handle saving current scene
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        // Open the Start-Scene
        UnityEngine.SceneManagement.Scene startScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // Find the Main Canvas
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        Canvas mainCanvas = null;
        
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "StartSceneCanvas" || canvas.name == "Canvas")
            {
                mainCanvas = canvas;
                break;
            }
        }

        if (mainCanvas == null)
        {
            if (canvases.Length > 0)
            {
                mainCanvas = canvases[0]; // fallback
            }
            else
            {
                Debug.LogError("Could not find a Canvas in Start-Scene!");
                return;
            }
        }

        // Check if button already exists
        Transform existingBtn = mainCanvas.transform.Find("RoadmapButton");
        if (existingBtn != null)
        {
            Debug.LogWarning("RoadmapButton already exists in the scene. Deleting it to create a fresh one.");
            Object.DestroyImmediate(existingBtn.gameObject);
        }

        // Create the Roadmap Button
        GameObject buttonGO = new GameObject("RoadmapButton");
        buttonGO.transform.SetParent(mainCanvas.transform, false);
        
        // Setup RectTransform
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -150); // Placed slightly below center, assuming other buttons are above
        rect.sizeDelta = new Vector2(400, 80);

        // Setup Image
        Image bgImg = buttonGO.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.6f, 0.8f, 1f); // Nice blue color

        // Setup Button
        Button btn = buttonGO.AddComponent<Button>();
        btn.targetGraphic = bgImg;

        // Add TextMeshPro
        GameObject textGO = new GameObject("Text (TMP)");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "View Learning Roadmap";
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Attach SceneChanger
        SceneChanger sceneChanger = buttonGO.AddComponent<SceneChanger>();
        sceneChanger.sceneName = "RoadmapScene";
        sceneChanger.sceneChangeButton = btn;

        // Save scene
        EditorSceneManager.MarkSceneDirty(startScene);
        EditorSceneManager.SaveScene(startScene, scenePath);
        
        Debug.Log("<color=green><b>Successfully injected RoadmapButton into Start-Scene!</b></color>");
    }
}
