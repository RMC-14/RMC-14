using Content.Shared._RMC14.Rules;

namespace Content.Client._RMC14.Rules;

public sealed class RMCRoundInfoSystem : EntitySystem
{
    public string GetOperationName()
    {
        var query = EntityQueryEnumerator<RMCRoundInfoComponent>();
        while (query.MoveNext(out _, out var info))
            return info.OperationName;
        return string.Empty;
    }

    public string GetPlanetName()
    {
        var query = EntityQueryEnumerator<RMCRoundInfoComponent>();
        while (query.MoveNext(out _, out var info))
            return info.PlanetName;
        return string.Empty;
    }

    public string GetShipName()
    {
        var query = EntityQueryEnumerator<RMCRoundInfoComponent>();
        while (query.MoveNext(out _, out var info))
            return info.ShipName;
        return string.Empty;
    }
}
