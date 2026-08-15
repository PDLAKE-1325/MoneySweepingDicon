using UnityEngine;

[CreateAssetMenu(fileName = "BaseUnitData", menuName = "Scriptable Objects/BaseUnitData")]
public class BaseUnitData : ScriptableObject
{
    [SerializeField] string _name;
    [SerializeField, TextArea] string _description;
    [SerializeField] UnitClass _unitClass;
    [SerializeField] Status _status;
    public string Name => _name;
    public string Description => _description;
    public UnitClass UnitClass => _unitClass;
    public Status Status => _status;
}
