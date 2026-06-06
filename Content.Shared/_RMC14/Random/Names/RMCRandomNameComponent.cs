using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Random.Names;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCRandomNameComponent : Component
{
    [DataField(required: true)]
    public LocId BaseName;

    [DataField(required: true)]
    public LocId PostFix;

    [DataField]
    public int MaxNumber = 2500;
}
