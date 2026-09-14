using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakeExecutesPlugin.Memory;

namespace RetakeExecutesPlugin;

public class UtilService(BasePlugin _plugin, GameState _gameState, MapConfiguration _mapConfig)
{
    private readonly BasePlugin plugin = _plugin;
    private readonly GameState gameState = _gameState;
    private readonly MapConfiguration mapConfig = _mapConfig;

    public void ThrowRoundUtility()
    {
        var currentRound =
            gameState.CurrentRoundStrategy
            ?? throw new Exception("Current round in play not found. Cannot throw utility.");

        RegisterSmokes(currentRound);
        RegisterFlashes(currentRound);
    }

    public void RethrowRoundUtility(int roundId)
    {
        var round = mapConfig.GetRoundById(roundId);

        if (round is null)
        {
            Server.PrintToChatAll($"No round found with id {roundId}. Check config");
            return;
        }

        Server.PrintToChatAll($"Throwing {roundId}: {round.RoundStrategy} smokes.");

        RegisterSmokes(round);
        RegisterFlashes(round);
    }

    private void RegisterSmokes(Round round)
    {
        var delayedSmokes = round.Smokes.Where(s => s.Delay != 0);

        var regularSmokes = round.Smokes.Except(delayedSmokes);

        foreach (var smoke in regularSmokes)
        {
            ThrowSmoke(smoke);
        }

        foreach (var delayedSmoke in delayedSmokes)
        {
            Log.Info($"Registering {delayedSmoke.Name}: Delay is {delayedSmoke.Delay}s");
            plugin.AddTimer(delayedSmoke.Delay, () => ThrowSmoke(delayedSmoke));
        }
    }

    private void RegisterFlashes(Round round)
    {
        var delayedFlashes = round.Flashes.Where(s => s.Delay != 0);
        var regularFlashes = round.Flashes.Except(delayedFlashes);

        foreach (var smoke in regularFlashes)
        {
            ThrowFlash(smoke);
        }

        foreach (var delayedFlash in delayedFlashes)
        {
            Log.Info($"Registering {delayedFlash.Name}: Delay is {delayedFlash.Delay}s");
            plugin.AddTimer(
                delayedFlash.Delay,
                () =>
                {
                    ThrowFlash(delayedFlash);
                    // Server.PrintToChatAll("Throwing flash!");
                }
            );
        }
    }

    private static void ThrowSmoke(Projectile smoke)
    {
        var projectile = GrenadeHelper.CSmokeGrenadeProjectile_CreateFunc.Invoke(
            smoke.Origin.Handle,
            smoke.Angle.Handle,
            smoke.Velocity.Handle,
            smoke.Velocity.Handle,
            IntPtr.Zero,
            45,
            (int)CsTeam.Terrorist
        );

        if (projectile == null || !projectile.IsValid)
        {
            Log.Warn("Failed to create smoke projectile", smoke.Name);
        }
    }

    private static void ThrowFlash(Projectile flash)
    {
        {
            var grenadeEntity = Utilities.CreateEntityByName<CFlashbangProjectile>(
                "flashbang_projectile"
            );
            if (grenadeEntity == null)
            {
                Log.Warn($"Failed to create flashbang entity for {flash.Name}");
                return;
            }

            grenadeEntity.DispatchSpawn();

            grenadeEntity.InitialPosition.X = flash.Origin.X;
            grenadeEntity.InitialPosition.Y = flash.Origin.Y;
            grenadeEntity.InitialPosition.Z = flash.Origin.Z;

            grenadeEntity.InitialVelocity.X = flash.Velocity.X;
            grenadeEntity.InitialVelocity.Y = flash.Velocity.Y;
            grenadeEntity.InitialVelocity.Z = flash.Velocity.Z;

            grenadeEntity.AngVelocity.X = flash.Velocity.X;
            grenadeEntity.AngVelocity.Y = flash.Velocity.Y;
            grenadeEntity.AngVelocity.Z = flash.Velocity.Z;

            grenadeEntity.Teleport(flash.Origin, flash.Angle, flash.Velocity);
            grenadeEntity.Globalname = "custom";
        }
    }
}
