using Cysharp.Threading.Tasks;

public interface ICommand
{
    public CommandInfo Info { get; }
    public UniTask Execute();
}

[System.Serializable]
public class CommandInfo
{
    public string Name { get; private set; }
    public string User { get; private set; }
    public string[] Targets { get; private set; }
    public string Note { get; private set; }

    public CommandInfo(string name, string user, string[] targets, string note = "")
    {
        Name = name;
        User = user;
        Targets = targets;
        Note = note;
    }
}