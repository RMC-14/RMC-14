using Content.Shared._RMC14.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Shared.Decals;
// ReSharper restore CheckNamespace

public sealed partial class DecalPrototype : ICMSpecific
{
    [DataField]
    public bool IsCM { get; }
}
