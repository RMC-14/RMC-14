using Content.Shared._RMC14.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Shared.Roles;

public sealed partial class AntagPrototype : ICMSpecific
{
    [DataField]
    public bool IsCM { get; }
}
