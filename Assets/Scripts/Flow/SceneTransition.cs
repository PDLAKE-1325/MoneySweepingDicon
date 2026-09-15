using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public bool IsLoading { get; private set; }
    public event Action<string> LoadFailed;

    public bool Load(string scenePath)
    {
        if (IsLoading) return false;
        if (!Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError("Build Settings에 Scene이 없습니다: " + scenePath);
            LoadFailed?.Invoke(scenePath);
            return false;
        }
        IsLoading = true;
        LoadAsync(scenePath).Forget();
        return true;
    }

    async UniTask LoadAsync(string scenePath)
    {
        try { await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single).ToUniTask(cancellationToken: destroyCancellationToken); }
        catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
        catch (Exception exception) { Debug.LogException(exception); LoadFailed?.Invoke(scenePath); }
        finally { IsLoading = false; }
    }
}
