using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Draws one <see cref="UI_Bar_ColorPreset"/> as a single row: [label] [color] [SET]. Clicking SET
/// applies that swatch's color to the owning <see cref="UI_Bar"/>'s fill image (with undo), so a
/// palette can be applied with one click. UI Toolkit so it composes with the EditorAttributes-drawn
/// rest of the UI_Bar inspector.
/// </summary>
[CustomPropertyDrawer(typeof(UI_Bar_ColorPreset))]
public class UI_Bar_ColorPresetDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        SerializedProperty labelProp = property.FindPropertyRelative("label");
        SerializedProperty colorProp = property.FindPropertyRelative("color");

        VisualElement row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

        TextField labelField = new TextField { bindingPath = labelProp.propertyPath, isDelayed = false };
        labelField.style.flexGrow = 0.4f;
        labelField.style.marginRight = 4;

        ColorField colorField = new ColorField { bindingPath = colorProp.propertyPath };
        colorField.style.flexGrow = 1f;

        Button setButton = new Button(() =>
        {
            UI_Bar bar = property.serializedObject.targetObject as UI_Bar;
            if (bar == null || bar.fillImage == null)
                return;

            Undo.RecordObject(bar.fillImage, "Set Bar Color");
            bar.SetColor(colorProp.colorValue);
            EditorUtility.SetDirty(bar.fillImage);
        })
        { text = "SET" };
        setButton.style.width = 44;
        setButton.style.marginLeft = 4;

        row.Add(labelField);
        row.Add(colorField);
        row.Add(setButton);
        return row;
    }
}
