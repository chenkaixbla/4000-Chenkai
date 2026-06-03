using System;
using UnityEngine;

[Serializable]
public class ItemSlotSpawnReferences
{
    // Parent transform where runtime item slot elements are spawned.
    [SerializeField] RectTransform content;

    // ItemSlot prefab instantiated for each candidate/action row.
    [SerializeField] ItemSlot itemSlotPrefab;

    public RectTransform Content => content;
    public ItemSlot ItemSlotPrefab => itemSlotPrefab;
}
