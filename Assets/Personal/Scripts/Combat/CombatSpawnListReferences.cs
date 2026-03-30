using System;
using UnityEngine;

[Serializable]
public class CombatSpawnListReferences
{
    // Parent transform where runtime button elements are spawned for this list.
    [SerializeField] RectTransform content;

    // Prefab instantiated for each runtime entry in this list.
    [SerializeField] MonsterSelectionButtonElement elementPrefab;

    public RectTransform Content => content;
    public MonsterSelectionButtonElement ElementPrefab => elementPrefab;
}