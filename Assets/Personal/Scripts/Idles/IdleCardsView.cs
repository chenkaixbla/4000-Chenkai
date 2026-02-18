using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class IdleCardsView : MonoBehaviour
{
    [Title("UI")]
    public Transform cardContainer;
    public IdleCard idleCardPrefab;

    // Object pooling for idle cards
    List<IdleCard> idleCardPool = new();

    public void UpdateView(JobInstance jobInstance)
    {
        ClearExistingCards();

        foreach (IdleInstance idleInstance in jobInstance.idleInstances)
        {
            IdleCard card = GetIdleCardFromPool();
            card.transform.SetParent(cardContainer, false);
            card.SetInstance(idleInstance);
        }
    }

    public IdleCard GetIdleCardFromPool()
    {
        while (idleCardPool.Count > 0)
        {
            int lastIndex = idleCardPool.Count - 1;
            IdleCard card = idleCardPool[lastIndex];
            idleCardPool.RemoveAt(lastIndex);

            if (card == null)
            {
                continue;
            }

            card.gameObject.SetActive(true);
            return card;
        }

        return Instantiate(idleCardPrefab);
    }

    void ClearExistingCards()
    {
        idleCardPool.Clear();

        foreach (Transform child in cardContainer)
        {
            child.gameObject.SetActive(false);

            if (child.TryGetComponent(out IdleCard card))
            {
                idleCardPool.Add(card);
            }
        }
    }
}
