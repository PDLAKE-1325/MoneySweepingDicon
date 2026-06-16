using UnityEngine;

[CreateAssetMenu(fileName = "UnitBaseData", menuName = "Scriptable Objects/UnitBaseData")]
public class UnitBaseData : ScriptableObject
{
    [SerializeField] string _name;
    [SerializeField] string _description;
    [SerializeField] UnitClass _unitClass;
    [SerializeField] DamageType _damageType;
    [SerializeField] Status _status;
    public string Name => _name;
    public string Description => _description;
    public UnitClass UnitClass => _unitClass;
    public DamageType DamageType => _damageType;
    public Status Status => _status;
}
