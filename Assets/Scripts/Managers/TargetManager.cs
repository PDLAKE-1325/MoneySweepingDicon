using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }
    [SerializeField] Vector3 _playerTargetSelectionRotation;
    [SerializeField] Color _disabledColor;
    readonly List<int> _targets = new();
    readonly Dictionary<SpriteRenderer, Color> _originalColors = new();
    int _maxTargets;
    bool _targetSelected;
    bool _cancelSelection;
    UnitTeam _selectorTeam;
    TargetType _targetType;
    public bool IsSelecting { get; private set; }
    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }
    void Update()
    {
        if (!IsSelecting || _selectorTeam != UnitTeam.Player) return;
        if (Input.GetKeyDown(KeyCode.A)) ApplyTargets();
        if (Input.GetKeyDown(KeyCode.Escape)) _cancelSelection = true;
    }

    public async UniTask<int[]> SelectTarget(BattleUnit selector, TargetType type, int maxTargets, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (IsSelecting) throw new System.InvalidOperationException("이미 대상 선택 중입니다.");
        _targets.Clear();
        _maxTargets = Mathf.Max(1, maxTargets);
        _targetSelected = false;
        _cancelSelection = false;
        _selectorTeam = selector.Team;
        _targetType = type;
        IsSelecting = true;
        try
        {
            BattleUnit[] all = BattleManager.Instance.AllUnits;
            BattleUnit[] candidates = all.Where(Aimable).ToArray();
            if (candidates.Length == 0) return null;
            if (type == TargetType.All || selector.Team == UnitTeam.Enemy)
                return candidates.Take(type == TargetType.All ? candidates.Length : _maxTargets).Select(unit => unit.Id).ToArray();
            Cam.Instance.CamMovement.RotateCameraPivot(_playerTargetSelectionRotation);
            Cam.Instance.SetTSView(true);
            foreach (BattleUnit unit in all)
            {
                if (unit == null || unit.SpriteRenderer == null || Aimable(unit)) continue;
                _originalColors[unit.SpriteRenderer] = unit.SpriteRenderer.color;
                unit.SpriteRenderer.color = _disabledColor;
            }
            await UniTask.WaitUntil(() => _targetSelected || _cancelSelection || selector.IsDied, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            return _targetSelected && !selector.IsDied ? _targets.ToArray() : null;
        }
        finally { ResetSelection(); }
    }

    public void ResetSelection()
    {
        IsSelecting = false;
        _cancelSelection = true;
        _targets.Clear();
        foreach (var pair in _originalColors)
            if (pair.Key != null) pair.Key.color = pair.Value;
        _originalColors.Clear();
        if (Cam.Instance != null)
        {
            Cam.Instance.CamMovement.RotateCameraPivot();
            Cam.Instance.SetTSView(false);
        }
    }

    public void TargetClicked(BattleUnit unit)
    {
        if (!IsSelecting || !Aimable(unit)) return;
        if (_targets.Remove(unit.Id)) return;
        if (_targets.Count >= _maxTargets) return;
        _targets.Add(unit.Id);
        if (_targets.Count == _maxTargets) ApplyTargets();
    }
    public void ApplyTargets() { if (IsSelecting && _targets.Count > 0) _targetSelected = true; }
    public bool Aimable(BattleUnit unit) => unit != null && !unit.IsDied &&
        (_targetType == TargetType.Both || _targetType == TargetType.All
        || (_targetType == TargetType.Enemy && _selectorTeam != unit.Team)
        || (_targetType == TargetType.Player && _selectorTeam == unit.Team));
}
public enum TargetType { Player, Enemy, Both, All }
