using UnityEngine;
using UnityEngine.UI;

public class StageMapSceneUI : MonoBehaviour
{
    [SerializeField] Text _stageName;
    [SerializeField] Text _progressText;
    [SerializeField] Button _normalRoom;
    [SerializeField] Button _finalRoom;
    [SerializeField] Text _normalLabel;
    [SerializeField] Text _finalLabel;
    GameSession _session;

    void Start()
    {
        _session = GameSession.Instance;
        if (_session.Run == null) _session.BeginStage();
        StageSession run = _session.Run;
        if (run == null) { _session.Scenes.Load(_session.Settings.ReadyScene); return; }
        _stageName.text = _session.Settings.StageName;
        _progressText.text = "확보 구역  " + run.CompletedRooms + " / " + StageSession.RoomCount;
        _normalRoom.interactable = run.CanEnter(0);
        _finalRoom.interactable = run.CanEnter(1);
        _normalLabel.text = run.CompletedRooms > 0 ? "01  외곽 경비\n확보 완료" : "01  외곽 경비\n전투 진입";
        _finalLabel.text = run.CompletedRooms == 1 ? "02  중앙 격벽\n최종 전투 진입" : "02  중앙 격벽\n경로 잠김";
    }

    public void EnterNormal() => EnterRoom(0);
    public void EnterFinal() => EnterRoom(1);
    public void EnterRoom(int index)
    {
        if (_session.Scenes.IsLoading || !_session.Run.Enter(index)) return;
        _normalRoom.interactable = _finalRoom.interactable = false;
        _session.Scenes.Load(_session.Settings.BattleScene);
    }
    public void Abandon()
    {
        if (_session.Scenes.IsLoading || !_session.AbandonStage()) return;
        _session.Scenes.Load(_session.Settings.ResultScene);
    }
}
