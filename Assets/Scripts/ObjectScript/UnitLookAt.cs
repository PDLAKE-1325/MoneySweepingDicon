using UnityEngine;

public class Unit : MonoBehaviour
{
    public Camera cam;
    void Update()
    {
        transform.rotation = cam.transform.rotation;
    }
}
