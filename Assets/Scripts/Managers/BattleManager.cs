using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(BattleUIManager))]
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public BattleUIManager UIManager { get; private set; }

    public const int MaxPlayerUnits = 5;
    public const int MaxEnemyUnits = 5;
    CancellationTokenSource _cancelSource;

    [SerializeField] Transform[] _playerPos = new Transform[MaxPlayerUnits];
    [SerializeField] Transform[] _enemyPos = new Transform[MaxEnemyUnits];

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
        _cancelSource = new();
        BattleProcess(_cancelSource.Token).Forget();
    }

    public void ForceFinishBattle()
    {
        _cancelSource.Cancel();
        _cancelSource.Dispose();
        Debug.LogWarning("[전투 강제 종료됨]");
    }

    private void InitializeBattle()
    {

    }

    private void SetPlayerUnit()
    {

    }

    private void SetEnemyUnit()
    {

    }

    async UniTask BattleProcess(CancellationToken token)
    {
    }
    #endregion
}
// await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);

[System.Serializable]
public class BattleData
{
    public BattleUnit[] _playerUnit = new BattleUnit[BattleManager.MaxPlayerUnits];
    public BattleUnit[] _enemyUnit = new BattleUnit[BattleManager.MaxEnemyUnits];
    // public sound등으로 전투 배경 사운드 넣기.

    // public Action onVictory;
    // public Action onLoss;
}


// [System.Serializable]
// public class Sound
// {
//     public AudioClip clip;
//     public float volume = 1.0f;
//     public float pitch = 1.0f;

//     [HideInInspector] public bool foldout = false;
// }