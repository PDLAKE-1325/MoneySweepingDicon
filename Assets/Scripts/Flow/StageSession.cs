using System;
using System.Linq;

public enum StageOutcome { Cleared, Defeated, Abandoned, Error }

public sealed class StageSession
{
    public const int RoomCount = 2;
    readonly BattleUnit[] _players;
    readonly BattleUnit[] _enemies;
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public int CompletedRooms { get; private set; }
    public int ActiveRoom { get; private set; } = -1;
    public StageOutcome? Outcome { get; private set; }
    public int SettlementCount { get; private set; }
    public bool IsSettled => Outcome.HasValue;

    public StageSession(BattleUnit[] players, BattleUnit[] enemies)
    {
        if (players == null || enemies == null || !players.Any(unit => unit != null) || !enemies.Any(unit => unit != null))
            throw new ArgumentException("아군과 적 편성이 필요합니다.");
        _players = players.Where(unit => unit != null).ToArray();
        _enemies = enemies.Where(unit => unit != null).ToArray();
    }

    public bool CanEnter(int room) => !IsSettled && ActiveRoom < 0 && room == CompletedRooms && room < RoomCount;
    public bool Enter(int room)
    {
        if (!CanEnter(room)) return false;
        ActiveRoom = room;
        return true;
    }
    public BattleData CreateBattle() => new BattleData
    {
        PlayerUnits = (BattleUnit[])_players.Clone(), EnemyUnits = (BattleUnit[])_enemies.Clone()
    };

    public bool CompleteBattle(BattleOutcome outcome)
    {
        if (IsSettled || ActiveRoom < 0) return false;
        ActiveRoom = -1;
        if (outcome == BattleOutcome.Victory)
        {
            CompletedRooms++;
            if (CompletedRooms == RoomCount) Settle(StageOutcome.Cleared);
        }
        else Settle(outcome == BattleOutcome.Defeat ? StageOutcome.Defeated
            : outcome == BattleOutcome.Abandoned ? StageOutcome.Abandoned : StageOutcome.Error);
        return true;
    }

    public bool Settle(StageOutcome outcome)
    {
        if (IsSettled) return false;
        Outcome = outcome;
        ActiveRoom = -1;
        SettlementCount++;
        return true;
    }
}
