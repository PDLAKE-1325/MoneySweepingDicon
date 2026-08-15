using UnityEngine;

public class DamageCalculator
{
    public static void GiveDamage(BattleUnit user, BattleUnit target, int damage, DamageType damageType)
    {
        int shield;
        switch (damageType)
        {
            case DamageType.Physics:
                shield = target.Status_AttackDefence;
                break;
            case DamageType.Magic:
                shield = target.Status_MagicDefence;
                break;
            default:
                shield = Mathf.RoundToInt((target.Status_AttackDefence + target.Status_MagicDefence) / 2f);
                break;
        }

        bool isCritial = Random.Range(0, 100) < user.Status_Critial;
        double Ddamage = damage * 100d / (100 + shield - user.Status_Penetration) * (isCritial ? (user.Status_CritialDamage / 100d) : 1);
        int finalDamage = Mathf.RoundToInt((float)Ddamage);

        target.GetDamage(finalDamage);
    }
}