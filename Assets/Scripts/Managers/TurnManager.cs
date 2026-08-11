using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    void Awake() => Instance = this;

    const double TakeTurnValue = 10000;

    public int[] GetTurnOrder(int length, List<BattleUnit> units)
    {
        if (units.Count <= 0)
        {
            Debug.LogWarning("[TurnManager > GetTurnOrder 유닛이 안들어옴]");
            return new int[length];
        }

        int[] order = new int[length];
        double[] turnTime = new double[units.Count];

        for (int i = 0; i < units.Count; i++)
        {
            turnTime[i] = TakeTurnValue / units[i].UnitData.Status.Speed;
        }

        for (int i = 0; i < length; i++)
        {
            double minValue = double.MaxValue;
            int minUnitId = -1;
            int minUnits = 0;
            int minIdx = -1;
            for (int j = 0; j < units.Count; j++)
            {
                if (turnTime[j] == minValue)
                {
                    minUnits++;
                    if (minUnitId > units[j].Id)
                    {
                        minUnitId = units[j].Id;
                        minIdx = j;
                    }
                }
                else if (turnTime[j] < minValue && turnTime[j] != -1)
                {
                    minUnits = 1;
                    minValue = turnTime[j];
                    minUnitId = units[j].Id;
                    minIdx = j;
                }
            }


            if (minUnits == 1)
                for (int j = 0; j < units.Count; j++)
                {
                    turnTime[j] -= minValue;
                    if (turnTime[j] <= 0) turnTime[j] = TakeTurnValue / units[j].UnitData.Status.Speed;
                }
            else
            {
                turnTime[minIdx] = -1;
            }

            order[i] = minUnitId;
        }

        return order;
    }
}