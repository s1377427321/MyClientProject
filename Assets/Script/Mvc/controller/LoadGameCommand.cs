using Client.UIFramework.UI;
using PureMVC.Interfaces;
using PureMVC.Patterns;

public class LoadGameCommand : SimpleCommand
{
    public override void Execute(INotification notification)
    {
        Page.ShowPage<UI.UILoadGame>(notification.Body);
    }
}
