using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public static class ServerHelper
{
    public static bool IsPracMode { get; private set; } = false;

    public static void SetPracMode(bool value)
    {
        IsPracMode = value;
    }

    private static readonly string ConfigDirectory = Path.Combine(
        Server.GameDirectory,
        "csgo",
        "cfg",
        "cs2-retake-executes"
    );
    private static readonly string PracticeCfgPath = Path.Combine(
        ConfigDirectory,
        "cs2-practice-mode.cfg"
    );

    private static readonly string RetakeExecuteCfgPath = Path.Combine(
        ConfigDirectory,
        "cs2-retake-execute-mode.cfg"
    );

    public static void ExecutePracticeConfig()
    {
        if (!File.Exists(PracticeCfgPath))
        {
            BuildPracticeConfig();
        }

        Server.ExecuteCommand("exec cs2-retake-executes/cs2-practice-mode.cfg");

        Server.PrintToChatAll(
            $" {ChatColors.Blue} [MACROS]: >>> {ChatColors.White} Practice Mode Loaded"
        );

        SetPracMode(true);
    }

    public static void ExecuteRetakeExecuteMode(bool overide = false)
    {
        if (!File.Exists(RetakeExecuteCfgPath) || overide)
        {
            BuildRetakeExecuteConfig(overide);
        }

        Server.ExecuteCommand("exec cs2-retake-executes/cs2-retake-execute-mode.cfg");
        Server.PrintToChatAll(
            $" {ChatColors.Gold} [MACROS]: >>> {ChatColors.White} Retake Executes Mode Loaded"
        );
        SetPracMode(false);
    }

    private static void BuildPracticeConfig()
    {
        Directory.CreateDirectory(ConfigDirectory);
        var practiceFile = File.Create(PracticeCfgPath);

        var contents =
            @"
        sv_cheats 1;
        mp_limitteams 0;
        mp_autoteambalance 0;
        mp_maxmoney 60000;
        mp_startmoney 60000;
        mp_buytime 9999;
        mp_buy_anywhere 1;
        mp_freezetime 0;
        mp_roundtime_defuse 60;
        mp_respawn_on_death_ct 1;
        mp_respawn_on_death_t 1;
        sv_infinite_ammo 1;
        mp_roundtime_defuse 60;
        sv_grenade_trajectory 1;
        sv_grenade_trajectory_prac_pipreview true;
        sv_grenade_trajectory_prac_trailtime 15;
        sv_grenade_trajectory_time_spectator 15;
        sv_grenade_trajectory_time 15;
        sv_showimpacts 1;
        sv_showimpacts_time 10;
        ammo_grenade_limit_total 5;
        sv_disable_teamselect_menu 0;
        mp_warmup_end;
        bot_kick;
        mp_restartgame 1        
        ";

        var practiceConfigFile = Encoding.UTF8.GetBytes(contents);
        practiceFile.Write(practiceConfigFile, 0, practiceConfigFile.Length);
        practiceFile.Close();
    }

    private static void BuildRetakeExecuteConfig(bool overide = false)
    {
        if (overide)
        {
            File.Delete(RetakeExecuteCfgPath);
        }

        Directory.CreateDirectory(ConfigDirectory);
        var retakeExecuteFile = File.Create(RetakeExecuteCfgPath);

        var contents =
            @"
                // Things you shouldn't change:
                bot_kick
                bot_quota 0
                mp_autoteambalance 0
                mp_forcecamera 1
                mp_give_player_c4 1
                mp_halftime 0
                mp_ignore_round_win_conditions 0
                mp_join_grace_time 0
                mp_match_can_clinch 0
                mp_maxmoney 0
                sv_disable_teamselect_menu
                mp_playercashawards 0
                mp_respawn_on_death_ct 0
                mp_respawn_on_death_t 0
                mp_solid_teammates 1
                mp_teamcashawards 0
                mp_warmup_pausetimer 0
                sv_skirmish_id 0

                // Reset practice mode settings:
                sv_cheats 0
                sv_infinite_ammo 0
                sv_grenade_trajectory 0
                sv_grenade_trajectory_prac_pipreview false
                sv_grenade_trajectory_prac_trailtime 0
                sv_grenade_trajectory_time_spectator 0
                sv_grenade_trajectory_time 0
                sv_showimpacts 0
                sv_showimpacts_time 0
                ammo_grenade_limit_total 3
                mp_buy_anywhere 0
                mp_limitteams 2
                mp_buytime 20
                mp_startmoney 0
                mp_maxmoney 0

                // Things you can change, and may want to:
                mp_roundtime_defuse 1.167
                mp_autokick 0
                mp_c4timer 40
                mp_freezetime 1
                mp_friendlyfire 0
                mp_round_restart_delay 2
                sv_talk_enemy_dead 0
                sv_talk_enemy_living 0
                sv_deadtalk 1
                spec_replay_enable 0
                mp_maxrounds 20
                mp_match_end_restart 0
                mp_timelimit 0
                mp_match_restart_delay 10
                mp_death_drop_gun 1
                mp_death_drop_defuser 1
                mp_death_drop_grenade 1
                mp_shorthanded_cash_award 0
                mp_shorthanded_cash_award_max_count 0
                mp_warmuptime 5
                mp_restartgame 1   
";

        var retakeExecuteConfigFile = Encoding.UTF8.GetBytes(contents);
        retakeExecuteFile.Write(retakeExecuteConfigFile, 0, retakeExecuteConfigFile.Length);
        retakeExecuteFile.Close();
    }
}
