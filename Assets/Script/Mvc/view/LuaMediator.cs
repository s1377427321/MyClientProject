using System.Collections.Generic;
using PureMVC.Interfaces;
using PureMVC.Patterns;
using SLua;
public class LuaMediator : Mediator
{
    private LuaTable table;
    public LuaMediator(string mediatorName, object viewComponent, LuaTable table)
        : base(mediatorName, viewComponent)
    {
        this.table = table;
    }
    public override void OnRegister()
    {
        if (table != null)
        {
            table.invoke("OnRegister", table);
        }
    }
    public object Call(string functionName, object arg)
    {
        if (table != null)
        {
            return table.invoke(functionName, table, arg);
        }
        return null;
    }
    public override void OnRemove()
    {
        if (table != null)
        {
            table.invoke("OnRemove", table);
        }
    }

    public override void HandleNotification(INotification notification)
    {
        if (table != null)
        {
            table.invoke("HandleNotification", table, notification);
        }
    }

    public override IList<string> ListNotificationInterests()
    {
        List<string> list = new List<string> { };
        if (table != null)
        {
            var arg = table.invoke("ListNotificationInterests", table);
            if (arg != null)
            {
                list = (List<string>)arg;
            }
        }
        return list;
    }
}
