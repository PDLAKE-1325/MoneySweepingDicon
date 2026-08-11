using Cysharp.Threading.Tasks;
using UnityEngine;

public class BattleUnit : Unit
{
    [SerializeField] UnitTeam _team;
    [SerializeField] int _id;
    public UnitTeam Team => _team;
    public int Id => _id;
    // UnitData, _unitdata

    public void SetUnitId(int num) => _id = num;

    protected virtual void Update()
    {
        RotateToCamera();
    }

    protected virtual void RotateToCamera()
    {
        transform.rotation = Cam.Instance.MainCamera.transform.rotation;
    }

    public virtual async UniTask Act()
    {

    }
}