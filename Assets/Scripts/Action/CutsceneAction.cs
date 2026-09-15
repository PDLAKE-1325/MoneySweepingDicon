using UnityEngine;

public class CutsceneAction : MonoBehaviour
{
    public bool IsAnimEnd { get; private set; }
    [SerializeField] int _destroyTime = 30;

    void Start()
    {
        IsAnimEnd = false;
    }

    public void AnimEnd()
    {
        IsAnimEnd = true;
        gameObject.SetActive(false);
        // Unity cancels delayed destruction when the scene or owner destroys this object first.
        Destroy(gameObject, _destroyTime);
    }
}
