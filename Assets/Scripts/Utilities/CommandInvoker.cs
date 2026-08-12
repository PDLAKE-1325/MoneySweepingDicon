using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CommandInvoker : MonoBehaviour
{
    private static Stack<ICommand> History = new();

    public static void ExecuteCommand(ICommand command)
    {
        command.Execute().Forget();
        History.Push(command);
    }
}
