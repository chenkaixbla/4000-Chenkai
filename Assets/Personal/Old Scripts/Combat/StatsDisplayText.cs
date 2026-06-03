using TMPro;
using UnityEngine;

public class StatsDisplayText : MonoBehaviour
{
    // Label text (always normalized to end with a colon).
    [SerializeField] TMP_Text label;

    // Value text displayed next to/under the label.
    [SerializeField] TMP_Text value;

    public TMP_Text Label => label;
    public TMP_Text Value => value;

    public void Set(string labelText, string valueText)
    {
        if (label != null)
        {
            label.text = NormalizeLabel(labelText);
        }

        if (value != null)
        {
            value.text = valueText ?? string.Empty;
        }
    }

    static string NormalizeLabel(string raw)
    {
        string value = string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.EndsWith(":") ? value : value + ":";
    }
}
