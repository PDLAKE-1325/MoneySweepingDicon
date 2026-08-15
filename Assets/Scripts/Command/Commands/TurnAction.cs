using Cysharp.Threading.Tasks;
using UnityEngine;

public class TurnAction : ICommand
{
    [SerializeField] BattleAction _battleAction;
    [SerializeField] int[] _targetsId;
    [SerializeField] int _userId;
    public CommandInfo Info { get; private set; }

    public TurnAction(BattleAction action, int userId, int[] targetsId)
    {
        _battleAction = action;
        _targetsId = targetsId;
        _userId = userId;

        string[] targets = new string[targetsId.Length];
        for (int i = 0; i < targetsId.Length; i++)
            targets[i] = BattleManager.Instance.GetUnit(targetsId[i]).Info_Name;

        Info = new(action.Name, BattleManager.Instance.GetUnit(userId).Info_Name, targets, action.Description);
    }

    public async UniTask Execute()
    {
        await _battleAction.Act(_userId, _targetsId);
    }
}