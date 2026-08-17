using System;
using UnityEngine;

[RequireComponent(typeof(CamMovement))]
public class Cam : MonoBehaviour
{
    public static Cam Instance { get; private set; }
    public Camera MainCamera { get; private set; }
    public CamMovement CamMovement { get; private set; }

    public static Action<BattleUnit> OnEntityEnter;
    public static Action OnEntityExit;

    [SerializeField] LayerMask _objectLayer;
    [SerializeField] GameObject _targetIndicater;

    public bool IsTargetSelectionView { get; private set; } = false;
    public void SetTSView(bool parm) => IsTargetSelectionView = parm;

    private void Awake()
    {
        Instance = this;
        if (CamMovement == null) CamMovement = GetComponent<CamMovement>();
    }

    private BattleUnit _unit = null;
    private RaycastHit _currentHit;
    private bool _entityActionEnterable = true;

    private void Start() => MainCamera = Camera.main;

    private void Update()
    {
        if (_targetIndicater == null) return;

        if (!IsTargetSelectionView)
        {
            DeActivate();
            return;
        }

        CastRay();

        if (_unit != null)
            OnHoverStay();
    }

    private void DeActivate()
    {
        if (_unit != null)
        {
            _entityActionEnterable = true;
            OnEntityExit?.Invoke();
        }

        _unit = null;
        _currentHit = default;
        _targetIndicater.SetActive(false);
    }

    private void CastRay()
    {
        Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _objectLayer))
        {
            if (hit.collider != _currentHit.collider)
            {
                _currentHit = hit;
                hit.transform.TryGetComponent(out _unit);
                _unit = hit.transform.GetComponentInParent<BattleUnit>();
                if (_unit != null && !_unit.IsDied && TargetManager.Instance.Aimable(_unit)) _targetIndicater.SetActive(true);
                else _targetIndicater.SetActive(false);
            }
            if (_entityActionEnterable && _unit != null)
            {
                _entityActionEnterable = false;
                OnEntityEnter?.Invoke(_unit);
            }
        }
        else
        {
            DeActivate();
        }
    }

    private void OnHoverStay()
    {
        _targetIndicater.transform.position = _unit.TargetMarkPoint.position;
        if (Input.GetMouseButtonDown(0) && !_unit.IsDied)
        {
            TargetManager.Instance.TargetClicked(_unit);
        }
    }
}
