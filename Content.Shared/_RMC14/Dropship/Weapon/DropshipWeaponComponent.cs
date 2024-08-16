using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class DropshipWeaponComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Abbreviation = string.Empty;

    [DataField, AutoNetworkedField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(7);

    [DataField, AutoNetworkedField]
    public TimeSpan FireDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextFireAt;

    [DataField, AutoNetworkedField]
    public bool FireInTransport;

    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills;
}
