using Content.Shared._CM14.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Shared.Roles;

public sealed partial class AntagPrototype : ICMSpecific
{
    [DataField]
    public bool IsCM { get; }
}
