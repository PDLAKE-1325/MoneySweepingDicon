using System;

public class EffectSetter
{
    public static void SetEffect(BattleUnitEffect effect, BattleUnitEffect effectTemp, Action<BattleUnitEffect, BattleUnit> applyAction, Action<BattleUnitEffect, BattleUnit> removeAction, int userId)
    {
        effect.Name = effectTemp.Name;
        effect.UserId = userId;
        effect.Affectable = true;
        effect.ApplyActionType = effectTemp.ApplyActionType;
        effect.EffectType = effectTemp.EffectType;
        effect.ApplyEffectFunc = applyAction;
        effect.RemoveEffectFunc = removeAction;
        effect.DisappearWhenUserDied = effectTemp.DisappearWhenUserDied;
        effect.OverlapEffect = effectTemp.OverlapEffect;
        effect.AffectTurn = effectTemp.AffectTurn;
    }
}