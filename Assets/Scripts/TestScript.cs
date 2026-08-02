using System;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    [SerializeField] KeyCode muffle;
    [SerializeField] KeyCode runBattle;
    [SerializeField] KeyCode finishBattle;

    [SerializeField] BattleData TestBattleData;
    void Update()
    {
        if (Input.GetKeyDown(runBattle)) BattleManager.Instance.StartBattle(TestBattleData);
        if (Input.GetKeyDown(finishBattle)) BattleManager.Instance.ForceFinishBattle();
    }
}
