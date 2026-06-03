using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A thin element script that bundles the common parts of a button so managers can
/// fill them in code: the <see cref="Button"/> itself, its text label, and an image.
/// Put this on a button prefab, point the three fields at the right child objects
/// (the [Button] Auto Assign grabs sensible defaults), and let managers drive it.
/// </summary>
[DisallowMultipleComponent]
public class UI_Button : MonoBehaviour
{
    [Required]
    [Tooltip("The clickable Button. Wire its onClick, or read it from a manager.")]
    public Button button;

    [Tooltip("Text shown on the button (e.g. a job name). Optional.")]
    public TMP_Text label;

    [Tooltip("Image shown on the button (e.g. a job icon). Optional.")]
    public Image image;

    /// <summary>Sets the label text (no-op if no label is assigned).</summary>
    public void SetText(string text)
    {
        if (label != null)
            label.text = text;
    }

    /// <summary>Sets the image sprite (no-op if no image is assigned).</summary>
    public void SetImage(Sprite sprite)
    {
        if (image != null)
            image.sprite = sprite;
    }

    void Reset()
    {
        AutoAssign();
    }

    [Button]
    [Tooltip("Fills the three references from this object and its children.")]
    void AutoAssign()
    {
        if (button == null)
            button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        if (image == null)
            image = button != null ? button.targetGraphic as Image : GetComponentInChildren<Image>(true);
    }
}
