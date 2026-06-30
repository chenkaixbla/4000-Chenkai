using UnityEditor;
using UnityEngine;

/// <summary>
/// Small IMGUI helper for drawing tidy inspector tables: a colored title bar, a column-label
/// header row, and zebra-striped data rows with column rects. Shared by the manager editors so
/// their custom lists look consistent (background coloring, headers, grayed disabled rows).
/// </summary>
public static class EditorTableGUI
{
    static readonly Color TitleColor = new Color(0.17f, 0.36f, 0.53f);
    static readonly Color HeaderColor = new Color(0.24f, 0.24f, 0.24f);
    static readonly Color RowEven = new Color(1f, 1f, 1f, 0.03f);
    static readonly Color RowOdd = new Color(0f, 0f, 0f, 0.12f);
    static readonly Color StrikeColor = new Color(0.85f, 0.4f, 0.4f, 0.95f);

    static Rect _lastRow;

    const float TitleHeight = 22f;
    const float CellPad = 6f;
    const float ColSpacing = 6f;

    static GUIStyle _title;
    static GUIStyle _header;
    static GUIStyle _cell;

    static GUIStyle Title_ => _title ??= new GUIStyle(EditorStyles.boldLabel)
    { normal = { textColor = Color.white }, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(6, 6, 0, 0) };
    static GUIStyle Header_ => _header ??= new GUIStyle(EditorStyles.miniBoldLabel)
    { normal = { textColor = new Color(0.82f, 0.82f, 0.82f) }, alignment = TextAnchor.MiddleLeft };
    static GUIStyle Cell_ => _cell ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };

    static float RowHeight => EditorGUIUtility.singleLineHeight + 6f;

    /// <summary>Colored title bar spanning the inspector width.</summary>
    public static void DrawTitle(string text)
    {
        Rect r = EditorGUILayout.GetControlRect(false, TitleHeight);
        EditorGUI.DrawRect(r, TitleColor);
        GUI.Label(r, text, Title_);
    }

    /// <summary>Column-label header row. <paramref name="widths"/> &lt;= 0 means flexible.</summary>
    public static void DrawHeader(float[] widths, string[] labels)
    {
        Rect row = EditorGUILayout.GetControlRect(false, RowHeight);
        EditorGUI.DrawRect(row, HeaderColor);
        Rect[] cells = Split(row, widths);
        for (int i = 0; i < labels.Length && i < cells.Length; i++)
            GUI.Label(cells[i], labels[i], Header_);
    }

    /// <summary>Begins a data row: draws the zebra background and returns the column rects.</summary>
    public static Rect[] DrawRow(float[] widths, int index)
    {
        Rect row = EditorGUILayout.GetControlRect(false, RowHeight);
        _lastRow = row;
        EditorGUI.DrawRect(row, (index & 1) == 0 ? RowEven : RowOdd);
        return Split(row, widths);
    }

    /// <summary>Draws a strikethrough line across the most recent row (call after its content).</summary>
    public static void StrikeRow()
    {
        float y = Mathf.Round(_lastRow.y + _lastRow.height * 0.5f);
        EditorGUI.DrawRect(new Rect(_lastRow.x + 4f, y, _lastRow.width - 8f, 1f), StrikeColor);
    }

    /// <summary>Draws a text label in a cell.</summary>
    public static void LabelCell(Rect cell, string text)
    {
        GUI.Label(cell, text, Cell_);
    }

    static Rect[] Split(Rect row, float[] widths)
    {
        row = new Rect(row.x + CellPad, row.y, row.width - CellPad * 2f, row.height);

        float fixedTotal = 0f;
        int flexCount = 0;
        for (int i = 0; i < widths.Length; i++)
        {
            if (widths[i] > 0f) fixedTotal += widths[i];
            else flexCount++;
        }

        float spacing = ColSpacing * (widths.Length - 1);
        float flexWidth = flexCount > 0 ? Mathf.Max(40f, (row.width - fixedTotal - spacing) / flexCount) : 0f;

        float h = EditorGUIUtility.singleLineHeight;
        float y = row.y + (row.height - h) * 0.5f;

        Rect[] cells = new Rect[widths.Length];
        float x = row.x;
        for (int i = 0; i < widths.Length; i++)
        {
            float w = widths[i] > 0f ? widths[i] : flexWidth;
            cells[i] = new Rect(x, y, w, h);
            x += w + ColSpacing;
        }

        return cells;
    }
}
