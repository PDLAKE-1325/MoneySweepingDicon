using Unity.VisualScripting;
using UnityEngine;

public class BattleUnit : Unit
{
    [SerializeField] UnitTeam _team;
    public UnitTeam Team => _team;

    protected virtual void Update()
    {
        RotateToCamera();
    }

    protected virtual void RotateToCamera()
    {
        transform.rotation = Cam.Instance.MainCamera.transform.rotation;
    }
}