using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public static class CommandCallAdmin
{
    public static void Execute(CCSPlayerController? player, CommandInfo command)
    {
        command.ReplyToCommand(
            $" {ChatColors.Green} [MACROS]: >>> {ChatColors.White} Visit {ChatColors.Green} https://forms.gle/w8VwTzrG5G33Tz7Y9 {ChatColors.White} and report a bug. A proper system is coming soon."
        );
    }
}
