using UnityEngine;

[DisallowMultipleComponent]
public class Menu_ViewPanel : MonoBehaviour
{
    public Menu_ViewPanelId panelId = Menu_ViewPanelId.None;
    public string legacyPanelName;

    public bool IsVisible => gameObject.activeSelf;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public bool Matches(string panelName)
    {
        if (string.IsNullOrWhiteSpace(panelName))
        {
            return false;
        }

        string cleanInput = panelName.Trim();
        if (!string.IsNullOrWhiteSpace(legacyPanelName) &&
            string.Equals(legacyPanelName.Trim(), cleanInput, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(panelId.ToString(), cleanInput, System.StringComparison.OrdinalIgnoreCase);
    }
}
