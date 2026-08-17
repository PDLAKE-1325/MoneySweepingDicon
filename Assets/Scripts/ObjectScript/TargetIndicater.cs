using UnityEngine;

public class TargetIndicater : MonoBehaviour
{
    void Update()
    {
        transform.rotation = Cam.Instance.MainCamera.transform.rotation;
    }
}
