using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] UnitDataBase _baseUnitData;
    protected UnitData _unitData;

    public UnitData UnitData => _unitData;

    protected virtual void Awake()
    {
        InitializeUnit();
    }

    protected virtual void InitializeUnit()
    {
        _unitData = new UnitData(_baseUnitData);
    }
}


[System.Serializable]
public class UnitData
{
    public string Name;
    [TextArea] public string Description;
    public UnitClass Class;
    public DamageType DamageType;
    public Status Status;
    public UnitData(UnitDataBase data)
    {
        Name = data.Name;
        Description = data.Description;
        Class = data.UnitClass;
        DamageType = data.DamageType;
        Status = data.Status;
    }
}