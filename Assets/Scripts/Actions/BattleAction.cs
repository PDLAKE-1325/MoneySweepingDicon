using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class BattleAction : ScriptableObject
{
    [SerializeField] string _name;
    public string Name => _name;

    public abstract UniTask Execute(BattleUnit unit);
}

public enum TargetType
{
    
}