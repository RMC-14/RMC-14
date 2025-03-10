using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class RepairableXenoStructureComponent : Component
{
    public FixedPoint2 StoredPlasma = 0;

    [DataField]
    public TimeSpan RepairLength = TimeSpan.FromSeconds(10);

    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 PlasmaCost;

    [DataField, AutoNetworkedField]
    public FixedPoint2 RepairPercent = 1;
}
