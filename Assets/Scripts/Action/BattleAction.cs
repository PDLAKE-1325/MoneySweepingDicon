using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class BattleAction : ScriptableObject
{
    [SerializeField] string _name;
    [SerializeField] string _description;
    [SerializeField] TargetType _targetType;
    [SerializeField] int _maxTargets;
    public string Name => _name;
    public string Description => _description;
    public TargetType TargetType => _targetType;
    public int MaxTargets => _maxTargets;

    public abstract UniTask Act(int userId, int[] targetsId);
}