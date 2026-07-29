using Unity.VisualScripting;
using UnityEngine;

public class BattleUnit : Unit
{
    protected virtual void Update()
    {
        RotateToCamera();
    }

    protected virtual void RotateToCamera()
    {
        transform.rotation = Cam.Instance.MainCamera.transform.rotation;
    }
}