using UnityEngine;
using UnityEngine.UI;

public class BattleSceneController : MonoBehaviour
{
    [SerializeField] BattleManager _battleManager;
    [SerializeField] Text _roomName;
    [SerializeField] Button _abandonButton;
    GameSession _session;
    StageSession _run;

    void Start()
    {
        _session = GameSession.Instance;
        // Supports directly pressing Play in the existing battle scene.
        if (_session.Run == null)
        {
            _session.BeginStage();
            _session.Run?.Enter(0);
        }
        _run = _session.Run;
        if (_run == null || _run.IsSettled || _run.ActiveRoom < 0)
        {
            _session.Scenes.Load(_session.Settings.MapScene);
            return;
        }
        _roomName.text = _run.ActiveRoom == 0 ? "외곽 경비 · 일반 전투" : "중앙 격벽 · 최종 전투";
        BattleData data = _run.CreateBattle();
        data.onFinished = OnBattleFinished;
        if (!_battleManager.TryStartBattle(data)) OnBattleFinished(BattleOutcome.Error);
    }

    void OnBattleFinished(BattleOutcome outcome)
    {
        if (this == null || !isActiveAndEnabled || _session.Run != _run) return;
        if (!_session.ResolveBattle(outcome)) return;
        _session.Scenes.Load(_run.IsSettled ? _session.Settings.ResultScene : _session.Settings.MapScene);
    }

    public void Abandon()
    {
        if (!_battleManager.IsBattleInProgress) return;
        _abandonButton.interactable = false;
        _battleManager.ForceFinishBattle();
    }
    void OnDisable() { if (_battleManager != null) _battleManager.ForceFinishBattle(); }
}
