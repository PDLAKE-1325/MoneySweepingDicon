using UnityEngine;

[CreateAssetMenu(fileName = "GameFlowSettings", menuName = "Scriptable Objects/Game Flow Settings")]
public class GameFlowSettings : ScriptableObject
{
    [Header("Scene paths (Build Settings)")]
    [SerializeField] string _mainScene = "Assets/Scenes/MainScene.unity";
    [SerializeField] string _readyScene = "Assets/Scenes/BattleReadyScene.unity";
    [SerializeField] string _mapScene = "Assets/Scenes/StageMapScene.unity";
    [SerializeField] string _battleScene = "Assets/Scenes/JangTest.unity";
    [SerializeField] string _resultScene = "Assets/Scenes/ResultScene.unity";
    [Header("Prototype stage")]
    [SerializeField] string _stageName = "온실 외곽 · 침투 작전";
    [SerializeField] BattleUnit[] _roster;
    [SerializeField] BattleUnit[] _enemies;
    public string MainScene => _mainScene;
    public string ReadyScene => _readyScene;
    public string MapScene => _mapScene;
    public string BattleScene => _battleScene;
    public string ResultScene => _resultScene;
    public string StageName => _stageName;
    public int RosterCount => _roster.Length;
    public BattleUnit GetRosterUnit(int index) => _roster[index];
    public BattleUnit[] CopyEnemies() => (BattleUnit[])_enemies.Clone();
}
