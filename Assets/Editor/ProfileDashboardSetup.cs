#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility — adds the Profile Dashboard panel to Start-Scene.
/// Run via:  Tools → AR Anatomy → Add Profile Dashboard to Start-Scene
/// </summary>
public static class ProfileDashboardSetup
{
    [MenuItem("Tools/AR Anatomy/Add Profile Dashboard to Start-Scene")]
    public static void AddDashboard()
    {
        // ── Open Start-Scene ─────────────────────────────────────────
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Start-Scene.unity", OpenSceneMode.Single);

        // Find the existing canvas
        var canvasGo = GameObject.Find("StartSceneCanvas");
        if (canvasGo == null)
        {
            Debug.LogError("[ProfileDashboardSetup] 'StartSceneCanvas' not found in Start-Scene!");
            return;
        }
        var canvasRect = canvasGo.GetComponent<RectTransform>();

        // Guard against duplicates
        if (canvasGo.transform.Find("ProfileDashboardPanel") != null)
        {
            Debug.Log("[ProfileDashboardSetup] Dashboard already present. Skipping.");
            return;
        }

        // ── HUD: small profile avatar button (top-right corner) ───────
        var hudAvatarGo = CreateImg(canvasGo.transform, "HudAvatarButton",
            new Vector2(1f, 1f), new Vector2(-70, -70), new Vector2(100, 100),
            new Color(0.3f, 0.5f, 1f));

        // Circle mask
        var hudMask = hudAvatarGo.AddComponent<Mask>();
        hudMask.showMaskGraphic = true;

        // RawImage inside for the actual photo
        var hudRawGo  = CreateChild(hudAvatarGo.transform, "HudPhoto");
        var hudRawImg = hudRawGo.AddComponent<RawImage>();
        hudRawImg.color = Color.white;
        Stretch(hudRawGo.GetComponent<RectTransform>());

        // Button on top
        var hudBtn = hudAvatarGo.AddComponent<Button>();
        hudBtn.targetGraphic = hudAvatarGo.GetComponent<Image>();

        // ── Dashboard Panel (slides in from right) ────────────────────
        var panelGo   = CreateImg(canvasGo.transform, "ProfileDashboardPanel",
            new Vector2(1f, 0f), new Vector2(0, 0), new Vector2(600, 1920),
            new Color(0.07f, 0.07f, 0.12f, 0.97f));
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.pivot = new Vector2(1f, 0f);
        panelGo.SetActive(false);

        // ── Header bar ────────────────────────────────────────────────
        var headerGo  = CreateImg(panelGo.transform, "Header",
            new Vector2(0f, 1f), new Vector2(0, 0), new Vector2(600, 160),
            new Color(0.1f, 0.1f, 0.18f));
        var headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.pivot        = new Vector2(0.5f, 1f);
        headerRect.anchorMin    = new Vector2(0, 1);
        headerRect.anchorMax    = new Vector2(1, 1);
        headerRect.sizeDelta    = new Vector2(0, 160);
        headerRect.anchoredPosition = Vector2.zero;

        var headerLabel = CreateTMP(headerGo.transform, "HeaderLabel",
            "My Profile", 40, Color.white, FontStyles.Bold,
            new Vector2(0.1f, 0f), new Vector2(0.85f, 1f));

        // Close button (×)
        var closeBtnGo = CreateImg(headerGo.transform, "CloseButton",
            new Vector2(1f, 0.5f), new Vector2(-30, 0), new Vector2(60, 60),
            new Color(0.3f, 0.3f, 0.4f));
        var closeBtn   = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnGo.GetComponent<Image>();
        CreateTMP(closeBtnGo.transform, "X", "✕", 36, Color.white, FontStyles.Normal,
            Vector2.zero, Vector2.one);

        // ── Large avatar (centre of panel) ───────────────────────────
        var avatarContainer = CreateImg(panelGo.transform, "AvatarContainer",
            new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(160, 160),
            new Color(0.2f, 0.3f, 0.6f));
        var avaRect = avatarContainer.GetComponent<RectTransform>();
        avaRect.anchorMin = new Vector2(0.5f, 1f);
        avaRect.anchorMax = new Vector2(0.5f, 1f);
        avaRect.pivot     = new Vector2(0.5f, 0.5f);
        avaRect.anchoredPosition = new Vector2(0, -220);
        avaRect.sizeDelta        = new Vector2(160, 160);
        avatarContainer.AddComponent<Mask>().showMaskGraphic = true;
        var avatarRaw = CreateChild(avatarContainer.transform, "AvatarPhoto");
        avatarRaw.AddComponent<RawImage>().color = Color.white;
        Stretch(avatarRaw.GetComponent<RectTransform>());

        // ── Name ──────────────────────────────────────────────────────
        CreateCentredTMP(panelGo.transform, "NameText",
            "Student Name", 44, Color.white, FontStyles.Bold,
            new Vector2(0, -340), new Vector2(520, 70));

        // ── Email ─────────────────────────────────────────────────────
        CreateCentredTMP(panelGo.transform, "EmailText",
            "email@example.com", 28, new Color(0.6f, 0.7f, 0.9f), FontStyles.Normal,
            new Vector2(0, -420), new Vector2(520, 50));

        // ── UID row ───────────────────────────────────────────────────
        CreateCentredTMP(panelGo.transform, "UserIdText",
            "ID: …", 24, new Color(0.4f, 0.5f, 0.6f), FontStyles.Normal,
            new Vector2(0, -490), new Vector2(520, 40));

        // ── Guest badge ───────────────────────────────────────────────
        var guestBadgeGo  = CreateImg(panelGo.transform, "GuestBadge",
            new Vector2(0.5f, 1f), new Vector2(0, -550), new Vector2(200, 50),
            new Color(0.6f, 0.4f, 0f));
        var guestBadgeRect = guestBadgeGo.GetComponent<RectTransform>();
        guestBadgeRect.anchorMin = new Vector2(0.5f, 1f);
        guestBadgeRect.anchorMax = new Vector2(0.5f, 1f);
        guestBadgeRect.pivot     = new Vector2(0.5f, 0.5f);
        guestBadgeRect.anchoredPosition = new Vector2(0, -550);
        guestBadgeRect.sizeDelta        = new Vector2(200, 50);
        CreateTMP(guestBadgeGo.transform, "GuestLabel", "👤 Guest", 28,
            Color.white, FontStyles.Normal, Vector2.zero, Vector2.one);
        guestBadgeGo.SetActive(false);

        // ── Google badge ──────────────────────────────────────────────
        var googleBadgeGo  = CreateImg(panelGo.transform, "GoogleBadge",
            new Vector2(0.5f, 1f), new Vector2(0, -550), new Vector2(260, 50),
            new Color(0.1f, 0.5f, 0.2f));
        var googleBadgeRect = googleBadgeGo.GetComponent<RectTransform>();
        googleBadgeRect.anchorMin = new Vector2(0.5f, 1f);
        googleBadgeRect.anchorMax = new Vector2(0.5f, 1f);
        googleBadgeRect.pivot     = new Vector2(0.5f, 0.5f);
        googleBadgeRect.anchoredPosition = new Vector2(0, -550);
        googleBadgeRect.sizeDelta        = new Vector2(260, 50);
        CreateTMP(googleBadgeGo.transform, "GoogleLabel", "✔ Google Account", 26,
            Color.white, FontStyles.Normal, Vector2.zero, Vector2.one);

        // ── Divider ───────────────────────────────────────────────────
        var divGo = CreateImg(panelGo.transform, "Divider",
            new Vector2(0.5f, 1f), new Vector2(0, -620), new Vector2(500, 2),
            new Color(0.2f, 0.2f, 0.3f));
        var divRect = divGo.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.5f, 1f);
        divRect.anchorMax = new Vector2(0.5f, 1f);
        divRect.pivot     = new Vector2(0.5f, 0.5f);
        divRect.anchoredPosition = new Vector2(0, -620);
        divRect.sizeDelta        = new Vector2(500, 2);

