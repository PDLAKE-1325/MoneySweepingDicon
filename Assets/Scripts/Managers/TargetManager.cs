using Unity.VisualScripting;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }
    private void Awake() => Instance = this;

    public int[] SelectTarget(BattleUnit selector, TargetType targetType, int maxTargets)
    {
        int[] targets = new int[maxTargets];

        // MaxTargets면 비는곳 새길수 있어서 나중에 선택한 숫자만큼만 배열 길이 해서 해야함.

        // 임시
        if (selector.Team == UnitTeam.Player)
        {
            targets[0] = 1;
        }
        else targets[0] = 0;

        return targets;
    }
}

public enum TargetType
{
    Player, Enemy, Both, All
}