using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SceneFlowSmokeTests
{
    const string Pending = "SceneFlowSmokeTests.Pending";
    static readonly List<string> Checks = new();
    static bool _running;
    static SceneFlowSmokeTests()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                Run().Forget();
            }
        };
    }

    [MenuItem("Tools/Game/Run Scene Flow Tests")]
    public static void Start()
    {
        if (_running || EditorApplication.isPlaying) return;
        if (SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save the current scene before testing.");
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.Show(); view.Focus();
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Checks.Add(name);
        Debug.Log("[SceneFlow] PASS: " + name);
    }
    static async UniTask Until(Func<bool> condition, int seconds = 20)
    {
        using var timeout = new CancellationTokenSource();
        using var timer = timeout.CancelAfterSlim(TimeSpan.FromSeconds(seconds), DelayType.UnscaledDeltaTime);
        await UniTask.WaitUntil(condition, cancellationToken: timeout.Token);
    }
    static async UniTask Scene(string name)
    {
        await Until(() => SceneManager.GetActiveScene().name == name && GameSession.Instance != null && !GameSession.Instance.Scenes.IsLoading);
        await UniTask.DelayFrame(3);
        Check(SceneManager.sceneCount == 1, name + " is a real single-loaded scene");
        Check(UnityEngine.Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length == 1, "one persistent session");
    }
    static T UI<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Click(string name)
    {
        Button button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name);
        Check(button.interactable && button.onClick.GetPersistentEventCount() > 0, name + " has Inspector button wiring");
        button.onClick.Invoke();
    }
    static async UniTask Capture(string name)
    {
        await UniTask.DelayFrame(2);
        await UniTask.WaitForEndOfFrame(GameSession.Instance);
        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        Directory.CreateDirectory("Logs/SceneFlowValidation");
        File.WriteAllBytes("Logs/SceneFlowValidation/" + name + ".png", shot.EncodeToPNG());
        UnityEngine.Object.Destroy(shot);
    }
    static async UniTask Win()
    {
        await Until(() => BattleManager.Instance != null && BattleManager.Instance.IsBattleInProgress);
        foreach (BattleUnit enemy in BattleManager.Instance.EnemyUnits)
            if (enemy != null) enemy.GetDamage(enemy.Status_Hp - 1);
        float deadline = Time.realtimeSinceStartup + 20;
        while (BattleManager.Instance != null && BattleManager.Instance.IsBattleInProgress)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("real attack victory");
            foreach (BattleUnit player in BattleManager.Instance.PlayerUnits)
                if (player is Nora nora) nora.RequestAction(TurnActionType.NormalAttack);
            if (TargetManager.Instance.IsSelecting)
            {
                BattleUnit enemy = BattleManager.Instance.EnemyUnits.FirstOrDefault(unit => unit != null && !unit.IsDied);
                if (enemy != null) TargetManager.Instance.TargetClicked(enemy);
            }
            await UniTask.Yield();
        }
    }
    static async UniTask Deploy()
    {
        Click("PrepareButton"); await Scene("BattleReadyScene");
        Click("DeployButton"); await Scene("StageMapScene");
    }
    static async UniTask Run()
    {
        _running = true;
        Checks.Clear();
        var errors = new List<string>();
        string failure = null;
        bool previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception) errors.Add(message + "\n" + stack); }
        Application.logMessageReceived += Log;
        try
        {
            await Scene("MainScene"); await Capture("main");
            Click("PrepareButton"); await Scene("BattleReadyScene"); await Capture("ready");
            UI<BattleReadySceneUI>().ToggleUnit(0);
            Check(GameSession.Instance.SelectedCount == 0 && !GameSession.Instance.BeginStage(), "empty formation rejected");
            UI<BattleReadySceneUI>().ToggleUnit(0);
            Click("DeployButton"); await Scene("StageMapScene"); await Capture("map");
            StageSession first = GameSession.Instance.Run;
            int settlements = 0;
            GameSession.Instance.StageSettled += _ => settlements++;
            Check(!first.Enter(1) && !GameSession.Instance.ToggleUnit(0), "final room and loadout locked");
            BattleData copy = first.CreateBattle(); copy.PlayerUnits[0] = null;
            Check(first.CreateBattle().PlayerUnits[0] != null, "formation snapshot protected");
            Click("NormalRoomButton"); await Scene("JangTest"); await Capture("battle");
            BattleUnit player = BattleManager.Instance.PlayerUnits[0];
            int removed = 0;
            player.AddEffect(new BattleUnitEffect { Name = "A", AffectTurn = 1, RemoveEffectFunc = (_, __) => removed++ },
                new BattleUnitEffect { Name = "B", AffectTurn = 1, RemoveEffectFunc = (_, __) => removed++ });
            player.OnTurnEnd();
            Check(removed == 2 && player.Effects.Count == 0, "simultaneous effects expire");
            var units = BattleManager.Instance.AllUnits.Where(u => u != null).ToArray();
            double time = TurnManager.Instance.BattleTime;
            Check(TurnManager.Instance.PreviewTurnOrder(units).SequenceEqual(TurnManager.Instance.PreviewTurnOrder(units)) &&
                time == TurnManager.Instance.BattleTime, "AV preview is read-only");
            await Win(); await Scene("StageMapScene");
            Check(first.CompletedRooms == 1 && !first.Enter(0), "normal victory unlocks final only");
            Check(BattleManager.Instance == null && TargetManager.Instance == null, "battle managers unloaded");
            Click("FinalRoomButton"); await Scene("JangTest");
            await Win(); await Scene("ResultScene"); await Capture("result");
            Check(first.Outcome == StageOutcome.Cleared && settlements == 1 && first.SettlementCount == 1 &&
                !first.CompleteBattle(BattleOutcome.Victory), "clear settles once");
            Click("ReturnMainButton"); await Scene("MainScene");
            Check(GameSession.Instance.Run == null && GameSession.Instance.SelectedCount == 1, "run cleared, loadout retained");

            await Deploy();
            Check(GameSession.Instance.Run.Id != first.Id && GameSession.Instance.Run.CompletedRooms == 0, "fresh deployment");
            Click("NormalRoomButton"); await Scene("JangTest");
            Click("AbandonBattleButton"); await Scene("ResultScene");
            Check(GameSession.Instance.Run.Outcome == StageOutcome.Abandoned, "abort animation then unload scene");
            Click("ReturnMainButton"); await Scene("MainScene");

            await Deploy(); Click("NormalRoomButton"); await Scene("JangTest");
            await Until(() => BattleManager.Instance.curTurnUnitId() == 0);
            var nora = (Nora)BattleManager.Instance.PlayerUnits[0];
            await Until(() => nora.RequestAction(TurnActionType.NormalAttack));
            await Until(() => TargetManager.Instance.IsSelecting);
            TargetManager.Instance.ResetSelection();
            await UniTask.DelayFrame(3);
            Check(BattleManager.Instance.IsBattleInProgress, "target cancel preserves turn");
            await Until(() => nora.RequestAction(TurnActionType.NormalAttack));
            await Until(() => TargetManager.Instance.IsSelecting);
            Click("AbandonBattleButton"); await Scene("ResultScene");
            Check(GameSession.Instance.Run.Outcome == StageOutcome.Abandoned, "abort target wait then unload scene");
            Click("ReturnMainButton"); await Scene("MainScene");

            await Deploy(); Click("NormalRoomButton"); await Scene("JangTest");
            BattleUnit victim = BattleManager.Instance.PlayerUnits[0];
            bool diedVisible = false;
            Action death = () => diedVisible = victim.IsDied;
            BattleManager.Instance.OnSomeoneDied += death;
            victim.GetDamage(victim.Status_Hp);
            BattleManager.Instance.OnSomeoneDied -= death;
            Check(diedVisible, "death state precedes event");
            await Scene("ResultScene");
            Check(GameSession.Instance.Run.Outcome == StageOutcome.Defeated, "defeat result scene");
            Click("ReturnMainButton"); await Scene("MainScene");

            await Deploy(); Click("NormalRoomButton"); await Scene("JangTest");
            await Until(() => BattleManager.Instance.curTurnUnitId() == 0);
            var ultimateUser = (Nora)BattleManager.Instance.PlayerUnits[0];
            await Until(() => ultimateUser.RequestAction(TurnActionType.Ultimate));
            await Until(() => TargetManager.Instance.IsSelecting);
            TargetManager.Instance.TargetClicked(BattleManager.Instance.EnemyUnits[0]);
            await Until(() => UI<CutsceneAction>() != null);
            await Until(() => BattleManager.Instance.EnemyUnits[0].GetMark(MarkType.Sniping) > 0);
            await UniTask.DelayFrame(2);
            Check(UnityEngine.Object.FindObjectsByType<CutsceneAction>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "ultimate uses existing cutscene and destroys it on completion");
            Click("AbandonBattleButton"); await Scene("ResultScene");
            Click("ReturnMainButton"); await Scene("MainScene");

            await Deploy(); Click("AbandonStageButton"); await Scene("ResultScene");
            Check(GameSession.Instance.Run.Outcome == StageOutcome.Abandoned, "abandon map");
            Click("ReturnMainButton"); await Scene("MainScene");
            await UniTask.Delay(TimeSpan.FromSeconds(31), DelayType.UnscaledDeltaTime);
            Check(errors.Count == 0, "no runtime errors or late timeout callbacks");
        }
        catch (Exception exception) { failure = exception.ToString(); }
        finally
        {
            Application.logMessageReceived -= Log;
            Application.runInBackground = previousBackground;
            Directory.CreateDirectory("Logs/SceneFlowValidation");
            File.WriteAllText("Logs/SceneFlowValidation/results.json", JsonUtility.ToJson(new Report
            { success = failure == null, checks = Checks.ToArray(), error = failure, runtimeErrors = errors.ToArray(), utc = DateTime.UtcNow.ToString("O") }, true));
            Debug.Log("[SceneFlow] " + (failure == null ? "ALL PASSED" : failure));
            _running = false;
        }
    }
    [Serializable] class Report { public bool success; public string[] checks; public string error; public string[] runtimeErrors; public string utc; }
}
