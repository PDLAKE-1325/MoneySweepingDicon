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
    [SerializeField] protected AnimationClip[] _animClip = new AnimationClip[1];
    public string Name => _name;
    public string Description => _description;
    public TargetType TargetType => _targetType;
    public int MaxTargets => _maxTargets;

    public async UniTask Act(int userId, int[] targetsId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        BattleUnit user = BattleManager.Instance.GetUnit(userId);
        AnimationClip clip = _animClip != null && _animClip.Length > 0 ? _animClip[0] : null;
        bool hit = clip == null;
        bool ended = clip == null;
        try
        {
            if (clip != null) user.PlayAnimClip(clip, () => hit = true, () => ended = true);
            await WaitForSignal(() => hit, clip == null ? 1f : clip.length + 2f, token);
            token.ThrowIfCancellationRequested();
            await ApplyAction(userId, targetsId, token);
            await WaitForSignal(() => ended, clip == null ? 1f : clip.length + 2f, token);
        }
        finally { if (user != null) user.ResetAnimation(); }
    }

    protected abstract UniTask ApplyAction(int userId, int[] targetsId, CancellationToken token);

    protected static async UniTask WaitForSignal(Func<bool> signal, float seconds, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        using var timer = timeout.CancelAfterSlim(TimeSpan.FromSeconds(Mathf.Max(1f, seconds)), DelayType.UnscaledDeltaTime);
        try { await UniTask.WaitUntil(signal, cancellationToken: timeout.Token); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { throw new TimeoutException("공격 애니메이션 이벤트 시간 초과"); }
    }
}
