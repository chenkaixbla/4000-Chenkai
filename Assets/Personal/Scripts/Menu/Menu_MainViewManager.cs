using UnityEngine;

public class Menu_MainViewManager : MonoBehaviour
{
    [System.Serializable]
    public class Panel
    {
        public string panelName;
        public Transform panelRoot;
    }

    [SerializeField] Panel[] panels;

    public void ShowPanel(string panelName)
    {
        foreach (var panel in panels)
        {
            panel.panelRoot.gameObject.SetActive(panel.panelName == panelName);
        }
    }

    public string[] GetPanelNames()
    {
        string[] names = new string[panels.Length];
        for (int i = 0; i < panels.Length; i++)
        {
            names[i] = panels[i].panelName;
        }
        return names;
    }
}
