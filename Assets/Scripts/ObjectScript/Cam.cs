using UnityEngine;

public class Cam : MonoBehaviour
{
    public static Cam Instance { get; private set; }

    public Camera MainCamera { get; private set; }

    private void Awake()
    {
        Instance = this;
        MainCamera = Camera.main;
    }
}
