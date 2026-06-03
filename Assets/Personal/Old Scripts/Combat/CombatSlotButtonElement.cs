using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatSlotButtonElement : MonoBehaviour
{
    [Title("Slot Binding")]
    // The combat slot this static button represents in the loadout panel.
    [SerializeField] CombatEquipSlot slot = CombatEquipSlot.None;

    // Utility slot index used only when slot == Utility. Keep -1 for non-utility slots.
    [SerializeField] int utilityIndex = -1;

    [Title("UI References")]
    // The clickable button component for this slot.
    [SerializeField] Button button;

    // Optional background used by the controller to draw selected/unselected state colors.
    [SerializeField] Image background;

    // Optional label used by the controller to render slot + equipped item text.
    [SerializeField] TMP_Text label;

    public CombatEquipSlot Slot => slot;
    public int UtilityIndex => utilityIndex;
    public Button Button => button;
    public Image Background => background;
    public TMP_Text Label => label;

    void Awake()
    {
        CacheMissingReferences();
    }

    void OnValidate()
    {
        NormalizeUtilityIndex();
        CacheMissingReferences();
    }

    void NormalizeUtilityIndex()
    {
        if (slot == CombatEquipSlot.Utility)
        {
            utilityIndex = Mathf.Max(0, utilityIndex);
            return;
        }

        utilityIndex = -1;
    }

    void CacheMissingReferences()
    {
        button ??= GetComponent<Button>();

        if (button != null)
        {
            background ??= button.targetGraphic as Image;
        }

        background ??= GetComponent<Image>();
        label ??= GetComponentInChildren<TMP_Text>(true);
    }
}
