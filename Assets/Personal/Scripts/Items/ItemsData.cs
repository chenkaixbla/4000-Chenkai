using EditorAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemsData", menuName = "Game/ItemsData")]
public class ItemsData : ScriptableObject
{
    public Catalog_ItemType itemType = Catalog_ItemType.Resource;
    public int itemID;
    public string displayName;
    [TextArea(0, int.MaxValue)] public string itemDescriptions;
    public int price;
    [AssetPreview] public Sprite icon;
}
