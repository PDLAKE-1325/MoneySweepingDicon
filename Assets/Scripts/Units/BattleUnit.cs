using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BattleUnit : Unit
{
    [SerializeField] UnitTeam _team;
    [SerializeField] int _id;
    [SerializeField] int _hp;
    public UnitTeam Team => _team;
    public int Id => _id;
    public int Hp => _hp;
    // UnitData, _unitdata

    public void SetUnitId(int num) => _id = num;

    protected override void Awake()
    {
        base.Awake();
        _hp = _unitData.Status.MaxHp;
    }

    protected virtual void Update()
    {
        RotateToCamera();
    }

    protected virtual void RotateToCamera()
    {
        transform.rotation = Cam.Instance.MainCamera.transform.rotation;
    }

    public virtual async UniTask OnMyTurn(CancellationToken token)
    {
        print($"[턴 시작 > {_unitData.Name} - {TurnManager.Instance.GetBattleTime()}]");
        await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.K), cancellationToken: token);
        // print($"[턴 종료 > {_unitData.Name}]");
    }
}
//플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)나는적(2)플레이어(0)
//플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)플레이어(0)플레이어2(1)나는적(2)플레이어(0)플레이어2(1)나는적(2)