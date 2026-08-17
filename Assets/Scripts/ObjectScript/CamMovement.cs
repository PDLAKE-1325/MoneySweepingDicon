using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class CamMovement : MonoBehaviour
{
    private Cam _cam;
    [SerializeField] Transform _camPivot;
    [SerializeField] float _rotateTime = 0.05f;
    private Vector3 _camRotation = new();

    private void Awake()
    {
        if (_cam == null) _cam = GetComponent<Cam>();
    }

    public void ShakeCamera(float duration, float size)
    {
        _cam.MainCamera.DOShakePosition(duration, size);
    }

    public void RotateCameraPivot(Vector3 rotation = new())
    {
        _camRotation = rotation;
    }

    void Update()
    {
        _camPivot.transform.DOKill();
        _camPivot.transform.DORotate(_camRotation, _rotateTime).SetEase(Ease.InOutSine);
    }
}