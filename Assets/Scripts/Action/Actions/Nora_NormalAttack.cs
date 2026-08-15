using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "Nora NormalAttack", menuName = "Scriptable Objects/BattleAction/Nora/NormalAttack")]
public class Nora_NormalAttack : BattleAction
{
    [SerializeField] DamageType _damageType;
    [SerializeField] MarkType _markType;

    public override async UniTask Act(int userId, int[] targetsId)
    {
        BattleUnit user = BattleManager.Instance.GetUnit(userId);

        await base.Act(userId, targetsId);

        for (int i = 0; i < targetsId.Length; i++)
        {
            BattleUnit target = BattleManager.Instance.GetUnit(targetsId[i]);

            int damage = 1 + target.GetMark(_markType);

            switch (_damageType)
            {
                case DamageType.Physics:
                    damage *= user.Status_AttackDamage;
                    break;
                case DamageType.Magic:
                    damage *= user.Status_MagicDamage;
                    break;
                default:
                    damage *= Mathf.RoundToInt((user.Status_AttackDamage + user.Status_MagicDamage) / 2f);
                    break;
            }

            DamageCalculator.GiveDamage(user, target, damage, _damageType);
        }
        await UniTask.WaitUntil(() => _endAnim == true);
    }
}
