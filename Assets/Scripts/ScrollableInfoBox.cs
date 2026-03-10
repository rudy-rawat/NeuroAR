using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScrollableInfoBox : MonoBehaviour
{
    public TMP_Text infoText;
    public ScrollRect scrollRect;

    public void SetText(string newText)
    {
        infoText.text = newText;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoText.rectTransform);
        scrollRect.verticalNormalizedPosition = 1f; // Scroll to top
    }
}
