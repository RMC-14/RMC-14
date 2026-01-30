#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests._RMC14;

public static class RMCTestExtensions
{
    public static string? Name(this RobustIntegrationTest.IntegrationInstance instance, EntityUid ent)
    {
        return instance.EntMan.GetComponentOrNull<MetaDataComponent>(ent)?.EntityName;
    }

    public static EntityPrototype? Prototype(this RobustIntegrationTest.IntegrationInstance instance, EntityUid ent)
    {
        return instance.EntMan.GetComponentOrNull<MetaDataComponent>(ent)?.EntityPrototype;
    }
}
