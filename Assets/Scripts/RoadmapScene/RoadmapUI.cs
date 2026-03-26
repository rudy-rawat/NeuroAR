using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RoadmapUI : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text overallFocusText;

    [Header("Top Buttons")]
    public Button refreshButton;

    [Header("Status Text")]
    [HideInInspector]
    public TMP_Text roadmapBodyText;

    [Header("Typography")]
    public int headingFontSize = 100;
    public int bodyFontSize = 80;
    public int sectionHeaderFontSize = 80;
    public int sectionBodyFontSize = 80;

    [Header("Body Panel")]
    public Color bodyPanelColor = new Color(0.10f, 0.14f, 0.22f, 0.95f);

    private GameObject _bodyPanel;
    private ScrollRect _scroll;
    private RectTransform _content;
    private Button _refreshButton;

    private void Start()
    {
        // Keep scene-authored UI positions/sizes unchanged (e.g., Back button).
        EnsureDropdownLayout();

        _refreshButton = ResolveRefreshButton();
        if (_refreshButton != null)
        {
            _refreshButton.onClick.RemoveListener(OnRefreshClicked);
            _refreshButton.onClick.AddListener(OnRefreshClicked);
            _refreshButton.interactable = true;
            _refreshButton.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning("RoadmapUI: Refresh button not assigned/found. Assign it in inspector or name it 'Referesh Button' under Canvas/HeaderLayer.");
        }

        if (overallFocusText != null)
        {
            overallFocusText.text = "Loading Learning Roadmap...";
            ApplyHeaderStyle();
        }

        LoadRoadmap();
    }

    public void LoadRoadmap(bool forceRefresh = false)
    {
        if (RoadmapService.Instance == null)
        {
            Debug.LogError("RoadmapService is not in the scene!");
            if (overallFocusText != null) overallFocusText.text = "Error: RoadmapService missing.";
            if (roadmapBodyText != null) roadmapBodyText.text = "Roadmap service object not found in this scene.";
            return;
        }

        string userId;
        if (!EnsureUserId(out userId))
        {
            if (overallFocusText != null) overallFocusText.text = "Unable to start session. Please login again.";
            if (roadmapBodyText != null) roadmapBodyText.text = "No user session available.";
            Debug.LogError("RoadmapUI: Could not resolve a valid UserId.");
            return;
        }

        if (overallFocusText != null)
        {
            overallFocusText.text = forceRefresh ? "Generating new roadmap..." : "Loading current roadmap...";
        }

        if (roadmapBodyText != null)
        {
            roadmapBodyText.text = "Fetching roadmap from backend...";
        }

        if (_refreshButton != null)
        {
            _refreshButton.interactable = false;
        }

        RoadmapService.Instance.GetRoadmap(userId, OnRoadmapLoaded, OnRoadmapError, forceRefresh);
    }

    private bool EnsureUserId(out string userId)
    {
        userId = string.Empty;

        if (UserSession.Instance == null)
        {
            var go = new GameObject("UserSession");
            go.AddComponent<UserSession>();
        }

        if (UserSession.Instance == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(UserSession.Instance.UserId))
        {
            UserSession.Instance.SetGuest();
        }

        userId = UserSession.Instance.UserId;
        return !string.IsNullOrEmpty(userId);
    }

    private void OnRoadmapLoaded(RoadmapResponse response)
    {
        if (response == null || response.steps == null)
        {
            if (overallFocusText != null) overallFocusText.text = "Failed to load roadmap data.";
            if (roadmapBodyText != null) roadmapBodyText.text = "Backend returned an empty roadmap payload.";
            return;
        }

        Debug.Log($"[RoadmapUI] Loaded roadmap: steps={response.steps.Count}, focusLen={(response.overallFocus ?? string.Empty).Length}");

        if (overallFocusText != null)
        {
            overallFocusText.text = "Personalized Learning Roadmap";
            ApplyHeaderStyle();
        }

        EnsureDropdownLayout();

        if (_content == null)
        {
            if (roadmapBodyText != null) roadmapBodyText.text = "UI layout error: content container missing.";
            return;
        }

        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            Destroy(_content.GetChild(i).gameObject);
        }

        if (roadmapBodyText != null)
        {
            string source = string.IsNullOrEmpty(response.source) ? "generated" : response.source;
            roadmapBodyText.text = source == "existing"
                ? "Showing your saved roadmap. Tap each section to expand details."
                : "Showing a newly generated roadmap. Tap each section to expand details.";
        }

        BuildRoadmapSections(response);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        if (_scroll != null)
        {
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;
        }

        if (_refreshButton != null)
        {
            _refreshButton.interactable = true;
        }
    }

    private void OnRoadmapError(string errorMsg)
    {
        if (overallFocusText != null) overallFocusText.text = "Error loading roadmap";
        if (roadmapBodyText != null) roadmapBodyText.text = $"Error loading roadmap:\n{errorMsg}";

        if (_refreshButton != null)
        {
            _refreshButton.interactable = true;
        }
    }

    private void ApplyHeaderStyle()
    {
        if (overallFocusText == null) return;
        overallFocusText.fontSize = headingFontSize;
        overallFocusText.fontStyle = FontStyles.Bold;
        overallFocusText.textWrappingMode = TextWrappingModes.Normal;
        overallFocusText.color = Color.white;
        overallFocusText.raycastTarget = false;
    }

    private void OnRefreshClicked()
    {
        Debug.Log("[RoadmapUI] Refresh clicked");
        LoadRoadmap(true);
    }

    private Button ResolveRefreshButton()
    {
        if (refreshButton != null) return refreshButton;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return null;

        string[] preferredPaths =
        {
            "HeaderLayer/Referesh Button",
            "HeaderLayer/Refresh Button",
            "HeaderLayer/RefreshButton",
            "Referesh Button",
            "Refresh Button",
            "RefreshButton"
        };

        for (int i = 0; i < preferredPaths.Length; i++)
        {
            Transform t = parentCanvas.transform.Find(preferredPaths[i]);
            if (t != null)
            {
                Button b = t.GetComponent<Button>();
                if (b != null) return b;
            }
        }

        Button[] allButtons = parentCanvas.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < allButtons.Length; i++)
        {
            string n = allButtons[i].name.ToLowerInvariant();
            if (n.Contains("refresh") || n.Contains("referesh"))
            {
                return allButtons[i];
            }
        }

        return null;
    }

    private void EnsureDropdownLayout()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        Transform oldScroll = parentCanvas.transform.Find("ScrollView");
        if (oldScroll != null)
        {
            oldScroll.gameObject.SetActive(false);
        }

        if (_bodyPanel != null && _content != null && _scroll != null)
        {
            return;
        }

        GameObject panel = parentCanvas.transform.Find("RoadmapBodyPanel")?.gameObject;
        if (panel == null)
        {
            panel = new GameObject("RoadmapBodyPanel");
            panel.transform.SetParent(parentCanvas.transform, false);
        }
        _bodyPanel = panel;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.06f, 0.08f);
        panelRect.anchorMax = new Vector2(0.94f, 0.82f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.color = bodyPanelColor;

        GameObject statusGo = panel.transform.Find("RoadmapStatusText")?.gameObject;
        if (statusGo == null)
        {
            statusGo = new GameObject("RoadmapStatusText");
            statusGo.transform.SetParent(panel.transform, false);
            statusGo.AddComponent<RectTransform>();
        }

        RectTransform statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0.90f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = new Vector2(20f, 0f);
        statusRect.offsetMax = new Vector2(-20f, -8f);

        roadmapBodyText = statusGo.GetComponent<TextMeshProUGUI>();
        if (roadmapBodyText == null) roadmapBodyText = statusGo.AddComponent<TextMeshProUGUI>();
        roadmapBodyText.fontSize = bodyFontSize - 1;
        roadmapBodyText.textWrappingMode = TextWrappingModes.Normal;
        roadmapBodyText.alignment = TextAlignmentOptions.MidlineLeft;
        roadmapBodyText.color = new Color(0.90f, 0.95f, 1f);
        roadmapBodyText.raycastTarget = false;

        GameObject scrollGo = panel.transform.Find("RoadmapAccordionScroll")?.gameObject;
        if (scrollGo == null)
        {
            scrollGo = new GameObject("RoadmapAccordionScroll");
            scrollGo.transform.SetParent(panel.transform, false);
            scrollGo.AddComponent<RectTransform>();
        }

        RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 0.90f);
        scrollRect.offsetMin = new Vector2(20f, 20f);
        scrollRect.offsetMax = new Vector2(-20f, -8f);

        Image scrollImage = scrollGo.GetComponent<Image>();
        if (scrollImage == null) scrollImage = scrollGo.AddComponent<Image>();
        scrollImage.color = new Color(0.05f, 0.09f, 0.15f, 0.55f);

        _scroll = scrollGo.GetComponent<ScrollRect>();
        if (_scroll == null) _scroll = scrollGo.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;

        GameObject viewportGo = scrollGo.transform.Find("Viewport")?.gameObject;
        if (viewportGo == null)
        {
            viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            viewportGo.AddComponent<RectTransform>();
        }

        RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewportGo.GetComponent<Image>();
        if (viewportImage == null) viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

        Mask mask = viewportGo.GetComponent<Mask>();
        if (mask == null) mask = viewportGo.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentGo = viewportGo.transform.Find("Content")?.gameObject;
        if (contentGo == null)
        {
            contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            contentGo.AddComponent<RectTransform>();
        }

        _content = contentGo.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup rootLayout = contentGo.GetComponent<VerticalLayoutGroup>();
        if (rootLayout == null) rootLayout = contentGo.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(8, 8, 8, 10);
        rootLayout.spacing = 26;
        rootLayout.childControlHeight = true;
        rootLayout.childControlWidth = true;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scroll.viewport = viewportRect;
        _scroll.content = _content;
    }

    private void BuildRoadmapSections(RoadmapResponse response)
    {
        string roadmapMeta =
            $"Overall Focus: {response.overallFocus}\n" +
            (!string.IsNullOrEmpty(response.learningNarrative) ? $"Narrative: {response.learningNarrative}\n" : "") +
            (!string.IsNullOrEmpty(response.studyAdvice) ? $"Study Advice: {response.studyAdvice}\n" : "");

        CreateAccordionSection(_content, "Roadmap Overview", roadmapMeta, true, null);

        if (response.steps == null) return;

        foreach (var step in response.steps)
        {
            string stepBody =
                $"Topic: {step.topic}\n" +
                $"Description: {step.description}\n" +
                $"Estimated Time: {step.estimatedTimeInMinutes} minutes\n";

            Transform stepContent = CreateAccordionSection(
                _content,
                $"Step {step.stepNumber}: {step.topic}",
                stepBody,
                false,
                null);

            if (step.resources == null || step.resources.Count == 0)
            {
                CreateBodyText(stepContent, "No resources for this step.", sectionBodyFontSize - 2, new Color(0.75f, 0.84f, 0.96f));
                continue;
            }

            foreach (var resource in step.resources)
            {
                string details =
                    $"Type: {resource.type}\n" +
                    (!string.IsNullOrEmpty(resource.referenceId) ? $"Reference ID: {resource.referenceId}\n" : "") +
                    (!string.IsNullOrEmpty(resource.url) ? $"URL: {resource.url}\n" : "");

                CreateAccordionSection(
                    stepContent,
                    $"Resource: {resource.name}",
                    details,
                    false,
                    () => OnResourceClicked(resource));
            }
        }
    }

    private Transform CreateAccordionSection(Transform parent, string headerText, string bodyText, bool expandedByDefault, UnityEngine.Events.UnityAction onOpenResource)
    {
        GameObject card = new GameObject(SafeName("Card_" + headerText));
        card.transform.SetParent(parent, false);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.19f, 0.28f, 0.92f);

        VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(12, 12, 10, 10);
        cardLayout.spacing = 10;
        cardLayout.childControlHeight = true;
        cardLayout.childControlWidth = true;
        cardLayout.childForceExpandHeight = false;
        cardLayout.childForceExpandWidth = true;

        LayoutElement cardLE = card.AddComponent<LayoutElement>();
        cardLE.minHeight = 96;

        GameObject headerBtnGo = new GameObject("HeaderButton");
        headerBtnGo.transform.SetParent(card.transform, false);
        Image headerBg = headerBtnGo.AddComponent<Image>();
        headerBg.color = new Color(0.19f, 0.28f, 0.42f, 1f);
        Button headerBtn = headerBtnGo.AddComponent<Button>();
        headerBtn.targetGraphic = headerBg;

        LayoutElement headerLE = headerBtnGo.AddComponent<LayoutElement>();
        headerLE.minHeight = 88;

        GameObject headerLabelGo = new GameObject("HeaderLabel");
        headerLabelGo.transform.SetParent(headerBtnGo.transform, false);
        RectTransform headerRect = headerLabelGo.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = new Vector2(20f, 10f);
        headerRect.offsetMax = new Vector2(-56f, -10f);
        TMP_Text headerLabel = headerLabelGo.AddComponent<TextMeshProUGUI>();
        headerLabel.text = headerText;
        headerLabel.fontSize = sectionHeaderFontSize;
        headerLabel.color = Color.white;
        headerLabel.alignment = TextAlignmentOptions.Left;
        headerLabel.textWrappingMode = TextWrappingModes.Normal;

        GameObject arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(headerBtnGo.transform, false);
        RectTransform arrowRect = arrowGo.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 1f);
        arrowRect.offsetMin = new Vector2(-48f, 0f);
        arrowRect.offsetMax = new Vector2(-10f, 0f);
        TMP_Text arrow = arrowGo.AddComponent<TextMeshProUGUI>();
        arrow.fontSize = sectionHeaderFontSize;
        arrow.alignment = TextAlignmentOptions.Center;
        arrow.color = new Color(0.90f, 0.96f, 1f);

        GameObject bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(card.transform, false);
        Image bodyBg = bodyGo.AddComponent<Image>();
        bodyBg.color = new Color(0.08f, 0.12f, 0.19f, 0.82f);

        VerticalLayoutGroup bodyLayout = bodyGo.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(18, 18, 16, 16);
        bodyLayout.spacing = 12;
        bodyLayout.childControlHeight = true;
        bodyLayout.childControlWidth = true;
        bodyLayout.childForceExpandHeight = false;
        bodyLayout.childForceExpandWidth = true;

        CreateBodyText(bodyGo.transform, bodyText, sectionBodyFontSize, new Color(0.87f, 0.93f, 1f));

        if (onOpenResource != null)
        {
            GameObject openBtnGo = new GameObject("OpenResourceButton");
            openBtnGo.transform.SetParent(bodyGo.transform, false);
            Image openBg = openBtnGo.AddComponent<Image>();
            openBg.color = new Color(0.18f, 0.48f, 0.74f, 1f);
            Button openBtn = openBtnGo.AddComponent<Button>();
            openBtn.targetGraphic = openBg;
            openBtn.onClick.AddListener(onOpenResource);

            LayoutElement openLE = openBtnGo.AddComponent<LayoutElement>();
            openLE.minHeight = 70;
            openLE.preferredHeight = 76;

            CreateButtonLabel(openBtnGo.transform, "Open Resource", Mathf.Max(20, sectionBodyFontSize - 4), Color.white);
        }

        bodyGo.SetActive(expandedByDefault);
        arrow.text = expandedByDefault ? "v" : ">";

        headerBtn.onClick.AddListener(() =>
        {
            bool next = !bodyGo.activeSelf;
            bodyGo.SetActive(next);
            arrow.text = next ? "v" : ">";
            Canvas.ForceUpdateCanvases();
            if (_content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            }
        });

        return bodyGo.transform;
    }

    private void CreateBodyText(Transform parent, string text, int size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        GameObject labelGo = new GameObject("BodyText");
        labelGo.transform.SetParent(parent, false);

        TMP_Text label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.alignment = align;

        LayoutElement le = labelGo.AddComponent<LayoutElement>();
        le.minHeight = 50;
        le.preferredHeight = -1;
    }

    private void CreateButtonLabel(Transform parent, string text, int size, Color color)
    {
        GameObject labelGo = new GameObject("ButtonText");
        labelGo.transform.SetParent(parent, false);

        RectTransform rect = labelGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 6f);
        rect.offsetMax = new Vector2(-8f, -6f);

        TMP_Text label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void OnResourceClicked(RoadmapResource resource)
    {
        if (resource == null) return;

        if (resource.type == "external_link" && !string.IsNullOrEmpty(resource.url))
        {
            Application.OpenURL(resource.url);
        }
        else if (resource.type == "organ_scene" && !string.IsNullOrEmpty(resource.referenceId))
        {
            PlayerPrefs.SetString("SelectedOrgan", resource.referenceId);
            SceneManager.LoadScene("3D-Anatomy-Model-Scene");
        }
    }

    private string SafeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Section";
        return name.Replace('/', '_').Replace(':', '_').Replace(' ', '_');
    }
}
