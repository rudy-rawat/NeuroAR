#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class FeedbackPanelSetup
{
    [MenuItem("AR-Anatomy/Add Feedback Panel To Start-Scene", false, 20)]
    public static void AddFeedbackPanel()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Start-Scene.unity", OpenSceneMode.Single);

        var canvasGo = GameObject.Find("StartSceneCanvas");
        if (canvasGo == null)
        {
            Debug.LogError("[FeedbackPanelSetup] StartSceneCanvas not found.");
            return;
        }

        if (canvasGo.transform.Find("FeedbackOpenButton") != null || canvasGo.transform.Find("FeedbackPanel") != null)
        {
            Debug.Log("[FeedbackPanelSetup] Feedback UI already exists. Skipping duplicate creation.");
            return;
        }

        // Open feedback button
        var openBtnGo = CreateImg(canvasGo.transform, "FeedbackOpenButton",
            new Vector2(0f, 0f), new Vector2(120, 90), new Vector2(260, 80),
            new Color(0.18f, 0.52f, 0.78f, 1f));
        var openBtn = openBtnGo.AddComponent<Button>();
        openBtn.targetGraphic = openBtnGo.GetComponent<Image>();
        CreateTMP(openBtnGo.transform, "Label", "Feedback", 30, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);

        // Panel root
        var panelGo = CreateImg(canvasGo.transform, "FeedbackPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(960, 1400),
            new Color(0.08f, 0.10f, 0.16f, 0.98f));
        panelGo.SetActive(false);

        CreateCentredTMP(panelGo.transform, "Title", "AI Feedback", 44,
            new Color(0.85f, 0.92f, 1f), FontStyles.Bold, new Vector2(0, -80), new Vector2(700, 70));

        // Close button
        var closeBtnGo = CreateImg(panelGo.transform, "CloseButton",
            new Vector2(1f, 1f), new Vector2(-50, -50), new Vector2(70, 70),
            new Color(0.28f, 0.28f, 0.34f, 1f));
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnGo.GetComponent<Image>();
        CreateTMP(closeBtnGo.transform, "Label", "X", 32, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);

        // Input field
        var inputGo = CreateImg(panelGo.transform, "FeedbackInputField",
            new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(860, 260),
            new Color(0.14f, 0.17f, 0.24f, 1f));
        var input = inputGo.AddComponent<TMP_InputField>();

        var textAreaGo = CreateChild(inputGo.transform, "Text Area");
        var textAreaRect = textAreaGo.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(20, 20);
        textAreaRect.offsetMax = new Vector2(-20, -20);
        textAreaGo.AddComponent<RectMask2D>();

        var placeholderGo = CreateChild(textAreaGo.transform, "Placeholder");
        var placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
        placeholder.text = "What felt wrong or difficult?";
        placeholder.fontSize = 28;
        placeholder.color = new Color(0.62f, 0.68f, 0.78f, 0.9f);
        placeholder.alignment = TextAlignmentOptions.TopLeft;
        Stretch(placeholderGo.GetComponent<RectTransform>());

        var textGo = CreateChild(textAreaGo.transform, "Text");
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = 30;
        text.color = new Color(0.90f, 0.94f, 1f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        Stretch(textGo.GetComponent<RectTransform>());

        input.textViewport = textAreaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.characterLimit = 4000;

        // Status text
        var status = CreateCentredTMP(panelGo.transform, "StatusText", "Share feedback to personalize AI responses.",
            26, new Color(0.74f, 0.83f, 1f), FontStyles.Normal, new Vector2(0, -510), new Vector2(860, 60));

        // Action buttons
        var tooComplex = CreateActionButton(panelGo.transform, "TooComplexButton", "Too Complex", new Vector2(-220, -620));
        var tooEasy = CreateActionButton(panelGo.transform, "TooEasyButton", "Too Easy", new Vector2(220, -620));
        var slower = CreateActionButton(panelGo.transform, "SlowerPaceButton", "Slower Pace", new Vector2(-220, -740));
        var faster = CreateActionButton(panelGo.transform, "FasterPaceButton", "Faster Pace", new Vector2(220, -740));
        var moderate = CreateActionButton(panelGo.transform, "ModeratePaceButton", "Moderate Pace", new Vector2(0, -860));
        var moreVisual = CreateActionButton(panelGo.transform, "MoreVisualButton", "Need More Visuals", new Vector2(-220, -980));
        var lessVisual = CreateActionButton(panelGo.transform, "LessVisualButton", "Need Fewer Visuals", new Vector2(220, -980));

        // Attach controller
        var ui = canvasGo.AddComponent<FeedbackPanelUI>();
        ui.panelRoot = panelGo;
        ui.openButton = openBtn;
        ui.closeButton = closeBtn;
        ui.feedbackInput = input;
        ui.statusText = status;
        ui.tooComplexBtn = tooComplex;
        ui.tooEasyBtn = tooEasy;
        ui.slowerPaceBtn = slower;
        ui.fasterPaceBtn = faster;
        ui.moderatePaceBtn = moderate;
        ui.moreVisualBtn = moreVisual;
        ui.lessVisualBtn = lessVisual;

        // Ensure service object exists in scene
        if (GameObject.Find("FeedbackService") == null)
        {
            var serviceGo = new GameObject("FeedbackService");
            serviceGo.AddComponent<FeedbackService>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FeedbackPanelSetup] Feedback panel added and Start-Scene saved.");
    }

    private static Button CreateActionButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = CreateImg(parent, name,
            new Vector2(0.5f, 1f), pos, new Vector2(380, 90), new Color(0.18f, 0.40f, 0.62f, 1f));
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        CreateTMP(go.transform, "Label", label, 28, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
        return btn;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CreateImg(Transform parent, string name,
        Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = CreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        float size, Color color, FontStyles style, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = CreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static TextMeshProUGUI CreateCentredTMP(Transform parent, string name, string text,
        float size, Color color, FontStyles style, Vector2 pos, Vector2 sizeDelta)
    {
        var go = CreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = sizeDelta;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
#endif
