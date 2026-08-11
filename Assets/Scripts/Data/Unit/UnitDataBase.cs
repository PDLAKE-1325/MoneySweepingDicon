using UnityEngine;

[CreateAssetMenu(fileName = "UnitDataBase", menuName = "Scriptable Objects/UnitDataBase")]
public class UnitDataBase : ScriptableObject
{
    [SerializeField] string _name;
    [SerializeField, TextArea] string _description;
    [SerializeField] UnitClass _unitClass;
    [SerializeField] DamageType _damageType;
    [SerializeField] Status _status;
    public string Name => _name;
    public string Description => _description;
    public UnitClass UnitClass => _unitClass;
    public DamageType DamageType => _damageType;
    public Status Status => _status;
}
