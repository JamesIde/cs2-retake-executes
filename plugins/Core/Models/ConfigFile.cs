using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace RetakeExecutesPlugin;

public class PluginConfiguration : BasePluginConfig
{
    [JsonPropertyName("db_connection_string")]
    public string? DatabaseConnectionString { get; set; }

    [JsonPropertyName("db_schema")]
    public string? DatabaseSchema { get; set; }
}
