using System;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    [SerializeField] KeyCode muffle;
    [SerializeField] KeyCode runBattle;
    [SerializeField] KeyCode finishBattle;
    void Update()
    {
        if (Input.GetKeyDown(runBattle)) BattleManager.Instance.StartBattle();
        if (Input.GetKeyDown(finishBattle)) BattleManager.Instance.ForceFinishBattle();
    }
}
