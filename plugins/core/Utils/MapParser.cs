using System.Globalization;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakeExecutesPlugin;

public static class MapParser
{
    public static MapConfiguration LoadAndParseMapConfig(string fileName)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(fileName));
            JsonElement root = doc.RootElement;

            JsonElement rounds = root.GetProperty("rounds");

            var config = new MapConfiguration
            {
                LastUpdated = root.GetProperty("last-updated").GetString(),
                LastUpdatedBy = root.GetProperty("last-updated-by").GetString(),
                ConfigVersion = root.GetProperty("map-config-version").GetString(),
                CreatedAt = root.GetProperty("created-at").GetString(),
                MapRounds = [],
            };

            foreach (JsonElement round in rounds.EnumerateArray())
            {
                JsonElement smokes = round.GetProperty("smokes");
                JsonElement flashes = round.GetProperty("flashes");
                JsonElement spawns = round.GetProperty("spawns");

                config.MapRounds.Add(
                    new Round
                    {
                        RoundStrategy = round.GetProperty("round-strategy").GetString(),
                        RoundIdentifier = round.GetProperty("round-identifier").GetInt32(),
                        Smokes =
                        [
                            .. smokes
                                .EnumerateArray()
                                .Select(s => new Projectile(
                                    s.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                                    ParseVector(s.GetProperty("origin").GetString()),
                                    ParseVector(s.GetProperty("velocity").GetString()),
                                    ParseQAngle(s.GetProperty("angle").GetString()),
                                    s.GetProperty("delay").GetSingle()
                                )),
                        ],
                        Flashes =
                        [
                            .. flashes
                                .EnumerateArray()
                                .Select(s => new Projectile(
                                    s.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                                    ParseVector(s.GetProperty("origin").GetString()),
                                    ParseVector(s.GetProperty("velocity").GetString()),
                                    ParseQAngle(s.GetProperty("angle").GetString()),
                                    s.GetProperty("delay").GetSingle()
                                )),
                        ],
                        Spawns = new Spawns
                        {
                            TerroristSpawns = spawns
                                .GetProperty("terrorist")
                                .EnumerateArray()
                                .Select(s => new SpawnData(
                                    s.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "",
                                    CsTeam.Terrorist,
                                    ParseVector(s.GetProperty("origin").GetString()),
                                    ParseQAngle(s.GetProperty("angle").GetString()),
                                    s.GetProperty("spawnId").GetInt32()
                                ))
                                .ToList(),

                            CounterTerroristSpawns = spawns
                                .GetProperty("counter-terrorist")
                                .EnumerateArray()
                                .Select(s => new SpawnData(
                                    s.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "",
                                    CsTeam.CounterTerrorist,
                                    ParseVector(s.GetProperty("origin").GetString()),
                                    ParseQAngle(s.GetProperty("angle").GetString()),
                                    s.GetProperty("spawnId").GetInt32()
                                ))
                                .ToList(),
                        },
                    }
                );
            }

            return config;
        }
        catch (Exception ex)
        {
            Log.Error($"Error parsing map configuration file", ex);
            Console.WriteLine(ex);
            throw;
        }
    }

    private static Vector ParseVector(string value)
    {
        var values = value.Split(',');

        if (
            !float.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y)
            || !float.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var z)
        )
        {
            throw new Exception("Unable to parse QVector float values.");
        }

        return new Vector(x, y, z);
    }

    private static QAngle ParseQAngle(string value)
    {
        var values = value.Split(',');

        if (
            !float.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y)
            || !float.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var z)
        )
        {
            throw new Exception("Unable to parse QAngle float values.");
        }

        return new QAngle(x, y, z);
    }
}
