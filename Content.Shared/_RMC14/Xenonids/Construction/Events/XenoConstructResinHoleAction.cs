using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

public sealed partial class XenoConstructResinHoleActionEvent : InstantActionEvent
{
    [DataField]
    public FixedPoint2 PlasmaCost = 200;

    [DataField]
    public EntProtoId Prototype = "XenoResinHole";
}
