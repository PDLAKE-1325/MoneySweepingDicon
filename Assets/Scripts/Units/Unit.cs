using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    [SerializeField] UnitData _data;
    public UnitData Data => _data;
}
