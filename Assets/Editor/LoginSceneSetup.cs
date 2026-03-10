#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility – creates and wires up the Login-Scene.
/// Run via:  Tools → AR Anatomy → Setup Login Scene
/// The script deletes itself from the Editor folder after running.
/// </summary>
public static class LoginSceneSetup
{
    [MenuItem("Tools/AR Anatomy/Setup Login Scene")]
    public static void CreateLoginScene()
    {
        // ── 1. Create / open a new scene ──────────────────────────────────
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene = EditorSceneManager.GetActiveScene();

        // ── 2. Camera ─────────────────────────────────────────────────────
        var camGo = new GameObject("Main Camera");
        var cam   = camGo.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.08f, 0.08f, 0.12f);   // dark navy
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        camGo.AddComponent<AudioListener>();
        camGo.tag = "MainCamera";

        // ── 3. Directional Light ──────────────────────────────────────────
        var lightGo = new GameObject("Directional Light");
        var dl      = lightGo.AddComponent<Light>();
        dl.type      = LightType.Directional;
        dl.intensity = 1f;
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ── 4. EventSystem ────────────────────────────────────────────────
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // ── 5. Canvas (full-screen) ───────────────────────────────────────
        var canvasGo = new GameObject("LoginCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── 6. Background panel ───────────────────────────────────────────
        var bgGo  = CreateUIChild(canvasGo.transform, "Background");
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.14f);
        Stretch(bgGo.GetComponent<RectTransform>());