        // ── Info section title ────────────────────────────────────────
        CreateCentredTMP(panelGo.transform, "AgentInfoTitle",
            "Learning Profile", 34, new Color(0.5f, 0.7f, 1f), FontStyles.Bold,
            new Vector2(0, -680), new Vector2(520, 50));

        // ── Placeholder stats (Agent 1 will populate these) ──────────
        CreateCentredTMP(panelGo.transform, "OrgansStudiedLabel",
            "Organs studied: —", 28, new Color(0.75f, 0.75f, 0.85f), FontStyles.Normal,
            new Vector2(0, -740), new Vector2(520, 45));

        CreateCentredTMP(panelGo.transform, "QuizScoreLabel",
            "Quiz score: —", 28, new Color(0.75f, 0.75f, 0.85f), FontStyles.Normal,
            new Vector2(0, -800), new Vector2(520, 45));

        CreateCentredTMP(panelGo.transform, "WeakConceptsLabel",
            "Weak concepts: —", 28, new Color(0.75f, 0.75f, 0.85f), FontStyles.Normal,
            new Vector2(0, -860), new Vector2(520, 45));

        // ── Sign-Out button ───────────────────────────────────────────
        var signOutGo  = CreateImg(panelGo.transform, "SignOutButton",
            new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(420, 90),
            new Color(0.7f, 0.15f, 0.15f));
        var signOutRect = signOutGo.GetComponent<RectTransform>();
        signOutRect.anchorMin = new Vector2(0.5f, 0f);
        signOutRect.anchorMax = new Vector2(0.5f, 0f);
        signOutRect.pivot     = new Vector2(0.5f, 0f);
        signOutRect.anchoredPosition = new Vector2(0, 80);
        signOutRect.sizeDelta        = new Vector2(420, 90);
        var signOutBtn = signOutGo.AddComponent<Button>();
        signOutBtn.targetGraphic = signOutGo.GetComponent<Image>();
        CreateTMP(signOutGo.transform, "Label", "Sign Out", 36,
            Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);

