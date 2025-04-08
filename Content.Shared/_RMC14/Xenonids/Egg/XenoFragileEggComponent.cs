using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Egg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoFragileEggComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan? ExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan? ShortExpireAt;

    [DataField, AutoNetworkedField]
    public EntityUid? SustainedBy;

    [DataField, AutoNetworkedField]
    public float SustainRange = 14;

    [DataField, AutoNetworkedField]
    public TimeSpan? BurstAt;

    [DataField, AutoNetworkedField]
    public TimeSpan? BurstDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan SustainCheckEvery = TimeSpan.FromMinutes(1);
}
