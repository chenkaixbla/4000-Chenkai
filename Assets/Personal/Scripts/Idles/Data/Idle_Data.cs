using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public enum Idle_Kind
{
    Woodcutting,
    Mining,
    Crafting,
    Pet,
    Cooking,
    Custom
}

[CreateAssetMenu(fileName = "Idle_Data", menuName = "Game/Idles/Idle_Data")]
public class Idle_Data : ScriptableObject
{
    [Title("General")]
    [AssetPreview(previewHeight: 96f)] public Sprite icon;
    public string guid;
    public string displayName;
    public Idle_Kind idleKind = Idle_Kind.Woodcutting;

    [Title("Timing")]
    [Min(0.1f)] public float interval = 3f;
    public bool autoRestart = true;
    public bool stopWhenCycleCannotRun = true;

    [Title("Progression")]
    [Min(0)] public int idleXPReward = 10;
    [Min(0)] public int jobXPReward = 5;

    [Title("Rewards (on level up)")]
    public List<Reward_Leveled> rewards = new();

    void OnEnable() => EnsureGuid();

    void OnValidate() => EnsureGuid();

    [Button]
    void GenerateGUID() => guid = Guid.NewGuid().ToString();

    public float GetIdleXpPerSecond() => interval > 0f ? idleXPReward / interval : 0f;

    public float GetJobXpPerSecond() => interval > 0f ? jobXPReward / interval : 0f;

    void EnsureGuid()
    {
        if (string.IsNullOrWhiteSpace(guid))
            GenerateGUID();
    }
}
