using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class RoadmapSceneBuilder
{
    [MenuItem("AR-Anatomy/Build Roadmap Scene", false, 10)]
    public static void BuildScene()
    {
        // 1. Create a brand new scene
        UnityEngine.SceneManagement.Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 2. Setup Services GameObject
        GameObject servicesGO = new GameObject("Services");
        servicesGO.AddComponent<RoadmapService>();

        // 3. Setup Canvas
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Setup EventSystem if missing
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 4. Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.15f, 1f); // Dark theme
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 5. Header / Overall Focus Text
        GameObject headerGO = new GameObject("HeaderLayer");
        headerGO.transform.SetParent(canvasGO.transform, false);
        RectTransform headerRect = headerGO.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.85f);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        // Back Button
        GameObject backBtnGO = new GameObject("BackButton");
        backBtnGO.transform.SetParent(headerGO.transform, false);
        Image backImg = backBtnGO.AddComponent<Image>();
        backImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button backBtn = backBtnGO.AddComponent<Button>();
        RectTransform backR = backBtnGO.GetComponent<RectTransform>();
        backR.anchorMin = new Vector2(0.05f, 0.2f);
        backR.anchorMax = new Vector2(0.15f, 0.8f);
        backR.offsetMin = Vector2.zero;
        backR.offsetMax = Vector2.zero;
        
        GameObject backTextGO = new GameObject("Text");
        backTextGO.transform.SetParent(backBtnGO.transform, false);
        TextMeshProUGUI backText = backTextGO.AddComponent<TextMeshProUGUI>();
        backText.text = "Back";
        backText.color = Color.white;
        backText.alignment = TextAlignmentOptions.Center;
        backText.fontSize = 30;
        RectTransform backTextR = backTextGO.GetComponent<RectTransform>();
        backTextR.anchorMin = Vector2.zero;
        backTextR.anchorMax = Vector2.one;
        backTextR.offsetMin = Vector2.zero;
        backTextR.offsetMax = Vector2.zero;

        SceneChanger sc = backBtnGO.AddComponent<SceneChanger>();
        sc.sceneName = "Start-Scene";
        sc.sceneChangeButton = backBtn;

        // Focus Text
        GameObject focusGO = new GameObject("OverallFocusText");
        focusGO.transform.SetParent(headerGO.transform, false);
        TextMeshProUGUI focusText = focusGO.AddComponent<TextMeshProUGUI>();
        focusText.text = "Loading Learning Roadmap...";
        focusText.fontSize = 36;
        focusText.alignment = TextAlignmentOptions.Left;
        focusText.color = Color.white;
        focusText.enableWordWrapping = true;
        RectTransform focusRect = focusGO.GetComponent<RectTransform>();
        focusRect.anchorMin = new Vector2(0.18f, 0.1f);
        focusRect.anchorMax = new Vector2(0.95f, 0.9f);
        focusRect.offsetMin = Vector2.zero;
        focusRect.offsetMax = Vector2.zero;

        // 6. Scroll View setup
        GameObject scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(canvasGO.transform, false);
        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        
        Image scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = new Color(0, 0, 0, 0.2f);
        RectTransform scrollR = scrollGO.GetComponent<RectTransform>();
        scrollR.anchorMin = new Vector2(0.1f, 0.05f);
        scrollR.anchorMax = new Vector2(0.9f, 0.80f);
        scrollR.offsetMin = Vector2.zero;
        scrollR.offsetMax = Vector2.zero;

        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        viewportGO.AddComponent<Image>().color = Color.clear; // Mask needs an image
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform viewportR = viewportGO.GetComponent<RectTransform>();
        viewportR.anchorMin = Vector2.zero;
        viewportR.anchorMax = Vector2.one;
        viewportR.offsetMin = Vector2.zero;
        viewportR.offsetMax = Vector2.zero;

        GameObject contentGO = new GameObject("StepsContainer");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentR = contentGO.AddComponent<RectTransform>();
        contentR.anchorMin = new Vector2(0, 1);
        contentR.anchorMax = new Vector2(1, 1);
        contentR.pivot = new Vector2(0.5f, 1);
        contentR.offsetMin = new Vector2(0, -500); 
        contentR.offsetMax = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 30;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentR;
        scrollRect.viewport = viewportR;

        // 7. Generate Prefabs
        EnsureFolders();

        GameObject stepPref = CreateStepPrefab();
        GameObject resPref = CreateResourcePrefab();

        GameObject savedStepPrefab = PrefabUtility.SaveAsPrefabAsset(stepPref, "Assets/Prefabs/Roadmap/RoadmapStep_Pref.prefab");
        GameObject savedResPrefab = PrefabUtility.SaveAsPrefabAsset(resPref, "Assets/Prefabs/Roadmap/ResourceBtn_Pref.prefab");

        Object.DestroyImmediate(stepPref);
        Object.DestroyImmediate(resPref);

        // 8. Attach Script to Canvas
        RoadmapUI roadmapUI = canvasGO.AddComponent<RoadmapUI>();
        roadmapUI.overallFocusText = focusText;

        // 9. Save and wrap up
        string scenePath = "Assets/Scenes/RoadmapScene.unity";
        EditorSceneManager.SaveScene(newScene, scenePath, false);
        
        Debug.Log($"<color=green><b>Successfully generated RoadmapScene + Prefabs!</b></color>\nScene saved at {scenePath}");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
            
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Roadmap"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Roadmap");
            
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    private static GameObject CreateStepPrefab()
    {
        GameObject stepGO = new GameObject("RoadmapStep_Pref");
        Image stepImg = stepGO.AddComponent<Image>();
        stepImg.color = new Color(0.2f, 0.22f, 0.28f, 1f);

        VerticalLayoutGroup stepVlg = stepGO.AddComponent<VerticalLayoutGroup>();
        stepVlg.padding = new RectOffset(20, 20, 20, 20);
        stepVlg.spacing = 15;
        stepVlg.childControlHeight = true;
        stepVlg.childControlWidth = true;
        stepVlg.childForceExpandHeight = false;
        stepVlg.childForceExpandWidth = true;

        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(stepGO.transform, false);
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Step Title";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.4f, 0.8f, 1f, 1f); // Light blue
        
        GameObject descGO = new GameObject("DescText");
        descGO.transform.SetParent(stepGO.transform, false);
        TextMeshProUGUI descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text = "Step Description";
        descText.fontSize = 20;
        descText.color = Color.white;
        descText.enableWordWrapping = true;

        GameObject resContainerGO = new GameObject("ResourceContainer");
        resContainerGO.transform.SetParent(stepGO.transform, false);
        VerticalLayoutGroup resVlg = resContainerGO.AddComponent<VerticalLayoutGroup>();
        resVlg.spacing = 10;
        resVlg.childControlHeight = true;
        resVlg.childControlWidth = true;
        resVlg.childForceExpandHeight = false;

        return stepGO;
    }

    private static GameObject CreateResourcePrefab()
    {
        GameObject resGO = new GameObject("ResourceBtn_Pref");
        Image resImg = resGO.AddComponent<Image>();
        resImg.color = new Color(0.3f, 0.6f, 0.9f, 1f); // Nice blue button
        
        Button resBtn = resGO.AddComponent<Button>();
        resBtn.targetGraphic = resImg;
        
        LayoutElement le = resGO.AddComponent<LayoutElement>();
        le.minHeight = 60; // Button height

        GameObject resTextGO = new GameObject("Text (TMP)");
        resTextGO.transform.SetParent(resGO.transform, false);
        TextMeshProUGUI resText = resTextGO.AddComponent<TextMeshProUGUI>();
        resText.text = "Resource Link";
        resText.color = Color.white;
        resText.alignment = TextAlignmentOptions.Center;
        resText.fontSize = 24;
        
        RectTransform resTextR = resTextGO.GetComponent<RectTransform>();
        resTextR.anchorMin = Vector2.zero;
        resTextR.anchorMax = Vector2.one;
        resTextR.offsetMin = new Vector2(10, 10);
        resTextR.offsetMax = new Vector2(-10, -10);

        return resGO;
    }
}
