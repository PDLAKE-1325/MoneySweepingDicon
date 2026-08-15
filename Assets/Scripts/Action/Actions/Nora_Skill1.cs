using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "Nora Skill 1", menuName = "Scriptable Objects/BattleAction/Nora/Skill_1")]
public class Nora_Skill1 : BattleAction
{
    [SerializeField] BattleUnitEffect _markEffectTemp;
    [SerializeField] MarkType _markType;

    public override async UniTask Act(int userId, int[] targetsId)
    {
        for (int i = 0; i < targetsId.Length; i++)
        {
            BattleUnit target = BattleManager.Instance.GetUnit(targetsId[i]);

            BattleUnitEffect effect = new();
            EffectSetter.SetEffect(effect, _markEffectTemp, AddMark, RemoveMark, userId);

            target.AddEffect(effect);
        }
    }

    private void AddMark(BattleUnitEffect effect, BattleUnit target)
    {
        effect.Affectable = false;
        target.AddMark(_markType, 1);
    }

    private void RemoveMark(BattleUnitEffect effect, BattleUnit target)
    {
        target.AddMark(_markType, -1);
    }
}
