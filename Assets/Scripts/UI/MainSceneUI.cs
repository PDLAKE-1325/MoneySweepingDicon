using UnityEngine;
using UnityEngine.UI;

public class MainSceneUI : MonoBehaviour
{
    [SerializeField] Button _prepareButton;
    GameSession _session;
    void Start() { _session = GameSession.Instance; }
    public void OpenReady()
    {
        if (_session == null || _session.Scenes.IsLoading) return;
        if (_session.Scenes.Load(_session.Settings.ReadyScene)) _prepareButton.interactable = false;
    }
}
