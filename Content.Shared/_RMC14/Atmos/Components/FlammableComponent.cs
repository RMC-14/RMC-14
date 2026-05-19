// ReSharper disable CheckNamespace
namespace Content.Shared.Atmos.Components;

public sealed partial class FlammableComponent : IComponentDebug
{
    public string GetDebugString()
    {
        return $"""
            FireSpread: {FireSpread}
            CanResistFire: {CanResistFire}
            FireSpread: {FireSpread}
            Damage: {Damage}
            ResistStacks: {ResistStacks}
            ResistDuration: {ResistDuration.TotalSeconds}
            """;
    }
}
