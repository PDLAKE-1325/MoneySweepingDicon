using UnityEngine;

public abstract class BattleEntityBase : MonoBehaviour
{
    public UnitClass UnitClass { get; protected set; }
    public DamageType DamageType { get; protected set; }
    public Status Status { get; protected set; }
    public UnitTeam Team { get; protected set; }

    // public void Initialize(UnitClass type, DamageType damageType, Status status, UnitTeam team)
    // {
    //     UnitClass = type;
    //     DamageType = damageType;
    //     Status = status;
    //     Team = team;
    // }
}
