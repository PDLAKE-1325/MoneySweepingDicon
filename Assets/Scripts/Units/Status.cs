using System;
using UnityEngine;

[Serializable]
public class Status
{
    [Range(1, 10000)] public int MaxHp; // 체력
    [Range(0, 1000)] public int AttackDamage; // 물공
    [Range(0, 1000)] public int MagicDamage; // 마공
    [Range(0, 1000)] public int AttackDefence; // 물방
    [Range(0, 1000)] public int MagicDefence; // 마방
    [Range(0, 1000)] public int Penetration; // 관통력
    [Range(1, 1000)] public int Speed; // 민첩
    [Range(0, 100)] public int Critial; // 치명타 확률 %
    [Range(100, 1000)] public int CritialDamage; // 치명타 공격력 배율 % (곱연산)
}

[Serializable]
public class BattleUnitEffect
{
    public ActionType ActionType;
    public EffectType EffectType;
    public Action<BattleUnit> ApplyEffect;
    public Action<BattleUnit> RemoveEffect;
    public bool Affected;
    public int AffectTurn;
} // 마킹도 이걸로 처리 ㄱㄱ

public enum EffectType
{
    Positive,
    Negative,
}

public enum MarkType
{
    TargetLockOn,
}

public enum ActionType
{
    Once,
    OnTurnStart,
    OnTurnEnd,
    OnKill,
    Ondead,
    OnRevive,
    OnBlock,
    OnBlocked,
    OnAvoid,
    BeforeNormalAttack,
    BeforeNormalDamage,
    AfterNormalAttack,
    AfterNormalDamage,
    BeforeSkillAttack,
    BeforeSkillDamage,
    AfterSkillAttack,
    AfterSkillDamage,
    BeforeUltimateAttack,
    BeforeUltimatelDamage,
    AfterUltimateAttack,
    AfterUltimateDamage,
}
