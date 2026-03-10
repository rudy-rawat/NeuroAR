using UnityEngine;
using Vuforia;
using System.Collections;
using System.Collections.Generic;

public class OrganTarget : MonoBehaviour
{
    [Header("Organ Info")]
    public string organType;
    public bool haveDetailVersion;

    [Header("Label System")]
    public OrganLabelManager labelManager;  // Assign in inspector

    private GameObject currentOrgan;
    private bool showingDetailed = false;
    private ObserverBehaviour observer;
    public float fadeDuration = 0.5f;

    // ── Agent 1 session tracking ──────────────────────────────────────
    private float  _sessionStartTime = -1f; // -1 = target not yet tracked this session
    private bool   _viewedBasic     = false;
    private bool   _viewedDetailed  = false;
    private bool   _viewedLabels    = false;
    private bool   _viewedInfo      = false;

    
    void Awake()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    void OnDestroy()
    {
        if (observer != null)
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
        {
            // Start tracking session time
            _sessionStartTime = Time.realtimeSinceStartup;
            _viewedBasic    = !showingDetailed;   // spawning basic counts as viewing it
            _viewedDetailed = showingDetailed;
            _viewedLabels   = false;
            _viewedInfo     = false;

            SpawnOrgan(showingDetailed);
            NarrationManager.Instance?.RequestNarration(organType);

            if (OrganToggleUI.Instance != null)
            {
                // Show extra buttons (labels, quiz)
                OrganToggleUI.Instance.ShowExtraButtons();

                // Show toggle button only if organ has detailed version
                if (haveDetailVersion)
                    OrganToggleUI.Instance.ShowToggleButton();
                else
                    OrganToggleUI.Instance.HideToggleButton();
            }
            else
            {
                Debug.LogWarning("OrganToggleUI Instance is null!");
            }
        }
        else
        {
            NarrationManager.Instance?.HideBanner();
            LogSessionToAgent1();
            RemoveOrganImmediate();
        }
    }

    private void SpawnOrgan(bool detailed)
    {
        // Destroy existing organ if it exists
        if (currentOrgan != null)
        {
            Destroy(currentOrgan);
            currentOrgan = null;
        }

        GameObject prefab = detailed ? OrganRegistry.Instance.GetDetailed(organType) : OrganRegistry.Instance.GetBasic(organType);

        if (prefab != null)
        {
            currentOrgan = Instantiate(prefab, transform);
            currentOrgan.tag = detailed ? "DETAILED" : "BASIC";
            StartCoroutine(FadeInModel(currentOrgan));

            // Add PinchToZoom component
            if (currentOrgan.GetComponent<PinchToZoom>() == null)
            {
                currentOrgan.AddComponent<PinchToZoom>();
            }

            // Setup labels - automatically find anchors in the spawned model
            SetupLabelsForCurrentOrgan(detailed);
        }

        showingDetailed = detailed;
    }

    private void SetupLabelsForCurrentOrgan(bool detailed)
    {
        if (labelManager == null)
        {
            Debug.LogWarning("Label manager not assigned!");
            return;
        }

        if (currentOrgan == null)
        {
            Debug.LogWarning("No current organ to setup labels for!");
            return;
        }

        // Find all label anchors automatically in the spawned model
        LabelPoint[] labelsWithAnchors = FindAnchorsInModel(null, currentOrgan);
        labelManager.SetupLabels(labelsWithAnchors);
    }

    private LabelPoint[] FindAnchorsInModel(LabelPoint[] labelDefinitions, GameObject model)
    {
        // Find all GameObjects with specific tag or naming pattern
        // For simplicity, find all children that could be label anchors
        Transform[] allChildren = model.GetComponentsInChildren<Transform>();

        List<LabelPoint> foundLabels = new List<LabelPoint>();

        foreach (Transform child in allChildren)
        {
            // Skip the root and renderer objects
            if (child == model.transform || child.GetComponent<Renderer>() != null)
                continue;

            // Check if this looks like a label anchor (empty GameObject or tagged)
            if (child.childCount == 0 || child.CompareTag("LabelAnchor"))
            {
                LabelPoint point = new LabelPoint
                {
                    anchorPoint = child,
                    labelColor = Color.white  // Default color, can be customized later
                };
                foundLabels.Add(point);
                Debug.Log($"Found label anchor: {child.name}");
            }
        }

        return foundLabels.ToArray();
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        // Check direct children first
        foreach (Transform child in parent)
        {
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        // Recursively search in children
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    public void ToggleOrgan()
    {
        showingDetailed = !showingDetailed;

        // Mark which version was viewed
        if (showingDetailed) _viewedDetailed = true;
        else                 _viewedBasic    = true;

        StartCoroutine(AnimateModelSwap(showingDetailed));
    }

    /// <summary>Called by OrganLabelManager when labels are shown.</summary>
    public void OnLabelsViewed()  => _viewedLabels = true;

    /// <summary>Called by OrganInfoUI when info panel is opened.</summary>
    public void OnInfoViewed()    => _viewedInfo   = true;

    private IEnumerator AnimateModelSwap(bool newDetailState)
    {
        GameObject old = currentOrgan;
        if (old != null)
        {
            yield return StartCoroutine(FadeOutModel(old));
        }

        if (old != null)
            Destroy(old);

        SpawnOrgan(newDetailState);
    }

    private IEnumerator FadeInModel(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        Vector3 originalScale = model.transform.localScale;
        model.transform.localScale = originalScale * 0.3f;

        foreach (var r in renderers)
            foreach (var mat in r.materials)
                mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0f);

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = t / fadeDuration;
            float scale = Mathf.Lerp(0.3f, 1f, alpha);

            foreach (var r in renderers)
                foreach (var mat in r.materials)
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, alpha);

            model.transform.localScale = originalScale * scale;
            yield return null;
        }

        model.transform.localScale = originalScale;
    }

    private IEnumerator FadeOutModel(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        Vector3 originalScale = model.transform.localScale;

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = 1f - (t / fadeDuration);
            float scale = Mathf.Lerp(1f, 0.3f, t / fadeDuration);

            foreach (var r in renderers)
                foreach (var mat in r.materials)
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, alpha);

            model.transform.localScale = originalScale * scale;
            yield return null;
        }
    }

    private void RemoveOrganImmediate()
    {
        if (currentOrgan != null)
            Destroy(currentOrgan);

        if (labelManager != null)
            labelManager.ClearLabels();
    }

    // ── Agent 1 integration ───────────────────────────────────────────

    private void LogSessionToAgent1()
    {
        // Nothing to log if agent system isn't running
        if (LearnerProfileManager.Instance == null || !LearnerProfileManager.Instance.IsReady)
            return;

        // Guard: LOST fired without a prior TRACKED (e.g. Vuforia init edge case)
        if (_sessionStartTime < 0f) return;

        // Don't log if the organ was barely shown (under 2 seconds — likely a misread)
        int timeSpent = Mathf.RoundToInt(Time.realtimeSinceStartup - _sessionStartTime);
        if (timeSpent < 2) return;

        LearnerProfileManager.Instance.LogOrganSession(
            organName:      organType,
            timeSeconds:    timeSpent,
            viewedBasic:    _viewedBasic,
            viewedDetailed: _viewedDetailed,
            viewedLabels:   _viewedLabels,
            viewedInfo:     _viewedInfo
        );

        Debug.Log($"[OrganTarget] Session logged — {organType} | {timeSpent}s | " +
                  $"basic:{_viewedBasic} detailed:{_viewedDetailed} " +
                  $"labels:{_viewedLabels} info:{_viewedInfo}");
    }
}