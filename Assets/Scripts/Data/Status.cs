using System;
using UnityEngine;

[Serializable]
public class Status
{
    [Range(0, 1000)] public int MaxHp; // 체력
    [Range(0, 100)] public int AttackDamage; // 물공
    [Range(0, 100)] public int MagicDamage; // 마공
    [Range(0, 100)] public int AttackDefence; // 물공
    [Range(0, 100)] public int MagicDefence; // 마공
    [Range(0, 100)] public int Penetration; // 관통력
    [Range(0, 100)] public int Speed; // 민첩
    [Range(0, 1f)] public int Critial; // 치명타 확률 (0~1)
    [Range(0, 10f)] public int CritialDamage; // 치명타 공격력 배율 (곱연산)
}