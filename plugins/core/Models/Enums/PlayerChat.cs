using System.ComponentModel;

namespace RetakeExecutesPlugin;

public enum PlayerMessage
{
    [Description("Retake Executes requires 2 people to play...waiting for second player.")]
    WARMUP_PENDING_PLAYERS,
}

public enum MessageType
{
    CONSOLE,
    ALERT,
    CHAT,
}
