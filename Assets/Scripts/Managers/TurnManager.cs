using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    const double TakeTurnValue = 10000;

    double[] _turnTime;
    public double BattleTime { get; private set; }

    void Awake() => Instance = this;

    public void Init()
    {
        BattleTime = 0;
    }

    public float GetBattleTime() => (int)BattleTime / 100f;

    int _length = BattleManager.TurnOrderLength;
    public int[] GetTurnOrder(List<BattleUnit> units, bool isInit)
    {
        double[] turnTime = new double[units.Count];
        if (isInit)
        {
            _turnTime = new double[units.Count];
            for (int i = 0; i < units.Count; i++)
                turnTime[i] = TakeTurnValue / units[i].Status_Speed;
        }
        else
        {
            for (int i = 0; i < units.Count; i++)
                turnTime[i] = _turnTime[i];
        }


        if (units.Count <= 0)
        {
            Debug.LogWarning("[TurnManager > GetTurnOrder 유닛이 안들어옴]");
            return null;
        }

        int[] order = new int[_length];

        for (int i = 0; i < _length; i++)
        {
            if (i == 1)
                for (int j = 0; j < units.Count; j++)
                    _turnTime[j] = turnTime[j];

            double minValue = double.MaxValue;
            int minUnitId = -1;
            int minUnits = 0;
            int minIdx = -1;
            for (int j = 0; j < units.Count; j++)
            {
                if (units[j].IsDied) continue;
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

            if (minIdx == -1) return null;

            if (i == 0)
            {
                bool flag = false;
                for (int j = 0; j < units.Count; j++)
                {
                    if (turnTime[j] == -1) flag = true;
                }
                if (!flag) BattleTime += minValue;
            }

            if (minUnits == 1)
                for (int j = 0; j < units.Count; j++)
                {
                    turnTime[j] -= minValue;
                    if (turnTime[j] <= 0) turnTime[j] = TakeTurnValue / units[j].Status_Speed;
                }
            else
            {
                turnTime[minIdx] = -1;
            }

            // string aa = "[";
            // foreach (var item in turnTime)
            // {
            //     aa += $" {item},";
            // }
            // aa += "]";
            // print($"> {aa} : {i}");

            order[i] = minUnitId;
        }

        return order;
    }
}
// [ -1, 169.491525423729, 208.333333333333,] : 0
//[ 169.491525423729, 169.491525423729, 38.8418079096045,] : 0