using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "Nora Ultimate", menuName = "Scriptable Objects/BattleAction/Nora/Ultimate")]
public class Nora_Ultimate : BattleAction
{
    [SerializeField] BattleUnitEffect _markEffectTemp;
    [SerializeField] DamageType _damageType;
    [SerializeField] MarkType _markType;
    [SerializeField] float DamageMultiplier;
    [SerializeField] CutsceneAction _uiCutscene;

    public override async UniTask Act(int userId, int[] targetsId)
    {

        await base.Act(userId, targetsId);

        CutsceneAction cutscene = Instantiate(_uiCutscene, BattleManager.Instance.UI.UnitUICutsceneParent);
        await UniTask.WaitUntil(() => cutscene.IsAnimEnd);

        BattleUnit user = BattleManager.Instance.GetUnit(userId);
        for (int i = 0; i < targetsId.Length; i++)
        {
            BattleUnit target = BattleManager.Instance.GetUnit(targetsId[i]);

            float damage = target.GetMark(_markType) * DamageMultiplier;

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

            BattleUnitEffect effect = new();
            EffectSetter.SetEffect(effect, _markEffectTemp, AddMark, RemoveMark, userId);

            DamageCalculator.GiveDamage(user, target, Mathf.RoundToInt(damage), _damageType);
            target.AddEffect(effect);
        }
        await UniTask.WaitUntil(() => _endAnim == true);
    }

    private void AddMark(BattleUnitEffect effect, BattleUnit target)
    {
        effect.Affectable = false;
        target.AddMark(_markType, 1);
        Debug.Log("'저격' 표식 부여 > " + target.GetMark(_markType));
    }

    private void RemoveMark(BattleUnitEffect effect, BattleUnit target)
    {
        target.AddMark(_markType, -1);
        Debug.Log("'저격' 표식 제거 > " + target.GetMark(_markType));
    }
}
