using System;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    [SerializeField] KeyCode muffle;
    [SerializeField] KeyCode runBattle;
    void Update()
    {
        if(Input.GetKeyDown(runBattle)) BattleManager.Instance.StartBattle();
    }
}
