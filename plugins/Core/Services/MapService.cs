using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public class MapService()
{
    public MapConfiguration? InitialiseMapConfiguration(string moduleDirectory, string mapName)
    {
        try
        {
            Server.PrintToChatAll($"[MACROS]: >>> Loading {mapName} configuration file.");

            var mapConfigPath = Path.Combine($"{moduleDirectory}/maps/{mapName}.json");

            if (!File.Exists(mapConfigPath))
            {
                Log.Warn($"Error loading {mapName}.json. File does not exist.");
                return null;
            }

            Log.Info($"Loading {mapName} configuration");

            var parsedMapConfig = MapParser.LoadAndParseMapConfig(mapConfigPath);

            var rounds = parsedMapConfig.MapRounds.Count;

            var totalSmokes = 0;
            var totalCtSpawns = 0;
            var totalTSpawns = 0;
            var totalFlashes = 0;

            foreach (var round in parsedMapConfig.MapRounds)
            {
                totalCtSpawns += round.Spawns.CounterTerroristSpawns.Count;
                totalTSpawns += round.Spawns.TerroristSpawns.Count;
                totalSmokes += round.Smokes.Count;
                totalFlashes += round.Flashes.Count;
            }

            PrintToConsoleOnSuccess(
                mapName,
                parsedMapConfig.ConfigVersion!, // LoadAndParseMapConfig will throw exception if it cannot parse.
                rounds,
                totalSmokes,
                totalCtSpawns,
                totalTSpawns,
                totalFlashes
            );

            Server.PrintToChatAll(
                $" {ChatColors.Gold} [MACROS]: >>> Loaded {mapName} configuration file."
            );
            Log.Info($"Loaded {mapName} configuration");

            return parsedMapConfig;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Log.Error("Map service parsing error", ex);
            throw;
        }
    }

    private static void PrintToConsoleOnSuccess(
        string mapName,
        string version,
        int rounds,
        int smokes,
        int ctSpawns,
        int tSpawns,
        int flashes
    )
    {
        Server.PrintToChatAll($"Loaded     : {true, -33}│");
        Server.PrintToChatAll($"Map        : {mapName, -33}│");
        Server.PrintToChatAll($"Version    : {version, -33}│");
        Server.PrintToChatAll($"Rounds     : {rounds, -33}│");
        Server.PrintToChatAll($"Smokes     : {smokes, -33}│");
        Server.PrintToChatAll($"Flashes    : {flashes, -33}│");
        Server.PrintToChatAll($"CT Spawns  : {ctSpawns, -33}│");
        Server.PrintToChatAll($"T Spawns   : {tSpawns, -33}│");

        Log.Info($"Loaded       : {true, -33}│");
        Log.Info($"Map          : {mapName, -33}│");
        Log.Info($"Version      : {version, -33}│");
        Log.Info($"Rounds       : {rounds, -33}│");
        Log.Info($"Smokes       : {smokes, -33}│");
        Log.Info($"Flashes      : {flashes, -33}│");
        Log.Info($"CT Spawns    : {ctSpawns, -33}│");
        Log.Info($"T Spawns     : {tSpawns, -33}│");

        Console.WriteLine($"Loaded     : {true, -33}│");
        Console.WriteLine($"Map        : {mapName, -33}│");
        Console.WriteLine($"Version    : {version, -33}│");
        Console.WriteLine($"Rounds     : {rounds, -33}│");
        Console.WriteLine($"Smokes     : {smokes, -33}│");
        Console.WriteLine($"Flashes    : {flashes, -33}│");
        Console.WriteLine($"CT Spawns  : {ctSpawns, -33}│");
        Console.WriteLine($"T Spawns   : {tSpawns, -33}│");
    }
}
