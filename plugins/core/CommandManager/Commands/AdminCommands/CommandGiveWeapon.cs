using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace RetakeExecutesPlugin;

public static class CommandGiveWeapon
{
    [ConsoleCommand("css_give", "Give yourself a weapon. Usage: !give <weapon_name>")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void OnGiveWeaponCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!GameGuards.IsAdmin(player.AuthorizedSteamID?.SteamId2))
        {
            info.ReplyToCommand("You do not have permission make this command.");
            return;
        }

        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        if (info.ArgCount < 2)
        {
            player.PrintToChat(
                " \x0C[Give]\x01 Usage: \x0C!give <weapon_name>\x01  e.g. \x0Cweapon_ak47\x01"
            );
            return;
        }

        var weaponName = info.GetArg(1).ToLower();

        if (!weaponName.StartsWith("weapon_"))
            weaponName = "weapon_" + weaponName;

        player.GiveNamedItem(weaponName);
        player.PrintToChat($" \x0C[Give]\x01 Gave you \x0C{weaponName}\x01.");
    }
}
