using PureMVC.Patterns;
using SLua;
public class LuaProxy : Proxy
{
    LuaTable target = null;
    public LuaProxy(string name, object data, LuaTable target)
        : base(name, data)
    {
        this.target = target;
    }
    public object Call(string functionName, object arg)
    {
        if (target != null)
        {
            return target.invoke(functionName, target, arg);
        }
        return null;
    }
    public override void OnRegister()
    {
        if (target != null)
        {
            target.invoke("OnRegister", target);
        }
    }

    public override void OnRemove()
    {
        if (target != null)
        {
            target.invoke("OnRemove", target);
        }
    }
}
