using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(BattleUIManager))]
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public BattleUIManager UI { get; private set; }

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
    public BattleUnit GetUnit(int id) => _idToUnit[id];

    public Action OnSomeoneDied;

    public int _currentTurn { get; private set; }

    #region Unity Methods
    void Awake()
    {
        Instance = this;
        UI = GetComponent<BattleUIManager>();
    }

    void OnDisable()
    {
        _cancelSource?.Cancel();
        _cancelSource?.Dispose();
        _battleInProgress = false;
    }
    #endregion
    #region Battle

    private bool _battleInProgress = false;
    public void StartBattle(BattleData battleData)
    {
        if (_battleInProgress)
        {
            Debug.LogWarning("[전투 진행중이라 시작 안됨]");
            return;
        }
        _battleInProgress = true;

        InitializeBattle(battleData);

        _cancelSource = new();
        BattleProcess(_cancelSource.Token).Forget();
    }

    public void ForceFinishBattle()
    {
        _cancelSource?.Cancel();
        _cancelSource?.Dispose();
        Debug.LogWarning("[전투 강제 종료됨]");
        _battleInProgress = false;
    }

    private int _battleId;
    private void InitializeBattle(BattleData battleData)
    {
        _battleId = 0;
        _currentTurn = 0;
        _idToUnit = new();
        TurnManager.Instance.Init();

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
        unit.SetUnit(_battleId++, UnitTeam.Player);
        _idToUnit[unit.Id] = unit;
        _playerUnits[index++] = unit;
    }

    private void SetEnemyUnit(ref int index, BattleUnit unitPrefab)
    {
        BattleUnit unit = Instantiate(unitPrefab, _enemyPos[index]);
        unit.transform.localPosition = Vector3.zero;
        unit.SetUnit(_battleId++, UnitTeam.Enemy);
        _idToUnit[unit.Id] = unit;
        _enemyUnits[index++] = unit;
    }

    private int[] _turnOrder;

    public int curTurnUnitId()
    {
        if (_turnOrder == null) return -1;
        return _turnOrder[0];
    }


    async UniTask BattleProcess(CancellationToken token)
    {
        // 필요한거
        // - 종료 체크
        // - 턴 매니저에서 순서 결정
        // - 해당 유닛 턴 시작
        if (!SetTurnOrder(true)) return;
        while (true)
        {
            _currentTurn++;
            int curUnitId = _turnOrder[0];
            // string tOrder = "";
            // foreach (var item in _turnOrder)
            // {
            //     tOrder += $"{_idToUnit[item].Info_Name}({item})\n";
            // }
            // print(tOrder);
            UI.ShowTurn(_turnOrder);
            if (GetUnit(curUnitId).Team == UnitTeam.Player)
            {
                await GetUnit(curUnitId).OnPlayerTurn(token);
            }
            else
            {
                await GetUnit(curUnitId).OnEnemyTurn(token);
            }
            if (!SetTurnOrder()) return;
        }
    }

    private bool SetTurnOrder(bool isInit = false)
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
        _turnOrder = TurnManager.Instance.GetTurnOrder(units, isInit);
        if (_turnOrder == null)
        {
            CheckGameEnd();
            Debug.LogWarning("[BattleManager > SetTurnOrder : _turnOrder null반환]");
            return false;
        }
        return true;
    }

    private void CheckGameEnd()
    {

    }

    private void ClearGame()
    {
        _battleInProgress = false;
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