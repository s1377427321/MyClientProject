using UnityEngine;
using System.Collections;
using PureMVC.Patterns;
public class AppFacade : Facade
{
    public static AppFacade getInstance()
    {
        if (m_instance == null)
        {
            m_instance = new AppFacade();
        }
        return m_instance as AppFacade;
    }
    public AppFacade()
    {
        m_view.RemoveAllMediator();
        m_controller.RemoveAllCommand();
        m_model.RemoveAllProxy();
        InitializeFacade();
    }

    protected override void InitializeController()
    {
        base.InitializeController();
    }
    static public void Destroy()
    {
        m_instance = null;
    }
}
