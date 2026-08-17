using System;
using Cysharp.Threading.Tasks;
public class DelegateAction
{
    public static async UniTask Act(Action func, float delay = 0)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));
        func();
    }
}