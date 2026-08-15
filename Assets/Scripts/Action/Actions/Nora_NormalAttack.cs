using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[CreateAssetMenu(fileName = "Nora NormalAttack", menuName = "Scriptable Objects/BattleAction/Nora/NormalAttack")]
public class Nora_NormalAttack : BattleAction
{
    [SerializeField] DamageType _damageType;
    [SerializeField] MarkType _markType;

    [SerializeField] GameObject _vfxPrefab;
    [SerializeField] Vector3 _vfxPosition;

    public override async UniTask Act(int userId, int[] targetsId)
    {
        BattleUnit user = BattleManager.Instance.GetUnit(userId);

        await base.Act(userId, targetsId);

        for (int i = 0; i < targetsId.Length; i++)
        {
            BattleUnit target = BattleManager.Instance.GetUnit(targetsId[i]);

            int damage = 1;
            if (target.Marks.ContainsKey(_markType))
                damage += target.Marks[_markType];

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

    protected override void PlayVFX(BattleUnit unit)
    {
        if (_vfxPrefab == null) return;
        GameObject vfx = Instantiate(_vfxPrefab, unit.VfxParent);
        vfx.transform.localPosition = _vfxPosition;
    }
}
