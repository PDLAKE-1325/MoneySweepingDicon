using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
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
        DelegateAction.Act(() => Destroy(gameObject), _destroyTime).Forget();
    }
}