        // ── Attach ProfileDashboardUI component ───────────────────────
        var dashComp = canvasGo.AddComponent<ProfileDashboardUI>();
        dashComp.dashboardPanel = panelGo;
        dashComp.profilePhoto   = avatarRaw.GetComponent<RawImage>();
        dashComp.nameText       = GameObject.Find("StartSceneCanvas/ProfileDashboardPanel/NameText")?.GetComponent<TextMeshProUGUI>();
        dashComp.emailText      = GameObject.Find("StartSceneCanvas/ProfileDashboardPanel/EmailText")?.GetComponent<TextMeshProUGUI>();
        dashComp.userIdText     = GameObject.Find("StartSceneCanvas/ProfileDashboardPanel/UserIdText")?.GetComponent<TextMeshProUGUI>();
        dashComp.guestBadge     = guestBadgeGo;
        dashComp.googleBadge    = googleBadgeGo;
        dashComp.profileButton  = hudBtn;
        dashComp.signOutButton  = signOutBtn;
        dashComp.closeButton    = closeBtn;
        dashComp.hudAvatarImage = hudRawImg;
        dashComp.loginSceneName = "Login-Scene";

        // ── Save ──────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ProfileDashboardSetup] Dashboard added and Start-Scene saved.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    static GameObject CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject CreateImg(Transform parent, string name,
        Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go   = CreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta        = size;
        var img  = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        float size, Color color, FontStyles style, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go   = CreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var tmp  = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static TextMeshProUGUI CreateCentredTMP(Transform parent, string name, string text,
        float size, Color color, FontStyles style, Vector2 pos, Vector2 sizeDelta)
    {
        var go   = CreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta        = sizeDelta;
        var tmp  = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
#endif
