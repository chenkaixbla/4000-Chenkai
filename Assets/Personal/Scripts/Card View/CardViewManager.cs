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
    [ReadOnly] public int currentViewIndex = -1;
    public List<ScrollViewData> scrollViews = new();

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

    public void ShowScrollView(int index, bool hideOthers = true)
    {
        currentViewIndex = index;

        if(hideOthers)
        {
            for (int i = 0; i < scrollViews.Count; i++)
            {
                scrollViews[i].scrollView.SetActive(i == index);
            }
        }
        else
        {
            ScrollViewData data = GetScrollViewData(index);
            if (data != null)
            {
                data.scrollView.SetActive(true);
            }
        }
    }

    public void HideScrollView(int index)
    {
        ScrollViewData data = GetScrollViewData(index);
        if (data != null)
        {
            data.scrollView.SetActive(false);
        }
    }
}
