using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }
    private void Awake() => Instance = this;
    [SerializeField] Vector3 _playerTargetSelectionRotation;
    [SerializeField] Color _disabledColor;
    List<int> _targets;
    int _maxTargets;
    bool _targetSelected;
    bool _cancelSelection;
    UnitTeam _selectorTeam;
    TargetType _targetType;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) ApplyTargets();
        if (Input.GetKeyDown(KeyCode.Escape) && _selectorTeam == UnitTeam.Player)
        {
            _cancelSelection = true;
        }
    }

    public async UniTask<int[]> SelectTarget(BattleUnit selector, TargetType targetType, int maxTargets)
    {
        _targets = new();
        _maxTargets = maxTargets;
        _targetSelected = false;
        _cancelSelection = false;
        _selectorTeam = selector.Team;
        _targetType = targetType;

        print(selector.Team);

        BattleUnit[] allUnits = BattleManager.Instance.AllUnits;

        if (selector.Team == UnitTeam.Player)
        {
            Cam.Instance.CamMovement.RotateCameraPivot(_playerTargetSelectionRotation);
            Cam.Instance.SetTSView(true);

            for (int i = 0; i < allUnits.Length; i++)
            {
                BattleUnit unit = allUnits[i];
                if (unit != null && !Aimable(unit))
                    allUnits[i].SpriteRenderer.color = _disabledColor;
            }
        }
        else
        {
            for (int i = 0; i < BattleManager.MaxPlayerUnits; i++)
            {
                BattleUnit unit = BattleManager.Instance.PlayerUnits[i];
                if (unit != null && !unit.IsDied)
                {
                    print("ddd>" + unit.Id);
                    TargetClicked(unit);
                }
            }
            _targetSelected = true;
        }

        await UniTask.WaitUntil(() => _targetSelected == true || _cancelSelection == true);
        Cam.Instance.CamMovement.RotateCameraPivot();
        Cam.Instance.SetTSView(false);

        for (int i = 0; i < allUnits.Length; i++)
        {
            if (allUnits[i] != null)
            {
                allUnits[i].SpriteRenderer.color = new(1, 1, 1, 1);
            }
        }

        if (_targetSelected)
        {
            return _targets.ToArray();
        }
        else
        {
            return null;
        }

    }




    public void TargetClicked(BattleUnit unit)
    {
        print("clicked" + unit.Info_Name);
        int unitId = unit.Id;
        if (_targets.Contains(unitId))
        {
            _targets.Remove(unitId);
            return;
        }

        if (_targets.Count < _maxTargets)
        {
            if (Aimable(unit))
                _targets.Add(unitId);
            if (_targets.Count == _maxTargets)
                ApplyTargets();
        }
    }

    public void ApplyTargets()
    {
        print("apply" + _targets.Count);
        if (_targets.Count > 0)
            _targetSelected = true;
    }

    public bool Aimable(BattleUnit unit)
    {
        if (unit.IsDied) return false;

        bool result = false;

        if (
            _targetType == TargetType.Enemy && _selectorTeam != unit.Team
            || _targetType == TargetType.Player && _selectorTeam == unit.Team
        ) result = true;

        return result;
    }
}

public enum TargetType
{
    Player, Enemy, Both, All
}