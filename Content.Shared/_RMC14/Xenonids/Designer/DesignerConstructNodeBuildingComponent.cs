using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using System;

namespace Content.Shared._RMC14.Xenonids.Designer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DesignerConstructNodeBuildingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid User;

    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public TimeSpan BuildTime;

    [DataField, AutoNetworkedField]
    public bool ThickVariant;
}
