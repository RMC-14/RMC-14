using Content.Shared._RMC14.GMRequest;

namespace Content.Client._RMC14.GMRequest;

/// <summary>
/// Handles the storage and modification of the client copy of GM Request Logs
/// </summary>
public sealed class GMRequestClientManager
{
    public Dictionary<int, GMRequestLog> Logs { get; set; } = new();

    public void SetLogs(Dictionary<int, GMRequestLog> logs)
    {
        Logs = logs;
    }

    public void AddLog(int id, GMRequestLog log)
    {
        Logs[id] = log;
    }

    public void ClearLogs()
    {
        Logs.Clear();
    }
}
