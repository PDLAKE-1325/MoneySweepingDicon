using Unity.VisualScripting;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] BaseUnitData _baseUnitData;
    protected BattleUnitData _unitData;

    public string Info_Name => _baseUnitData.Name;
    [TextArea] public string Info_Description => _baseUnitData.Description;
    public UnitClass Info_Class => _baseUnitData.UnitClass;

    protected virtual void Awake()
    {
        InitializeUnit();
    }

    protected virtual void InitializeUnit()
    {
        _unitData = new BattleUnitData(_baseUnitData);
    }
}


[System.Serializable]
public class BattleUnitData
{
    public DamageType DamageType;
    public Status Status;
    public Status StatusModifier = new();
    public BattleUnitData(BaseUnitData data)
    {
        DamageType = data.DamageType;
        Status = data.Status;
    }
}

