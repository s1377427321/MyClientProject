using UnityEngine;
using System.Collections;
using PureMVC.Patterns;
using PureMVC.Interfaces;
using LuaMgr;

public class BootstrapCommands : SimpleCommand
{
    public override void Execute(INotification notification)
    {
        base.Execute(notification);
        LuaManager.getInstance().CallLuaFunction(notification.Body + "Launcher._registerCommand", notification);
    }
}
