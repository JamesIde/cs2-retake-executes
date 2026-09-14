namespace RetakeExecutesPlugin;

public static class DbExceptionHandler
{
    public static void Handle(Exception ex, string context = "")
    {
        var separator = new string('-', 40);

        Log.Error(separator);

        if (!string.IsNullOrEmpty(context))
            Log.Error($"CONTEXT:  {context}");

        Log.Error($"TYPE:     {ex.GetType().FullName}");
        Log.Error($"MESSAGE:  {ex.Message}");

        if (ex.InnerException != null)
        {
            Log.Error($"INNER:    {ex.InnerException.GetType().FullName}");
            Log.Error($"          {ex.InnerException.Message}");
        }

        Log.Error($"STACK:");
        Log.Error(ex.StackTrace ?? "No stack trace available");
        Log.Error(separator);
    }
}
