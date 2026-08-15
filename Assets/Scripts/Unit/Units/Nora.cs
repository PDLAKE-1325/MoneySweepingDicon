using Cysharp.Threading.Tasks;
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
        OnTurnStart();
        while (true)
        {
            await UniTask.WaitUntil(() => Input.anyKeyDown, cancellationToken: token);
            if (Input.GetKeyDown(KeyCode.Q))
            {
                await ExecuteTurnAction(TurnActionType.NormalAttack, token);
                break;
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                // ICommand command = new
                await ExecuteTurnAction(TurnActionType.Skill_1, token);
                break;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                // ICommand command = new
                break;
            }
        }
        // print($"[턴 종료 > {_unitData.Name}]");
        OnTurnEnd();
    }

    protected override async UniTask NormalAttack(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣을거
        int[] targets = TargetManager.Instance.SelectTarget(this, _normalAttack.TargetType, _normalAttack.MaxTargets);

        ICommand command = new TurnAction(_normalAttack, Id, targets);
        await CommandInvoker.ExecuteCommand(command);
    }

    protected override async UniTask Skill_1(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣을거
        int[] targets = TargetManager.Instance.SelectTarget(this, _normalAttack.TargetType, _normalAttack.MaxTargets);

        ICommand command = new TurnAction(_skill_1, Id, targets);
        await CommandInvoker.ExecuteCommand(command);
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
            (Team == UnitTeam.Player ? -_shellEjectPoint.right : _shellEjectPoint.right) * Random.Range(2.5f, 3.8f) +
            _shellEjectPoint.up * Random.Range(0.2f, 0.6f) * recoilsMulti,
            ForceMode.Impulse
        );

        rb.AddTorque(
            Random.insideUnitSphere * 5f,
            ForceMode.Impulse
        );

        Destroy(shell, 3f);
    }
}
