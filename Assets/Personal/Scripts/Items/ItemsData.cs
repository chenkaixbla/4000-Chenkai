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
}
