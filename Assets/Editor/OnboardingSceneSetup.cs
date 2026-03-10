#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Editor utility — creates the Onboarding-Scene for new students.
/// Run via:  Tools → AR Anatomy → Setup Onboarding Scene
/// </summary>
public static class OnboardingSceneSetup
{
    [MenuItem("Tools/AR Anatomy/Setup Onboarding Scene")]
    public static void CreateOnboardingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ──────────────────────────────────────────────────
        var camGo = new GameObject("Main Camera");
        var cam   = camGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.10f);
        cam.orthographic    = true;
        camGo.AddComponent<AudioListener>();
        camGo.tag = "MainCamera";

        // ── EventSystem ─────────────────────────────────────────────
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // ── Canvas ───────────────────────────────────────────────────
        var canvasGo = new GameObject("OnboardingCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Background ───────────────────────────────────────────────
        var bgGo  = MakePanel(canvasGo.transform, "BG", new Color(0.06f, 0.06f, 0.10f));
        Stretch(bgGo.GetComponent<RectTransform>());

        // ── Shared heading ───────────────────────────────────────────
        MakeTMP(bgGo.transform, "Heading", "Tell us about yourself",
            50, Color.white, FontStyles.Bold,
            new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(900, 80));

        // ── Step label ───────────────────────────────────────────────
        var stepLabelGo = MakeTMPObj(bgGo.transform, "StepLabel", "Step 1 of 3",
            32, new Color(0.5f, 0.6f, 0.9f), FontStyles.Normal,
            new Vector2(0.5f, 1f), new Vector2(0, -210), new Vector2(700, 50));

        // ─── STEP 1 — Grade ──────────────────────────────────────────
        var step1Go = new GameObject("StepGrade");
        step1Go.transform.SetParent(bgGo.transform, false);
        step1Go.AddComponent<RectTransform>();
        Stretch(step1Go.GetComponent<RectTransform>());

        MakeTMP(step1Go.transform, "Q1", "Which class are you in?",
            40, new Color(0.85f, 0.85f, 1f), FontStyles.Normal,
            new Vector2(0.5f, 1f), new Vector2(0, -310), new Vector2(800, 60));

        Button btn9  = MakeBtn(step1Go.transform, "BtnGrade9",  "Class 9",  new Vector2(0, -430));
        Button btn10 = MakeBtn(step1Go.transform, "BtnGrade10", "Class 10", new Vector2(0, -550));
        Button btn11 = MakeBtn(step1Go.transform, "BtnGrade11", "Class 11", new Vector2(0, -670));
        Button btn12 = MakeBtn(step1Go.transform, "BtnGrade12", "Class 12", new Vector2(0, -790));

        // ─── STEP 2 — Knowledge ──────────────────────────────────────
        var step2Go = new GameObject("StepKnowledge");
        step2Go.transform.SetParent(bgGo.transform, false);
        step2Go.AddComponent<RectTransform>();
        Stretch(step2Go.GetComponent<RectTransform>());
        step2Go.SetActive(false);

        MakeTMP(step2Go.transform, "Q2", "How well do you know human anatomy?",
            40, new Color(0.85f, 0.85f, 1f), FontStyles.Normal,
            new Vector2(0.5f, 1f), new Vector2(0, -310), new Vector2(800, 60));

        Button btnBeg  = MakeBtn(step2Go.transform, "BtnBeginner",     "Beginner – just starting",     new Vector2(0, -430));
        Button btnMid  = MakeBtn(step2Go.transform, "BtnIntermediate", "Intermediate – studied before", new Vector2(0, -560));
        Button btnAdv  = MakeBtn(step2Go.transform, "BtnAdvanced",     "Advanced – know it well",       new Vector2(0, -690));

        // ─── STEP 3 — Goal ───────────────────────────────────────────
        var step3Go = new GameObject("StepGoal");
        step3Go.transform.SetParent(bgGo.transform, false);
        step3Go.AddComponent<RectTransform>();
        Stretch(step3Go.GetComponent<RectTransform>());
        step3Go.SetActive(false);

        MakeTMP(step3Go.transform, "Q3", "What is your main goal?",
            40, new Color(0.85f, 0.85f, 1f), FontStyles.Normal,
            new Vector2(0.5f, 1f), new Vector2(0, -310), new Vector2(800, 60));

        Button btnExam = MakeBtn(step3Go.transform, "BtnExam",      "Exam preparation",  new Vector2(0, -430));
        Button btnCur  = MakeBtn(step3Go.transform, "BtnCuriosity", "Just curious",      new Vector2(0, -560));
        Button btnRev  = MakeBtn(step3Go.transform, "BtnRevision",  "Revision / review", new Vector2(0, -690));

        // ─── DONE screen ─────────────────────────────────────────────
        var stepDoneGo = new GameObject("StepDone");
        stepDoneGo.transform.SetParent(bgGo.transform, false);
        stepDoneGo.AddComponent<RectTransform>();
        Stretch(stepDoneGo.GetComponent<RectTransform>());
        stepDoneGo.SetActive(false);

        MakeTMP(stepDoneGo.transform, "DoneMsg",
            "🎉 You're all set!\nLoading your personalised AR experience…",
            44, Color.white, FontStyles.Bold,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 200));

        // ── OnboardingUI component ────────────────────────────────────
        var uiComp = canvasGo.AddComponent<OnboardingUI>();
        uiComp.stepGrade     = step1Go;
        uiComp.stepKnowledge = step2Go;
        uiComp.stepGoal      = step3Go;
        uiComp.stepDone      = stepDoneGo;
        uiComp.btnGrade9     = btn9;
        uiComp.btnGrade10    = btn10;
        uiComp.btnGrade11    = btn11;
        uiComp.btnGrade12    = btn12;
        uiComp.btnBeginner   = btnBeg;
        uiComp.btnIntermediate = btnMid;
        uiComp.btnAdvanced   = btnAdv;
        uiComp.btnExam       = btnExam;
        uiComp.btnCuriosity  = btnCur;
        uiComp.btnRevision   = btnRev;
        uiComp.stepLabel     = stepLabelGo;
        uiComp.homeSceneName = "Start-Scene";

        // ── Save scene ────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Onboarding-Scene.unity");

        // ── Add to Build Settings (index 1, between Login and Start) ──
        AddSceneToBuildSettings("Assets/Scenes/Onboarding-Scene.unity", 1);

        Debug.Log("[OnboardingSceneSetup] Onboarding-Scene created and saved.");
        EditorUtility.DisplayDialog("Done", "Onboarding-Scene created at Assets/Scenes/Onboarding-Scene.unity", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────

    static GameObject MakePanel(Transform parent, string name, Color color)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = rt.offsetMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static Button MakeBtn(Transform parent, string name, string label, Vector2 pos)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta        = new Vector2(760, 100);
        var img  = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.25f);
        var btn  = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var trect = txtGo.AddComponent<RectTransform>();
        trect.anchorMin = Vector2.zero; trect.anchorMax = Vector2.one;
        trect.offsetMin = trect.offsetMax = Vector2.zero;
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 34; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return btn;
    }

    static void MakeTMP(Transform parent, string name, string text, float size,
        Color color, FontStyles style, Vector2 anchor, Vector2 pos, Vector2 sizeDelta)
    {
        MakeTMPObj(parent, name, text, size, color, style, anchor, pos, sizeDelta);
    }

    static TextMeshProUGUI MakeTMPObj(Transform parent, string name, string text, float size,
        Color color, FontStyles style, Vector2 anchor, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta        = sizeDelta;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static void AddSceneToBuildSettings(string path, int index)
    {
        var existing = EditorBuildSettings.scenes;
        // Check not already present
        foreach (var s in existing)
            if (s.path == path) return;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(existing);
        list.Insert(index, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"[OnboardingSceneSetup] Added '{path}' to build settings at index {index}.");
    }
}
#endif
