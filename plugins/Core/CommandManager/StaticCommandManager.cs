namespace RetakeExecutesPlugin;

public class StaticCommandManager(RetakeExecutes _plugin)
{
    private readonly RetakeExecutes plugin = _plugin;

    public void RegisterCommands()
    {
        plugin.AddCommand("css_getpos", "Get position of the user", CommandGetPosition.Execute);
        plugin.AddCommand(
            "css_restart",
            "Restart the game",
            CommandVariousGameCommands.CommandRestartGame
        );
        plugin.AddCommand(
            "css_wstart",
            "Extend and start warmup",
            CommandVariousGameCommands.CommandExtendWarmup
        );
        plugin.AddCommand("css_wend", "End warmup", CommandVariousGameCommands.CommandEndWarmup);
        plugin.AddCommand(
            "css_rs",
            "Record last smoke trajectory",
            CommandRecordLastThrownSmoke.Execute
        );
        plugin.AddCommand(
            "css_fb",
            "Record last flashbang trajectory",
            CommandRecordLastThrownFlashbang.Execute
        );
        plugin.AddCommand("css_prac", "Execute practice config", CommandLoadPracticeMode.Execute);
        plugin.AddCommand(
            "css_re",
            "Execute starting retake execute config",
            CommandLoadExecuteMode.Execute
        );

        plugin.AddCommand("css_t", "Add bot to t side", CommandAddBot.ExecuteT);
        plugin.AddCommand("css_ct", "Add bot to ct side", CommandAddBot.ExecuteCT);
        plugin.AddCommand("css_cm", "Change Map", CommandChangeMap.Execute);
        plugin.AddCommand("css_give", "give weapon", CommandGiveWeapon.OnGiveWeaponCommand);
        plugin.AddCommand("css_calladmin", "call admin", CommandCallAdmin.Execute);
        plugin.AddCommand("css_reportbug", "help", CommandCallAdmin.Execute);
        plugin.AddCommand("css_report", "help", CommandCallAdmin.Execute);
        plugin.AddCommand("css_help", "help", CommandCallAdmin.Execute);
        plugin.AddCommand("css_asay", "admin say", CommandAdminChat.OnAdminSay);
    }
}
