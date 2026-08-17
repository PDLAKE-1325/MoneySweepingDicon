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

    public override async UniTask OnPlayerTurn(CancellationToken token)
    {
        print($"[턴 시작 > {Info_Name} - {TurnManager.Instance.GetBattleTime()}]");
        Tuple<BattleAction, string>[] tuples =
        {
            new(_normalAttack, "Q"),
            new(_skill_1, "W"),
            new(_ultimate, "E")
        };
        BattleManager.Instance.UI.DisplayActions(tuples);
        OnTurnStart();
        while (true)
        {
            await UniTask.WaitUntil(() => Input.anyKeyDown, cancellationToken: token);
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (await ExecuteTurnAction(TurnActionType.NormalAttack, token))
                    break;
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                // ICommand command = new
                if (await ExecuteTurnAction(TurnActionType.Skill_1, token))
                    break;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                // ICommand command = new
                if (await ExecuteTurnAction(TurnActionType.Ultimate, token))
                    break;
            }
        }
        // print($"[턴 종료 > {_unitData.Name}]");
        OnTurnEnd();
        BattleManager.Instance.UI.DisplayActions();
    }
    public override async UniTask OnEnemyTurn(CancellationToken token)
    {
        print($"[턴 시작 > {Info_Name} - {TurnManager.Instance.GetBattleTime()}]");
        OnTurnStart();
        await ExecuteTurnAction(TurnActionType.NormalAttack, token);
        // print($"[턴 종료 > {_unitData.Name}]");
        OnTurnEnd();
    }

    protected override async UniTask<bool> NormalAttack(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣을거
        int[] targets = await TargetManager.Instance.SelectTarget(this, _normalAttack.TargetType, _normalAttack.MaxTargets);
        if (targets == null)
            return false;

        print("reQ");
        ICommand command = new TurnAction(_normalAttack, Id, targets);
        await CommandInvoker.ExecuteCommand(command);
        return true;
    }

    protected override async UniTask<bool> Skill_1(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣을거
        int[] targets = await TargetManager.Instance.SelectTarget(this, _normalAttack.TargetType, _normalAttack.MaxTargets);
        if (targets == null)
            return false;
        ICommand command = new TurnAction(_skill_1, Id, targets);
        await CommandInvoker.ExecuteCommand(command);
        return true;
    }
    protected override async UniTask<bool> Ultimate(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣을거
        int[] targets = await TargetManager.Instance.SelectTarget(this, _normalAttack.TargetType, _normalAttack.MaxTargets);
        if (targets == null)
            return false;
        ICommand command = new TurnAction(_ultimate, Id, targets);
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
