using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(BattleUIManager))]
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public BattleUIManager UIManager { get; private set; }

    public const int MaxPlayerUnits = 4;
    public const int MaxEnemyUnits = 4;
    public const int TurnOrderLength = 10;
    CancellationTokenSource _cancelSource;

    [SerializeField] Transform[] _playerPos = new Transform[MaxPlayerUnits];
    [SerializeField] Transform[] _enemyPos = new Transform[MaxEnemyUnits];

    private BattleUnit[] _playerUnits = new BattleUnit[MaxPlayerUnits];
    private BattleUnit[] _enemyUnits = new BattleUnit[MaxEnemyUnits];
    public BattleUnit[] PlayerUnits => _playerUnits;
    public BattleUnit[] EnemyUnits => _enemyUnits;

    private Dictionary<int, BattleUnit> _idToUnit;

    #region Unity Methods
    void Awake()
    {
        Instance = this;
        UIManager = GetComponent<BattleUIManager>();
    }

    void OnDisable()
    {
        _cancelSource?.Cancel();
        _cancelSource?.Dispose();
    }
    #endregion
    #region Battle
    public void StartBattle(BattleData battleData)
    {
        InitializeBattle(battleData);

        _cancelSource = new();
        BattleProcess(_cancelSource.Token).Forget();
    }

    public void ForceFinishBattle()
    {
        _cancelSource?.Cancel();
        _cancelSource?.Dispose();
        Debug.LogWarning("[전투 강제 종료됨]");
    }

    private int _battleId;
    private void InitializeBattle(BattleData battleData)
    {
        _battleId = 0;
        _idToUnit = new();
        int posIndex = 0;
        for (int i = 0; i < MaxPlayerUnits; i++)
        {
            if (battleData.PlayerUnits[i] == null) continue;
            SetPlayerUnit(ref posIndex, battleData.PlayerUnits[i]);
        }
        posIndex = 0;
        for (int i = 0; i < MaxEnemyUnits; i++)
        {
            if (battleData.EnemyUnits[i] == null) continue;
            SetEnemyUnit(ref posIndex, battleData.EnemyUnits[i]);
        }
    }

    private void SetPlayerUnit(ref int index, BattleUnit unitPrefab)
    {
        BattleUnit unit = Instantiate(unitPrefab, _playerPos[index]);
        unit.transform.localPosition = Vector3.zero;
        unit.SetUnitId(_battleId++);
        _idToUnit[unit.Id] = unit;
        _playerUnits[index++] = unit;
    }

    private void SetEnemyUnit(ref int index, BattleUnit unitPrefab)
    {
        BattleUnit unit = Instantiate(unitPrefab, _enemyPos[index]);
        unit.transform.localPosition = Vector3.zero;
        unit.SetUnitId(_battleId++);
        _idToUnit[unit.Id] = unit;
        _enemyUnits[index++] = unit;
    }

    private int[] _turnOrder;
    async UniTask BattleProcess(CancellationToken token)
    {
        // 필요한거
        // - 종료 체크
        // - 턴 매니저에서 순서 결정
        // - 해당 유닛 턴 시작
        SetTurnOrder();
        foreach (var item in _turnOrder)
        {
            print(_idToUnit[item].UnitData.Name + " : " + item);
        }
    }

    private void SetTurnOrder()
    {
        List<BattleUnit> units = new();
        for (int i = 0; i < MaxPlayerUnits; i++)
        {
            if (_playerUnits[i] == null) continue;
            units.Add(_playerUnits[i]);
        }
        for (int i = 0; i < MaxEnemyUnits; i++)
        {
            if (_enemyUnits[i] == null) continue;
            units.Add(_enemyUnits[i]);
        }
        _turnOrder = TurnManager.Instance.GetTurnOrder(TurnOrderLength, units);
    }
    #endregion
}

// await TaskAsync();
// await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);

[System.Serializable]
public class BattleData
{
    public BattleUnit[] PlayerUnits = new BattleUnit[BattleManager.MaxPlayerUnits];
    public BattleUnit[] EnemyUnits = new BattleUnit[BattleManager.MaxEnemyUnits];

    // public sound등으로 전투 배경 사운드 넣기.

    public Action onVictory;
    public Action onLoss;
}


// [System.Serializable]
// public class Sound
// {
//     public AudioClip clip;
//     public float volume = 1.0f;
//     public float pitch = 1.0f;
// }