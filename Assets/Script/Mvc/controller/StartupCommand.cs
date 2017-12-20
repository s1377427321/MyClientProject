using UnityEngine;
using System.Collections;
using PureMVC.Patterns;
using PureMVC.Interfaces;

public class StartupCommand : MacroCommand
{
    protected override void InitializeMacroCommand()
    {
        base.InitializeMacroCommand();
        //BootstrapCommands
        AddSubCommand(typeof(BootstrapCommands));
        //BootstrapModels
        AddSubCommand(typeof(BootstrapModels));
        //BootstrapViewMediators
        AddSubCommand(typeof(BootstrapViewMediators));
    }

}
