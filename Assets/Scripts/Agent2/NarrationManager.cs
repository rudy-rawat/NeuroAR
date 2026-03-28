using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-level singleton for Agent 2 (Knowledge Agent).
/// Called by OrganTarget when an organ is TRACKED; requests personalised narration
/// from Azure Functions and passes the result to NarrationBanner.
/// </summary>
public class NarrationManager : MonoBehaviour
{
    public static NarrationManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Must match the Azure Function App URL set on LearnerProfileManager.")]
    public string azureFunctionsBaseUrl = "https://neuroar-apb0bnbwgvaqf2b4.centralindia-01.azurewebsites.net/";

    [Header("References")]
    public NarrationBanner banner;

    private NarrationService _service;
    private string _currentOrgan;
    private string _cachedNarrationText;   // cached text for re-appearing organs
    private int    _pageIndex = 0;
    private const int MAX_PAGES = 5;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _service = gameObject.AddComponent<NarrationService>();
            _service.Init(azureFunctionsBaseUrl);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by OrganTarget when an organ is TRACKED.
    /// Builds context from Agent 1, then requests personalised narration from Agent 2.
    /// </summary>
    public void RequestNarration(string organType)
    {
        // New organ — reset state and fetch fresh narration
        if (_currentOrgan != organType)
        {
            _currentOrgan = organType;
            _pageIndex = 0;
            _cachedNarrationText = null;
        }

        // Same organ re-appeared — show cached text instead of re-fetching
        if (!string.IsNullOrEmpty(_cachedNarrationText))
        {
            banner?.Show(_cachedNarrationText);
            return;
        }

        RequestPage(organType, _pageIndex);
    }

    /// <summary>Called by the Next button on NarrationBanner to load the next page.</summary>
    public void RequestNextPage()
    {
        if (string.IsNullOrEmpty(_currentOrgan)) return;
        _pageIndex = (_pageIndex + 1) % MAX_PAGES;
        _cachedNarrationText = null;   // force fresh fetch for the new page
        RequestPage(_currentOrgan, _pageIndex);
    }
    private void RequestPage(string organType, int page)
    {
        if (banner == null)
        {
            Debug.LogWarning("[NarrationManager] NarrationBanner not assigned in Inspector.");
            return;
        }

        banner.ShowLoading();

        // Pull context from Agent 1 (falls back to safe defaults if not ready)
        string       level        = "beginner";
        List<string> weakConcepts = new List<string>();
        int          sessionCount = 1;

        if (LearnerProfileManager.Instance != null && LearnerProfileManager.Instance.IsReady)
        {
            level        = LearnerProfileManager.Instance.GetLearningLevel();
            weakConcepts = LearnerProfileManager.Instance.GetWeakConcepts();

            var history = LearnerProfileManager.Instance.Profile?.organHistory;
            if (history != null && history.ContainsKey(organType))
                sessionCount = history[organType].sessionCount;
        }

        var req = new NarrationService.NarrationRequest
        {
            userId       = UserSession.Instance != null ? UserSession.Instance.UserId : "guest",
            organName    = organType,
            level        = level,
            weakConcepts = weakConcepts.ToArray(),
            sessionCount = sessionCount,
            pageIndex    = page
        };

        StartCoroutine(_service.RequestNarration(req,
            response =>
            {
                if (!string.IsNullOrEmpty(response?.narrationText))
                {
                    _cachedNarrationText = response.narrationText;
                    banner.Show(response.narrationText);
                }
                else
                    banner.Hide();
            },
            err =>
            {
                Debug.LogWarning($"[NarrationManager] Narration request failed: {err}");
                banner.Hide();
            }
        ));
    }

    /// <summary>Hide the banner immediately (called by OrganTarget on LOST).</summary>
    public void HideBanner() => banner?.Hide();
}
