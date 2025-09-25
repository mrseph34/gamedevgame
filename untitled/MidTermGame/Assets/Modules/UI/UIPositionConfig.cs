using UnityEngine;

[CreateAssetMenu(fileName = "UIPositionConfig", menuName = "UI/PositionConfig")]
public class UIPositionConfig : ScriptableObject
{
    [Header("Anchors (0–1)")]
    public Vector2 anchorMin = new Vector2(0.5f, 0.5f);
    public Vector2 anchorMax = new Vector2(0.5f, 0.5f);

    [Header("Offset in Pixels")]
    public Vector2 anchoredPosition = Vector2.zero;
}