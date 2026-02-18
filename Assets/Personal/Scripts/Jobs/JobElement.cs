using EditorAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JobElement : MonoBehaviour
{
    [Title("UI")]
    public Button button;
    public TMP_Text displayNameText;
    public Image iconImage;

    [Title("Data")]
    public JobInstance jobInstance;
}
