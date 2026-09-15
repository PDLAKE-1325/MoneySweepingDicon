using UnityEngine;
using UnityEngine.UI;

public class BattleReadySceneUI : MonoBehaviour
{
    [SerializeField] UnitSelectionUI _unitPrefab;
    [SerializeField] Transform _unitList;
    [SerializeField] Text _selectionText;
    [SerializeField] Button _deployButton;
    GameSession _session;
    UnitSelectionUI[] _units;

    void Start()
    {
        _session = GameSession.Instance;
        _units = new UnitSelectionUI[_session.Settings.RosterCount];
        for (int i = 0; i < _units.Length; i++)
        {
            _units[i] = Instantiate(_unitPrefab, _unitList);
            _units[i].Bind(i, _session.Settings.GetRosterUnit(i), _session.IsSelected(i), ToggleUnit);
        }
        RefreshSelection();
    }

    public void ToggleUnit(int index)
    {
        if (!_session.ToggleUnit(index)) return;
        _units[index].SetSelected(_session.IsSelected(index));
        RefreshSelection();
    }

    void RefreshSelection()
    {
        _selectionText.text = "출격 인원  " + _session.SelectedCount + " / " + BattleManager.MaxPlayerUnits;
        _deployButton.interactable = _session.SelectedCount > 0;
    }

    public void Deploy()
    {
        if (!_session.BeginStage()) return;
        _deployButton.interactable = false;
        _session.Scenes.Load(_session.Settings.MapScene);
    }
    public void Back()
    {
        if (_session.Run == null) _session.Scenes.Load(_session.Settings.MainScene);
    }
}
