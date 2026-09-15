using System;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] Transform[] _playerPos = new Transform[MaxPlayerUnits];
    [SerializeField] Transform[] _enemyPos = new Transform[MaxEnemyUnits];
    [SerializeField] GameObject endPannel;
    readonly BattleUnit[] _playerUnits = new BattleUnit[MaxPlayerUnits];
    readonly BattleUnit[] _enemyUnits = new BattleUnit[MaxEnemyUnits];
    readonly Dictionary<int, BattleUnit> _idToUnit = new();
    CancellationTokenSource _cancelSource;
    int[] _turnOrder;
    public BattleUnit[] PlayerUnits => _playerUnits;
    public BattleUnit[] EnemyUnits => _enemyUnits;
    public BattleUnit[] AllUnits => _playerUnits.Concat(_enemyUnits).ToArray();
    public bool IsBattleInProgress { get; private set; }
    public int CurrentTurn { get; private set; }
    public BattleOutcome? LastOutcome { get; private set; }
    public Action OnSomeoneDied;
    public BattleUnit GetUnit(int id) => _idToUnit[id];
    public bool TryGetUnit(int id, out BattleUnit unit) => _idToUnit.TryGetValue(id, out unit) && unit != null;
    public int curTurnUnitId() => _turnOrder != null && _turnOrder.Length > 0 ? _turnOrder[0] : -1;

    void Awake() { Instance = this; UI = GetComponent<BattleUIManager>(); }
    void OnDisable() => ForceFinishBattle();
    void OnDestroy() { if (Instance == this) Instance = null; }
    public void StartBattle(BattleData data) => TryStartBattle(data);

    public bool TryStartBattle(BattleData data)
    {
        if (IsBattleInProgress || !isActiveAndEnabled) return false;
        if (data == null || !ValidTeam(data.PlayerUnits, _playerPos, MaxPlayerUnits)
            || !ValidTeam(data.EnemyUnits, _enemyPos, MaxEnemyUnits)) return false;
        IsBattleInProgress = true;
        LastOutcome = null;
        CurrentTurn = 0;
        if (endPannel != null) endPannel.SetActive(false);
        _cancelSource = new CancellationTokenSource();
        RunBattle(data.Copy(), _cancelSource).Forget();
        return true;
    }

    static bool ValidTeam(BattleUnit[] team, Transform[] positions, int capacity)
    {
        if (team == null) return false;
        int count = team.Count(unit => unit != null);
        return count > 0 && count <= capacity && positions != null && positions.Length >= count
            && positions.Take(count).All(pos => pos != null);
    }

    public void ForceFinishBattle() => _cancelSource?.Cancel();

    async UniTask RunBattle(BattleData data, CancellationTokenSource source)
    {
        BattleOutcome outcome = BattleOutcome.Abandoned;
        CancellationToken token = source.Token;
        try
        {
            TurnManager.Instance.Init();
            SpawnTeam(data.PlayerUnits, _playerPos, _playerUnits, UnitTeam.Player);
            SpawnTeam(data.EnemyUnits, _enemyPos, _enemyUnits, UnitTeam.Enemy);
            await UniTask.Yield(token);
            while (true)
            {
                token.ThrowIfCancellationRequested();
                bool playerAlive = _playerUnits.Any(unit => unit != null && !unit.IsDied);
                bool enemyAlive = _enemyUnits.Any(unit => unit != null && !unit.IsDied);
                if (!playerAlive || !enemyAlive)
                {
                    outcome = playerAlive ? BattleOutcome.Victory : BattleOutcome.Defeat;
                    break;
                }
                _turnOrder = TurnManager.Instance.GetTurnOrder(AllUnits.Where(unit => unit != null).ToList(), CurrentTurn == 0);
                if (_turnOrder == null) throw new InvalidOperationException("생존 유닛의 턴 생성 실패");
                CurrentTurn++;
                UI.ShowTurn(_turnOrder);
                BattleUnit actor = GetUnit(_turnOrder[0]);
                if (actor.Team == UnitTeam.Player) await actor.OnPlayerTurn(token);
                else await actor.OnEnemyTurn(token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { outcome = BattleOutcome.Abandoned; }
        catch (Exception exception) { outcome = BattleOutcome.Error; Debug.LogException(exception); }
        finally
        {
            try { CleanupBattle(); }
            finally { _cancelSource = null; source.Dispose(); IsBattleInProgress = false; }
        }
        LastOutcome = outcome;
        if (this == null || !isActiveAndEnabled) return;
        if (data.onFinished == null && endPannel != null) endPannel.SetActive(true);
        try
        {
            if (outcome == BattleOutcome.Victory) data.onVictory?.Invoke();
            else if (outcome == BattleOutcome.Defeat) data.onLoss?.Invoke();
        }
        finally { data.onFinished?.Invoke(outcome); }
    }

    void SpawnTeam(BattleUnit[] prefabs, Transform[] positions, BattleUnit[] destination, UnitTeam team)
    {
        int position = 0;
        foreach (BattleUnit prefab in prefabs)
        {
            if (prefab == null) continue;
            BattleUnit unit = Instantiate(prefab, positions[position]);
            unit.transform.localPosition = Vector3.zero;
            unit.SetUnit(_idToUnit.Count, team);
            _idToUnit.Add(unit.Id, unit);
            destination[position++] = unit;
        }
    }

    void CleanupBattle()
    {
        TargetManager.Instance?.ResetSelection();
        if (UI != null) UI.ClearBattleUI();
        foreach (BattleUnit unit in AllUnits)
        {
            if (unit == null) continue;
            unit.ClearEffect(EffectType.Positive, true);
            unit.ResetAnimation();
            unit.gameObject.SetActive(false);
            Destroy(unit.gameObject);
        }
        Array.Clear(_playerUnits, 0, _playerUnits.Length);
        Array.Clear(_enemyUnits, 0, _enemyUnits.Length);
        _idToUnit.Clear();
        _turnOrder = null;
        TurnManager.Instance?.Init();
        CommandInvoker.ClearHistory();
        if (Cam.Instance != null) Cam.Instance.ResetBattleView();
    }
}

public enum BattleOutcome { Victory, Defeat, Abandoned, Error }

[Serializable]
public class BattleData
{
    public BattleUnit[] PlayerUnits = new BattleUnit[BattleManager.MaxPlayerUnits];
    public BattleUnit[] EnemyUnits = new BattleUnit[BattleManager.MaxEnemyUnits];
    public Action onVictory;
    public Action onLoss;
    public Action<BattleOutcome> onFinished;
    public BattleData Copy() => new BattleData
    {
        PlayerUnits = (BattleUnit[])PlayerUnits.Clone(), EnemyUnits = (BattleUnit[])EnemyUnits.Clone(),
        onVictory = onVictory, onLoss = onLoss, onFinished = onFinished
    };
}
