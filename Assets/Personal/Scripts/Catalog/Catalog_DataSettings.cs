using UnityEngine;

[CreateAssetMenu(fileName = "Catalog_DataSettings", menuName = "Game/Catalog/Data Settings")]
public class Catalog_DataSettings : ScriptableObject
{
    public string jobsDataFolder = "Assets";
    public string itemsDataFolder = "Assets";
    public string idleDataFolder = "Assets";
}
