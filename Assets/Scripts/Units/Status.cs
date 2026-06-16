using System;
using UnityEngine;

[Serializable]
public class Status
{
    public int MaxHp; // 체력
    public int AttackDamage; // 물공
    public int MagicDamage; // 마공
    public int Penetration; // 관통력
    public int Speed; // 민첩
    public int Critial; // 치명타 확률 (0~1)
    public int CritialDamage; // 치명타 공격력 배율 (곱연산)
}