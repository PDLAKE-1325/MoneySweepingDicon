using UnityEngine;

[CreateAssetMenu(fileName = "UnitBaseData", menuName = "Scriptable Objects/UnitBaseData")]
public class UnitBaseData : ScriptableObject
{
    [SerializeField] UnitClass _unitClass;
    [SerializeField] DamageType _damageType;
    [SerializeField] Status _status;
    public UnitClass unitClass => _unitClass;
    public DamageType damageType => _damageType;
    public Status status => _status;
}
