using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    const double TakeTurnValue = 10000;
    readonly Dictionary<int, double> _remaining = new();
    readonly Dictionary<int, int> _speeds = new();
    public double BattleTime { get; private set; }
    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }
    public void Init() { BattleTime = 0; _remaining.Clear(); _speeds.Clear(); }
    public float GetBattleTime() => (float)(BattleTime / 100d);

    public int[] GetTurnOrder(List<BattleUnit> units, bool isInit)
    {
        if (isInit) Init();
        BattleUnit[] alive = units.Where(unit => unit != null && !unit.IsDied).ToArray();
        if (alive.Length == 0) return null;
        foreach (BattleUnit unit in alive)
        {
            int speed = unit.Status_Speed;
            if (!_remaining.ContainsKey(unit.Id)) _remaining[unit.Id] = TakeTurnValue / speed;
            else if (_speeds[unit.Id] != speed) _remaining[unit.Id] *= (double)_speeds[unit.Id] / speed;
            _speeds[unit.Id] = speed;
        }
        int[] preview = PreviewTurnOrder(alive);
        Advance(alive, _remaining, out double elapsed);
        BattleTime += elapsed;
        return preview;
    }

    public int[] PreviewTurnOrder(IEnumerable<BattleUnit> units, int count = BattleManager.TurnOrderLength)
    {
        BattleUnit[] alive = units.Where(unit => unit != null && !unit.IsDied).ToArray();
        if (alive.Length == 0) return Array.Empty<int>();
        var times = new Dictionary<int, double>();
        foreach (BattleUnit unit in alive)
            times[unit.Id] = _remaining.TryGetValue(unit.Id, out double time)
                ? time * _speeds[unit.Id] / unit.Status_Speed : TakeTurnValue / unit.Status_Speed;
        int[] result = new int[Math.Max(0, count)];
        for (int i = 0; i < result.Length; i++) result[i] = Advance(alive, times, out _);
        return result;
    }

    static int Advance(BattleUnit[] units, Dictionary<int, double> times, out double elapsed)
    {
        BattleUnit actor = units.OrderBy(unit => times[unit.Id]).ThenBy(unit => unit.Id).First();
        elapsed = Math.Max(0, times[actor.Id]);
        foreach (BattleUnit unit in units) times[unit.Id] = Math.Max(0, times[unit.Id] - elapsed);
        times[actor.Id] = TakeTurnValue / actor.Status_Speed;
        return actor.Id;
    }
}
