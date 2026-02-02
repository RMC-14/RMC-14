using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ManageHiveSystem))]
public sealed partial class HiveBoonDefinitionComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Cost = 1;

    [DataField, AutoNetworkedField]
    public int Pylons = 1;

    [DataField, AutoNetworkedField]
    public bool Reusable = true;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown;

    [DataField, AutoNetworkedField]
    public TimeSpan UnlockAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UnlockAtRandomAdd;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<HiveBoonDefinitionComponent>? DuplicateId;

    [DataField, AutoNetworkedField]
    public bool NoLivingKing;

    [DataField, AutoNetworkedField]
    public bool RequiresCore;

    [DataField(required: true), AutoNetworkedField]
    public HiveBoonEvent? Event;
}
