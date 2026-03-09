using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ScrollViewData
{
    public GameObject scrollView;
    public Transform container;
    public GameObject prefab;
}

public class CardViewManager : MonoBehaviour
{
    public int defaultViewIndex = 0;
    public List<ScrollViewData> scrollViews = new();

    public ScrollViewData currentView { get; private set; }

    void Start()
    {
        ShowScrollView(defaultViewIndex);
    }

    public ScrollViewData GetScrollViewData(int index)
    {
        if (index >= 0 && index < scrollViews.Count)
        {
            return scrollViews[index];
        }
        else
        {
            return null;
        }
    }

    public void ShowScrollView(int index)
    {
        for (int i = 0; i < scrollViews.Count; i++)
        {
            scrollViews[i].scrollView.SetActive(i == index);
        }

        if (index >= 0 && index < scrollViews.Count)
        {
            currentView = scrollViews[index];
        }
    }
}
