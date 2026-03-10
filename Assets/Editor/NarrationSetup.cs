using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class NarrationSetup
{
    [MenuItem("Tools/AR Anatomy/Setup Narration Banner (Fix + Create)")]
    public static void AddNarrationBanner()
    {
        // Work on the already-open active scene — do NOT call OpenScene,
        // as that causes object references to become invalid mid-script.
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!scene.name.Contains("3D-Anatomy"))
        {
            Debug.LogError("[NarrationSetup] Please open 3D-Anatomy-Model-Scene first!");
            return;
        }

        // ── 1. Remove old Info Panel and Info button ─────────────────────────────
        // Search all transforms including inactive ones
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in allTransforms)
        {
            if (t.name == "Info Panel" || t.name == "Info")
            {
                Debug.Log($"[NarrationSetup] Deleted '{t.name}'.");
                Object.DestroyImmediate(t.gameObject);
            }
        }

        // ── 2. Remove any pre-existing NarrationBannerPanel to avoid duplicates ──
        // Use FindObjectsByType with inactive=Include since the panel starts disabled
        var existingBanners = Object.FindObjectsByType<NarrationBanner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in existingBanners)
        {
            Debug.Log($"[NarrationSetup] Removing existing NarrationBannerPanel: {b.gameObject.name}");
            Object.DestroyImmediate(b.gameObject);
        }

        // Find UICanvas specifically by name (it is active, so Find works fine)
        var canvasGO = GameObject.Find("UICanvas");
        if (canvasGO == null)
        {
            Debug.LogError("[NarrationSetup] 'UICanvas' GameObject not found! Make sure 3D-Anatomy-Model-Scene is open.");
            return;
        }
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[NarrationSetup] UICanvas has no Canvas component!");
            return;
        }

        // ── NarrationBannerPanel ─────────────────────────────────────────────
        var panelGO   = new GameObject("NarrationBannerPanel");
        panelGO.layer = LayerMask.NameToLayer("UI");
        panelGO.transform.SetParent(canvas.transform, false);

        var panelRect             = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin       = new Vector2(0, 0);
        panelRect.anchorMax       = new Vector2(1, 0);
        panelRect.pivot           = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 20);
        panelRect.sizeDelta       = new Vector2(0, 160);

        var panelImage  = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);

        // ── Header label ("AI NARRATION") ────────────────────────────────────
        var headerGO  = new GameObject("HeaderText");
        headerGO.transform.SetParent(panelGO.transform, false);

        var headerRect              = headerGO.AddComponent<RectTransform>();
        headerRect.anchorMin        = new Vector2(0, 1);
        headerRect.anchorMax        = new Vector2(1, 1);
        headerRect.pivot            = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = new Vector2(0, -8);
        headerRect.sizeDelta        = new Vector2(-24, 26);

        var headerTMP       = headerGO.AddComponent<TextMeshProUGUI>();
        headerTMP.text      = "AI NARRATION";
        headerTMP.fontSize  = 11;
        headerTMP.fontStyle = FontStyles.Bold;
        headerTMP.color     = new Color(0.4f, 0.8f, 1f);
        headerTMP.alignment = TextAlignmentOptions.TopLeft;

        // ── Narration text ───────────────────────────────────────────────────
        var textGO  = new GameObject("NarrationText");
        textGO.transform.SetParent(panelGO.transform, false);

        var textRect       = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(12, 12);
        textRect.offsetMax = new Vector2(-44, -36);

        var narrationTMP                 = textGO.AddComponent<TextMeshProUGUI>();
        narrationTMP.text               = "";
        narrationTMP.fontSize           = 14;
        narrationTMP.color              = Color.white;
        narrationTMP.alignment          = TextAlignmentOptions.TopLeft;
        narrationTMP.textWrappingMode   = TMPro.TextWrappingModes.Normal;

        // ── Close button ─────────────────────────────────────────────────────
        var closeBtnGO  = new GameObject("CloseButton");
        closeBtnGO.transform.SetParent(panelGO.transform, false);

        var closeRect              = closeBtnGO.AddComponent<RectTransform>();
        closeRect.anchorMin        = new Vector2(1, 1);
        closeRect.anchorMax        = new Vector2(1, 1);
        closeRect.pivot            = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-6, -6);
        closeRect.sizeDelta        = new Vector2(32, 32);

        var closeBtnImage   = closeBtnGO.AddComponent<Image>();
        closeBtnImage.color = new Color(1f, 1f, 1f, 0.15f);
        var closeBtn        = closeBtnGO.AddComponent<Button>();

        var closeTxtGO  = new GameObject("Text");
        closeTxtGO.transform.SetParent(closeBtnGO.transform, false);

        var closeTxtRect       = closeTxtGO.AddComponent<RectTransform>();
        closeTxtRect.anchorMin = Vector2.zero;
        closeTxtRect.anchorMax = Vector2.one;
        closeTxtRect.offsetMin = closeTxtRect.offsetMax = Vector2.zero;

        var closeTMP       = closeTxtGO.AddComponent<TextMeshProUGUI>();
        closeTMP.text      = "X";
        closeTMP.fontSize  = 14;
        closeTMP.color     = Color.white;
        closeTMP.alignment = TextAlignmentOptions.Center;

        // ── Attach NarrationBanner component ─────────────────────────────────
        var narrationBanner             = panelGO.AddComponent<NarrationBanner>();
        narrationBanner.narrationText   = narrationTMP;
        narrationBanner.closeButton     = closeBtn;
        narrationBanner.autoHideSeconds = 12f;

        // ── Wire NarrationManager (use existing if present, else create new) ──
        NarrationManager narrationManager = Object.FindFirstObjectByType<NarrationManager>(FindObjectsInactive.Include);
        if (narrationManager == null)
        {
            var managerGO = new GameObject("NarrationManager");
            narrationManager = managerGO.AddComponent<NarrationManager>();
            Debug.Log("[NarrationSetup] Created new NarrationManager.");
        }
        else
        {
            Debug.Log("[NarrationSetup] Found existing NarrationManager, updating banner reference.");
        }
        narrationManager.banner = narrationBanner;

        // Start hidden
        panelGO.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[NarrationSetup] Done! Set 'azureFunctionsBaseUrl' on the NarrationManager GameObject in the Inspector.");
    }
}
