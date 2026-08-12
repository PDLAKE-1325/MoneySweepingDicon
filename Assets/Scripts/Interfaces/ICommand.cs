using Cysharp.Threading.Tasks;

public interface ICommand
{
    public UniTask Execute();
}