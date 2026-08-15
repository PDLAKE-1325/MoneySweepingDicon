using System;
using System.Threading;
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

    [SerializeField] protected AnimationClip _animClip;
    protected bool _endAnim = false;

    public virtual async UniTask Act(int userId, int[] targetsId)
    {
        BattleUnit user = BattleManager.Instance.GetUnit(userId);
        bool playAct = false;
        _endAnim = false;
        user.PlayAnimClip(_animClip, () => playAct = true, () => _endAnim = true);
        if (await UniTask.WaitUntil(() => playAct == true).TimeoutWithoutException(TimeSpan.FromMinutes(10)))
        {
            Debug.LogError("Act 타임아웃 >" + Name);
            return;
        }
    }
}