        // ── 7. App logo / title text ──────────────────────────────────────
        var titleGo   = CreateUIChild(bgGo.transform, "TitleText");
        var titleRect  = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.75f);
        titleRect.anchorMax = new Vector2(0.5f, 0.75f);
        titleRect.sizeDelta = new Vector2(700, 120);
        titleRect.anchoredPosition = Vector2.zero;
        var titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "AR Anatomy";
        titleTMP.fontSize  = 72;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color     = Color.white;
        titleTMP.fontStyle = FontStyles.Bold;

        // ── 8. Subtitle ───────────────────────────────────────────────────
        var subGo   = CreateUIChild(bgGo.transform, "SubtitleText");
        var subRect  = subGo.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.68f);
        subRect.anchorMax = new Vector2(0.5f, 0.68f);
        subRect.sizeDelta = new Vector2(700, 60);
        subRect.anchoredPosition = Vector2.zero;
        var subTMP = subGo.AddComponent<TextMeshProUGUI>();
        subTMP.text      = "Interactive Human Anatomy in AR";
        subTMP.fontSize  = 32;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color     = new Color(0.7f, 0.7f, 0.9f);

        // ── 9. Google Sign-In button ──────────────────────────────────────
        var googleBtnGo    = CreateUIChild(bgGo.transform, "GoogleSignInButton");
        var googleRect     = googleBtnGo.GetComponent<RectTransform>();
        googleRect.anchorMin = new Vector2(0.5f, 0.50f);
        googleRect.anchorMax = new Vector2(0.5f, 0.50f);
        googleRect.sizeDelta = new Vector2(560, 100);
        googleRect.anchoredPosition = Vector2.zero;
        var googleBtn = googleBtnGo.AddComponent<Button>();
        var googleImg = googleBtnGo.AddComponent<Image>();
        googleImg.color = new Color(0.96f, 0.96f, 0.96f);
        googleBtn.targetGraphic = googleImg;
        // Label
        var googleLabelGo = CreateUIChild(googleBtnGo.transform, "Label");
        var glRect = googleLabelGo.GetComponent<RectTransform>();
        glRect.anchorMin = Vector2.zero;
        glRect.anchorMax = Vector2.one;
        glRect.offsetMin = glRect.offsetMax = Vector2.zero;
        var googleTMP = googleLabelGo.AddComponent<TextMeshProUGUI>();
        googleTMP.text      = "Sign in with Google";
        googleTMP.fontSize  = 36;
        googleTMP.alignment = TextAlignmentOptions.Center;
        googleTMP.color     = new Color(0.2f, 0.2f, 0.2f);

        // ── 10. "Continue as Guest" button ───────────────────────────────
        var guestBtnGo = CreateUIChild(bgGo.transform, "GuestButton");
        var guestRect  = guestBtnGo.GetComponent<RectTransform>();
        guestRect.anchorMin = new Vector2(0.5f, 0.40f);
        guestRect.anchorMax = new Vector2(0.5f, 0.40f);
        guestRect.sizeDelta = new Vector2(560, 80);
        guestRect.anchoredPosition = Vector2.zero;
        var guestBtn = guestBtnGo.AddComponent<Button>();
        var guestImg = guestBtnGo.AddComponent<Image>();
        guestImg.color = new Color(0.2f, 0.2f, 0.3f);
        guestBtn.targetGraphic = guestImg;
        var guestLabelGo = CreateUIChild(guestBtnGo.transform, "Label");
        var guestLabelRect = guestLabelGo.GetComponent<RectTransform>();
        guestLabelRect.anchorMin = Vector2.zero;
        guestLabelRect.anchorMax = Vector2.one;
        guestLabelRect.offsetMin = guestLabelRect.offsetMax = Vector2.zero;
        var guestTMP = guestLabelGo.AddComponent<TextMeshProUGUI>();
        guestTMP.text      = "Continue as Guest";
        guestTMP.fontSize  = 32;
        guestTMP.alignment = TextAlignmentOptions.Center;
        guestTMP.color     = new Color(0.8f, 0.8f, 0.9f);

        // ── 11. Status text ───────────────────────────────────────────────
        var statusGo   = CreateUIChild(bgGo.transform, "StatusText");
        var statusRect  = statusGo.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.32f);
        statusRect.anchorMax = new Vector2(0.5f, 0.32f);
        statusRect.sizeDelta = new Vector2(700, 60);
        statusRect.anchoredPosition = Vector2.zero;
        var statusTMP = statusGo.AddComponent<TextMeshProUGUI>();
        statusTMP.text      = "";
        statusTMP.fontSize  = 28;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color     = new Color(0.6f, 0.8f, 1.0f);

        // ── 12. Loading spinner placeholder ──────────────────────────────
        var spinnerGo   = CreateUIChild(bgGo.transform, "LoadingSpinner");
        var spinnerRect  = spinnerGo.GetComponent<RectTransform>();
        spinnerRect.anchorMin = new Vector2(0.5f, 0.25f);
        spinnerRect.anchorMax = new Vector2(0.5f, 0.25f);
        spinnerRect.sizeDelta = new Vector2(80, 80);
        spinnerRect.anchoredPosition = Vector2.zero;
        var spinnerImg   = spinnerGo.AddComponent<Image>();
        spinnerImg.color = new Color(1f, 1f, 1f, 0.8f);
        spinnerGo.SetActive(false);

        // ── 13. UserSession GameObject ────────────────────────────────────
        var sessionGo = new GameObject("UserSession");
        sessionGo.AddComponent<UserSession>();

        // ── 14. LoginManager on Canvas ────────────────────────────────────
        var lm = canvasGo.AddComponent<LoginManager>();
        lm.googleSignInButton = googleBtn;
        lm.guestButton        = guestBtn;
        lm.statusText         = statusTMP;
        lm.loadingSpinner     = spinnerGo;
        lm.homeSceneName      = "Start-Scene";

        // ── 15. Save scene ────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, "Assets/Scenes/Login-Scene.unity");
        if (saved)
            Debug.Log("[LoginSceneSetup] Login-Scene saved to Assets/Scenes/Login-Scene.unity");
        else
            Debug.LogError("[LoginSceneSetup] Failed to save Login-Scene!");

        // ── 16. Add to Build Settings ─────────────────────────────────────
        AddSceneToBuildSettings("Assets/Scenes/Login-Scene.unity", 0);

        EditorUtility.DisplayDialog(
            "Login Scene Created",
            "Login-Scene has been set up and saved!\n\n" +
            "Next steps:\n" +
            "1. Import Firebase Auth Unity SDK\n" +
            "2. Import Google Sign-In Unity Plugin\n" +
            "3. Add FIREBASE_ENABLED to Scripting Defines\n" +
            "4. Assign your Web Client ID in LoginManager",
            "Got it");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static GameObject CreateUIChild(Transform parent, string name)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin     = Vector2.zero;
        rt.anchorMax     = Vector2.one;
        rt.offsetMin     = Vector2.zero;
        rt.offsetMax     = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void AddSceneToBuildSettings(string scenePath, int insertAtIndex)
    {
        var scenes     = EditorBuildSettings.scenes;
        var newScenes  = new EditorBuildSettingsScene[scenes.Length + 1];
        var newEntry   = new EditorBuildSettingsScene(scenePath, true);

        newScenes[0] = newEntry;
        for (int i = 0; i < scenes.Length; i++)
            newScenes[i + 1] = scenes[i];

        EditorBuildSettings.scenes = newScenes;
        Debug.Log($"[LoginSceneSetup] Added '{scenePath}' as build index 0.");
    }
}
#endif
