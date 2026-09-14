namespace RetakeExecutesPlugin;

public class SpawnService()
{
    private static readonly Random random = new();

    public Dictionary<ulong, SpawnData> AllocateSpawnToPlayers(
        List<ulong> playerIds,
        List<SpawnData> spawns
    )
    {
        if (playerIds.Count > spawns.Count)
        {
            throw new Exception(
                $"The number of players {playerIds.Count} to assign a spawn to {spawns.Count} is greater than the number of spawns available."
            );
        }

        var allocatedPlayerSpawn = new Dictionary<ulong, SpawnData>();

        var spawnQueue = ShuffleSpawns(spawns);

        foreach (var playerId in playerIds)
        {
            if (!spawnQueue.TryDequeue(out SpawnData spawn))
                throw new Exception("Could not retrieve spawn from spawn shuffler");

            Log.Info($"Assigned {playerId} to {spawn.Label}");
            allocatedPlayerSpawn.Add(playerId, spawn);
        }
        return allocatedPlayerSpawn;
    }

    private static Queue<SpawnData> ShuffleSpawns(List<SpawnData> spawns)
    {
        if (spawns == null || spawns.Count == 0)
            return new Queue<SpawnData>();

        var items = spawns.ToArray();
        random.Shuffle(items);
        return new Queue<SpawnData>(items);
    }
}
