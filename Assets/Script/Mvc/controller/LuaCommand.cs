using LuaMgr;
using PureMVC.Interfaces;
using PureMVC.Patterns;

public class LuaCommand : SimpleCommand
{
    public override void Execute(INotification notification)
    {
        LuaManager.getInstance().CallLuaFunction("RegisteredCommand.Execute", notification);
    }
}
