using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Wounds;

/// <summary>
/// Restores a leg movement modifier when eschar is removed.
/// This component is added to the affected leg/foot body-part entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWoundsSystem))]
public sealed partial class EscharMoveEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public float WalkSpeed;

    [DataField, AutoNetworkedField]
    public float SprintSpeed;
}
