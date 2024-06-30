using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Wieldable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWieldableSystem))]
public sealed partial class WieldableSpeedModifiersComponent : Component
{
    [DataField, AutoNetworkedField]
    public float UnwieldedBaseWalk = 1f;

    [DataField, AutoNetworkedField]
    public float UnwieldedModifiedWalk = 1f;

    [DataField, AutoNetworkedField]
    public float UnwieldedBaseSprint = 1f;

    [DataField, AutoNetworkedField]
    public float UnwieldedModifiedSprint = 1f;
    
    [DataField, AutoNetworkedField]
    public float WieldedBaseWalk = 1f;

    [DataField, AutoNetworkedField]
    public float WieldedModifiedWalk = 1f;

    [DataField, AutoNetworkedField]
    public float WieldedBaseSprint = 1f;

    [DataField, AutoNetworkedField]
    public float WieldedModifiedSprint = 1f;
}
