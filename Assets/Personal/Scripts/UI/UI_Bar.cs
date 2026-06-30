using System;
using System.Collections.Generic;
using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One saved bar color. Each entry gets a "SET" button (via a custom drawer) that applies its
/// color to the bar's fill image. The optional label just names the swatch in the inspector.
/// </summary>
[Serializable]
public class UI_Bar_ColorPreset
{
    [Tooltip("Optional name for this swatch (e.g. \"Full\", \"Low\").")]
    public string label;
    public Color color = Color.white;
}

/// <summary>
/// A simple fill bar driven by a Filled <see cref="Image"/>'s fillAmount. The bar represents
/// a 0-1 fill mapped onto a <see cref="maxValue"/>, and can optionally show a "current/max" label.
/// The <see cref="fill"/> slider stays in sync with the image in both edit and play mode.
/// (Set the Image's Image Type to Filled, Fill Method Horizontal.)
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class UI_Bar : MonoBehaviour
{
    [Required]
    [Tooltip("The Filled Image whose fillAmount represents the current value.")]
    public Image fillImage;

    [Title("Value")]
    [Tooltip("The value the full bar represents.")]
    public float maxValue = 100f;

    [Range(0f, 1f)]
    [Tooltip("Normalized fill (0-1). Syncs with the image fill live in edit and play mode.")]
    public float fill = 1f;

    [Title("Text (all optional)")]
    [Tooltip("Title label for this bar (e.g. \"Level: 5\"). Not driven by UI_Bar - left for other scripts to fill.")]
    public TMP_Text titleText;

    [Tooltip("Text showing the current/max value. Driven by UI_Bar.")]
    public TMP_Text valueText;

    [Tooltip("If on, the value text shows \"current/max\". If off, it is cleared.")]
    public bool showValue = true;

    [Title("Coloring")]
    [InlineButton(nameof(ApplyColor), "SET", 44f)]
    [Tooltip("A working color. Click SET to apply it to the bar's fill image.")]
    public Color color = Color.white;

    [Tooltip("Saved color swatches. Each row has its own SET button to apply it to the bar.")]
    public List<UI_Bar_ColorPreset> colorPresets = new();

    /// <summary>The current value, derived from <see cref="fill"/> and <see cref="maxValue"/>.</summary>
    public float CurrentValue => fill * maxValue;

    void OnEnable()
    {
        Apply();
    }

    /// <summary>Sets the bar from a raw value (clamped to 0-<see cref="maxValue"/>).</summary>
    public void SetValue(float value)
    {
        fill = maxValue > 0f ? Mathf.Clamp01(value / maxValue) : 0f;
        Apply();
    }

    /// <summary>Sets the bar from a normalized 0-1 fill.</summary>
    public void SetFill(float normalized)
    {
        fill = Mathf.Clamp01(normalized);
        Apply();
    }

    /// <summary>Sets the fill image's color (no-op if there's no fill image).</summary>
    public void SetColor(Color value)
    {
        if (fillImage != null)
            fillImage.color = value;
    }

    // Backs the inline SET button on the color field.
    void ApplyColor() => SetColor(color);

    /// <summary>Pushes the current <see cref="fill"/> onto the image and label.</summary>
    public void Apply()
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(fill);

        if (valueText != null)
            valueText.text = showValue ? $"{Mathf.RoundToInt(CurrentValue)}/{Mathf.RoundToInt(maxValue)}" : string.Empty;
    }

    void Reset()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>() ?? GetComponentInChildren<Image>(true);

        Apply();
    }

    // Called by Unity whenever a serialized field is edited in the inspector
    // (including dragging the fill slider), in both edit and play mode.
    void OnValidate()
    {
        Apply();
    }
}
