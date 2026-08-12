using Cysharp.Threading.Tasks;

public interface ICommand
{
    public CommandInfo Info { get; }
    public UniTask Execute();
}

[System.Serializable]
public class CommandInfo
{
    public string Name;
    public string User;
    public string Target;
    public string Note;

    public CommandInfo(string name, string user, string target, string note = "")
    {
        Name = name;
        User = user;
        Target = target;
        Note = note;
    }
}