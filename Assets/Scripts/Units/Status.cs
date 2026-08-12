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
    public string Name; // 이름
    public ActionType ActionType;// 뭔상황에 발동하냐
    public EffectType EffectType;  // 버프냐 디버프냐 추후 특수 생기면 뭐 그것도
    public Action<BattleUnit> ApplyEffect; // 이펙트 적용시
    public Action<BattleUnit> RemoveEffect; // 이펙트 꺼질때
    public bool Affected; // 한번만 발동하는것들
    public bool OverlapEffect; // 같은 효과에 겹쳐지냐
    public int AffectTurn; // 적용 턴
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
