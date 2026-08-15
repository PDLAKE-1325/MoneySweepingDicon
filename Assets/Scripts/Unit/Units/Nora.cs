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
    }

    protected override async UniTask NormalAttack(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣기
        int[] targets = TargetManager.Instance.SelectTarget(this, _normalAttack.TargetType, _normalAttack.MaxTargets);

        ICommand command = new TurnAction(_normalAttack, Id, targets);
        CommandInvoker.ExecuteCommand(command);

        await UniTask.Yield(token);
    }

    protected override async UniTask Skill_1(CancellationToken token)
    {
        // 타깃은 여기서 받아서 넣기
        int[] targets = TargetManager.Instance.SelectTarget(this, _normalAttack.TargetType, _normalAttack.MaxTargets);

        ICommand command = new TurnAction(_skill_1, Id, targets);
        CommandInvoker.ExecuteCommand(command);

        await UniTask.Yield(token);
    }
}
