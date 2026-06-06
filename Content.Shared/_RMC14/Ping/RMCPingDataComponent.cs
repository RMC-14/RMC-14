using System.Collections.Generic;

namespace Content.Shared._RMC14.Ping;

public interface RMCPingDataComponent
{
    string Name { get; }
    string Description { get; }
    int Priority { get; }
    bool IsConstruction { get; }
    IReadOnlyCollection<string> Categories { get; }
}
