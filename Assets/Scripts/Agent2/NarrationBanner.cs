using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom banner that shows Agent 2 narration text when an organ appears in AR.
/// Assign to a panel GameObject in the AR scene Canvas.
/// </summary>
public class NarrationBanner : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI narrationText;
    public Button          closeButton;
    public Button          nextButton;   // loads the next narration page

    [Header("Settings")]
    [Tooltip("Seconds before the banner auto-hides. Set 0 to never auto-hide.")]
    public float autoHideSeconds = 12f;

    private Coroutine _autoHideCoroutine;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        gameObject.SetActive(false);
    }

    private void OnNextClicked()
    {
        if (NarrationManager.Instance != null)
        {
            CancelAutoHide();
            if (narrationText != null) narrationText.text = "Loading...";
            NarrationManager.Instance.RequestNextPage();
        }
    }

    /// <summary>Show a loading placeholder while waiting for the API response.</summary>
    public void ShowLoading()
    {
        CancelAutoHide();
        gameObject.SetActive(true);
        if (narrationText != null)
            narrationText.text = "Loading narration...";
    }

    /// <summary>Display the narration text and start the auto-hide timer.</summary>
    public void Show(string text)
    {
        CancelAutoHide();
        gameObject.SetActive(true);
        if (narrationText != null)
            narrationText.text = text;
        if (autoHideSeconds > 0)
            _autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
    }

    /// <summary>Immediately hide the banner (e.g. when organ leaves the camera).</summary>
    public void Hide()
    {
        CancelAutoHide();
        gameObject.SetActive(false);
    }

    private void CancelAutoHide()
    {
        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
            _autoHideCoroutine = null;
        }
    }

    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideSeconds);
        gameObject.SetActive(false);
        _autoHideCoroutine = null;
    }
}
