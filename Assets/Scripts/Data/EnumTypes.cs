using UnityEngine;

public enum UnitRole
{
    Defender,
    Attacker,
    Sniper,
    Controller,
    Healer,
}

public enum GameEventState
{
    OnTurnCatched,
    BeforeAttack,
    AfterAttack,
    BeforeSkill,
    AfterSkill,
    OnAvoidState,
    OnAvoidSucceed,
    OnDead,
    OnKill,
    OnParry,
    OnRest,
    OnTurnOver,
}