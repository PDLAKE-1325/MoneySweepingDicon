using System;
using System.Collections.Generic;
using UnityEngine;

// The only persistent scene service: loadout + the current run, never battle objects or UI.
[DefaultExecutionOrder(-200)]
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }
    [SerializeField] GameFlowSettings _settings;
    [SerializeField] SceneTransition _sceneTransition;
    bool[] _selected;
    public GameFlowSettings Settings => _settings;
    public SceneTransition Scenes => _sceneTransition;
    public StageSession Run { get; private set; }
    public event Action<StageSession> StageSettled;
    public int SelectedCount
    {
        get { int count = 0; foreach (bool selected in _selected) if (selected) count++; return count; }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _selected = new bool[_settings.RosterCount];
        for (int i = 0; i < _selected.Length && i < BattleManager.MaxPlayerUnits; i++) _selected[i] = true;
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
    public bool IsSelected(int index) => _selected[index];
    public bool ToggleUnit(int index)
    {
        if (Run != null || Scenes.IsLoading || index < 0 || index >= _selected.Length) return false;
        if (!_selected[index] && SelectedCount >= BattleManager.MaxPlayerUnits) return false;
        _selected[index] = !_selected[index];
        return true;
    }

    public bool BeginStage()
    {
        if (Run != null || SelectedCount == 0 || Scenes.IsLoading) return false;
        var players = new List<BattleUnit>();
        for (int i = 0; i < _selected.Length; i++) if (_selected[i]) players.Add(_settings.GetRosterUnit(i));
        Run = new StageSession(players.ToArray(), _settings.CopyEnemies());
        return true;
    }

    public bool ResolveBattle(BattleOutcome outcome)
    {
        if (Run == null || !Run.CompleteBattle(outcome)) return false;
        if (Run.IsSettled) StageSettled?.Invoke(Run);
        return true;
    }

    public bool AbandonStage()
    {
        if (Run == null || Run.ActiveRoom >= 0 || !Run.Settle(StageOutcome.Abandoned)) return false;
        StageSettled?.Invoke(Run);
        return true;
    }

    public bool ClearRun()
    {
        if (Run != null && !Run.IsSettled) return false;
        Run = null;
        return true;
    }
}
