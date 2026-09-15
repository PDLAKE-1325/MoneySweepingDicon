using UnityEngine;
using UnityEngine.UI;

public class ResultSceneUI : MonoBehaviour
{
    [SerializeField] Text _outcomeText;
    [SerializeField] Text _summaryText;
    [SerializeField] Text _rewardText;
    [SerializeField] Button _mainButton;
    GameSession _session;
    void Start()
    {
        _session = GameSession.Instance;
        StageSession run = _session.Run;
        _outcomeText.text = run?.Outcome == StageOutcome.Cleared ? "작전 완료"
            : run?.Outcome == StageOutcome.Defeated ? "작전 실패"
            : run?.Outcome == StageOutcome.Abandoned ? "작전 중단" : "결과 없음";
        _summaryText.text = "확보 구역  " + (run?.CompletedRooms ?? 0) + " / " + StageSession.RoomCount;
        _rewardText.text = run?.Outcome == StageOutcome.Error
            ? "전투 오류로 작전이 종료되었습니다. Console을 확인하세요."
            : "보상 시스템은 후속 개발 단계입니다.";
    }
    public void ReturnToMain()
    {
        if (!_session.ClearRun()) return;
        if (_session.Scenes.Load(_session.Settings.MainScene)) _mainButton.interactable = false;
    }
}
