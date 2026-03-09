using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Image_Text : MonoBehaviour
{
    public Image image;
    public TMP_Text text;

    public void Set(Sprite icon, string text)
    {
        if (image != null)
        {
            image.sprite = icon;
            image.enabled = icon != null;
        }

        if (this.text != null)
        {
            this.text.text = text ?? string.Empty;
        }
    }
}
