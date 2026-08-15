using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CommandInvoker : MonoBehaviour
{
    private static Stack<ICommand> History = new();

    public static async UniTask ExecuteCommand(ICommand command)
    {
        History.Push(command);
        await command.Execute();
    }
}
