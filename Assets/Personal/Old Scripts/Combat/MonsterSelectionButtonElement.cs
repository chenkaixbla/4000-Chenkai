using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MonsterSelectionButtonElement : MonoBehaviour
{
    [Title("UI References")]
    // Main button component that receives click callbacks.
    [SerializeField] Button button;

    // Optional background image used for selection highlighting.
    [SerializeField] Image background;

    // Text label that displays monster/item selection information.
    [SerializeField] TMP_Text label;

    // Optional layout element used to set runtime preferred height.
    [SerializeField] LayoutElement layoutElement;

    public Button Button => button;
    public Image Background => background;
    public TMP_Text Label => label;

    void Awake()
    {
        CacheMissingReferences();
    }

    void OnValidate()
    {
        CacheMissingReferences();
    }

    public void Configure(string value, UnityAction onClick, bool interactable, float preferredHeight)
    {
        CacheMissingReferences();
        SetLabel(value);
        SetInteractable(interactable);
        SetPreferredHeight(preferredHeight);

        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }
    }

    public void SetLabel(string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    public void SetBackgroundColor(Color color)
    {
        if (background != null)
        {
            background.color = color;
        }
    }

    public void SetLabelAlignment(TextAlignmentOptions alignment)
    {
        if (label != null)
        {
            label.alignment = alignment;
        }
    }

    public void SetLabelMargin(Vector4 margin)
    {
        if (label != null)
        {
            label.margin = margin;
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    public void SetPreferredHeight(float height)
    {
        if (layoutElement != null)
        {
            layoutElement.preferredHeight = height;
        }
    }

    void CacheMissingReferences()
    {
        button ??= GetComponent<Button>();

        if (button != null)
        {
            background ??= button.targetGraphic as Image;
        }

        background ??= GetComponent<Image>();
        label ??= GetComponentInChildren<TMP_Text>(true);
        layoutElement ??= GetComponent<LayoutElement>();
    }
}
