using System;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom inspector for <see cref="Item_Slot"/>. Each widget is drawn as one tidy row -
/// [toggle] [field name] [reference] - with the reference field stretched so the assigned
/// object's name is fully visible. Done in code (not stacked attributes) because HorizontalGroup
/// can't lay out a toggle + label + wide object field together without clipping one of them.
/// </summary>
[CustomEditor(typeof(Item_Slot))]
[CanEditMultipleObjects]
public class Item_SlotEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new();

        root.Add(new PropertyField(serializedObject.FindProperty("itemData")));

        HelpBox hint = new("Toggle a row to show/hide that widget.", HelpBoxMessageType.None);
        hint.style.marginTop = 4;
        hint.style.marginBottom = 4;
        root.Add(hint);

        root.Add(WidgetRow("showName", "nameText", "Name", typeof(TMP_Text)));
        root.Add(WidgetRow("showIcon", "iconImage", "Icon", typeof(UnityEngine.UI.Image)));
        root.Add(WidgetRow("showCounter", "counterText", "Counter", typeof(TMP_Text)));

        Button refresh = new(() =>
        {
            foreach (var t in targets)
                ((Item_Slot)t).Refresh();
        })
        { text = "Refresh" };
        refresh.style.marginTop = 6;
        root.Add(refresh);

        return root;
    }

    // One row: a toggle bound to the show flag, the field name, then the object reference field
    // (stretched to fill the row so the referenced object's name shows).
    VisualElement WidgetRow(string boolProp, string objProp, string label, Type objectType)
    {
        VisualElement row = new();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 2;

        Toggle toggle = new() { bindingPath = boolProp };
        toggle.style.marginRight = 4;
        row.Add(toggle);

        Label name = new(label);
        name.style.width = 60;
        name.style.unityTextAlign = TextAnchor.MiddleLeft;
        row.Add(name);

        ObjectField field = new()
        {
            bindingPath = objProp,
            objectType = objectType,
            allowSceneObjects = true
        };
        field.style.flexGrow = 1;
        row.Add(field);

        return row;
    }
}
