using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemsData", menuName = "Game/ItemsData")]
public class ItemsData : ScriptableObject
{
    public Catalog_ItemType itemType = Catalog_ItemType.Resource;
    public int itemID;
    public string displayName;
    [TextArea(0, int.MaxValue)] public string itemDescriptions;
    [Min(0)] public int price = 0;
    public List<ConditionRuleEntry> purchaseRequirements = new();
    [AssetPreview] public Sprite icon;

    void OnEnable()
    {
        EnsurePurchaseRequirements();
    }

    void OnValidate()
    {
        EnsurePurchaseRequirements();
    }

    void EnsurePurchaseRequirements()
    {
        if (purchaseRequirements == null)
        {
            purchaseRequirements = new List<ConditionRuleEntry>();
        }

        for (int i = 0; i < purchaseRequirements.Count; i++)
        {
            ConditionRuleEntry entry = purchaseRequirements[i];
            if (entry == null)
            {
                entry = new ConditionRuleEntry();
                purchaseRequirements[i] = entry;
            }

            ConditionRuleUtility.EnsureConditionRuleType(entry);
        }
    }
}
