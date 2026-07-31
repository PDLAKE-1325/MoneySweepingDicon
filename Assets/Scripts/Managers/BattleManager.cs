using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(BattleUIManager))]
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public BattleUIManager UIManager { get; private set; }

    CancellationTokenSource _cancelSource = new();

    [SerializeField] Transform[] _playerPos;
    [SerializeField] Transform[] _enemyPos;
    [SerializeField] BattleUnit[] _playerUnit;
    [SerializeField] BattleUnit[] _enemyUnit;

#region Unity Methods
    void Awake()
    {
        Instance = this;
        UIManager = GetComponent<BattleUIManager>();
    }

    void OnDisable()
    {
        _cancelSource.Cancel();
        _cancelSource.Dispose();
    }
#endregion
#region Battle
    public void StartBattle()
    {
        BattleProcess().Forget();
    }

    async UniTask BattleProcess()
    {
        
    }
#endregion
}
