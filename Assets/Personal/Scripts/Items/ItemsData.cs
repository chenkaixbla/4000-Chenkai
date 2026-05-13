using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemsData", menuName = "Game/ItemsData")]
public class ItemsData : ScriptableObject
{
    [AssetPreview(previewHeight: 64)] public Sprite icon;
    
    [Title("General")]
    public Catalog_ItemType itemType = Catalog_ItemType.Resource;
    public int itemID;
    public string displayName;
    [TextArea(0, int.MaxValue)] public string itemDescriptions;

    [Title("Shop")]
    [Min(0)] public int price = 0;
    public List<ConditionRuleEntry> purchaseRequirements = new();
    

    [Title("Combat")]
    [MessageBox("Combat data is only used for Weapon, Armor, Food, Potion, and Utility items.", nameof(HidesCombatData), MessageMode.Warning)]
    [SerializeField] private Void combatDataMessageBox;
    [ShowField(nameof(UsesCombatData))]
    public CombatItemDefinition combatData = new();

    public bool UsesCombatData => itemType is Catalog_ItemType.Weapon
        or Catalog_ItemType.Armor
        or Catalog_ItemType.Food
        or Catalog_ItemType.Potion
        or Catalog_ItemType.Utility;
    public bool HidesCombatData => !UsesCombatData;

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

        combatData ??= new CombatItemDefinition();
        combatData.EnsureInitialized();
        combatData.SynchronizeWithItemType(itemType);
        combatData.EnsureInitialized();
    }
}
