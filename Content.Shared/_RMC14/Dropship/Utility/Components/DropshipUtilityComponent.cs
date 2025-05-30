using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DropshipUtilityComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ActivateDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextActivateAt;

    [DataField, AutoNetworkedField]
    public bool ActivateInTransport;

    [DataField, AutoNetworkedField]
    public SkillWhitelist? Skills;

    public EntityUid? AttachmentPoint;

    /// <summary>
    /// Cached target of the weapons terminal
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Target = null;
}

[Serializable, NetSerializable]
public enum DropshipUtilityVisuals
{
    Sprite,
    State,
}
