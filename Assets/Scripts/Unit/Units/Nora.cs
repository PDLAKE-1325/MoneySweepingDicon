using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class Nora : BattleUnit
{
    [SerializeField] BattleAction _normalAttack;
    [SerializeField] BattleAction _skill_1;
    [SerializeField] BattleAction _ultimate;

    TurnActionType? _requestedAction;
    bool _acceptingAction;
    public bool RequestAction(TurnActionType action)
    {
        if (!_acceptingAction || IsDied || Team != UnitTeam.Player || _requestedAction.HasValue) return false;
        if (action != TurnActionType.NormalAttack && action != TurnActionType.Skill_1 && action != TurnActionType.Ultimate) return false;
        _requestedAction = action;
        return true;
    }

    public override async UniTask OnPlayerTurn(CancellationToken token)
    {
        BattleManager.Instance.UI.DisplayActions(new(_normalAttack, "Q"), new(_skill_1, "W"), new(_ultimate, "E"));
        OnTurnStart();
        try
        {
            while (!IsDied)
            {
                _acceptingAction = true;
                await UniTask.WaitUntil(() => Input.anyKeyDown || _requestedAction.HasValue || IsDied, cancellationToken: token);
                _acceptingAction = false;
                if (IsDied) break;
                TurnActionType? action = _requestedAction;
                _requestedAction = null;
                if (!action.HasValue)
                {
                    if (Input.GetKeyDown(KeyCode.Q)) action = TurnActionType.NormalAttack;
                    else if (Input.GetKeyDown(KeyCode.W)) action = TurnActionType.Skill_1;
                    else if (Input.GetKeyDown(KeyCode.E)) action = TurnActionType.Ultimate;
                }
                if (action.HasValue && await ExecuteTurnAction(action.Value, token)) break;
                await UniTask.Yield(token);
            }
        }
        finally
        {
            _acceptingAction = false;
            _requestedAction = null;
            if (BattleManager.Instance != null) BattleManager.Instance.UI.DisplayActions();
        }
        if (!IsDied) OnTurnEnd();
    }

    public override async UniTask OnEnemyTurn(CancellationToken token)
    {
        OnTurnStart();
        if (IsDied) return;
        await ExecuteTurnAction(TurnActionType.NormalAttack, token);
        if (!IsDied) OnTurnEnd();
    }

    protected override async UniTask<bool> NormalAttack(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣을거
        int[] targets = await TargetManager.Instance.SelectTarget(this, _normalAttack.TargetType, _normalAttack.MaxTargets, token);
        if (targets == null)
            return false;

        print("reQ");
        ICommand command = new TurnAction(_normalAttack, Id, targets, token);
        await CommandInvoker.ExecuteCommand(command);
        return true;
    }

    protected override async UniTask<bool> Skill_1(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣을거
        int[] targets = await TargetManager.Instance.SelectTarget(this, _skill_1.TargetType, _skill_1.MaxTargets, token);
        if (targets == null)
            return false;
        ICommand command = new TurnAction(_skill_1, Id, targets, token);
        await CommandInvoker.ExecuteCommand(command);
        return true;
    }
    protected override async UniTask<bool> Ultimate(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣을거
        int[] targets = await TargetManager.Instance.SelectTarget(this, _ultimate.TargetType, _ultimate.MaxTargets, token);
        if (targets == null)
            return false;
        ICommand command = new TurnAction(_ultimate, Id, targets, token);
        await CommandInvoker.ExecuteCommand(command);
        return true;
    }





    [SerializeField] GameObject _shellPrefab;
    [SerializeField] Transform _shellEjectPoint;

    [SerializeField] float recoilsMulti;

    public void VFX_Shell()
    {
        GameObject shell = Instantiate(
            _shellPrefab,
            _shellEjectPoint.position,
            _shellEjectPoint.rotation
        );

        Rigidbody rb = shell.GetComponent<Rigidbody>();

        rb.AddForce(
            (Team == UnitTeam.Player ? -_shellEjectPoint.right : _shellEjectPoint.right) * UnityEngine.Random.Range(2.5f, 3.8f) +
            _shellEjectPoint.up * UnityEngine.Random.Range(0.2f, 0.6f) * recoilsMulti,
            ForceMode.Impulse
        );

        rb.AddTorque(
            UnityEngine.Random.insideUnitSphere * 5f,
            ForceMode.Impulse
        );

        Destroy(shell, 3f);
    }

    [SerializeField] float _camShakeDuration;
    public void ShakeCamera(float size)
    {
        Cam.Instance.CamMovement.ShakeCamera(_camShakeDuration, size);
    }
}
