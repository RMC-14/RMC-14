using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Finesse;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoFinesseSystem))]
public sealed partial class XenoSpreadMarkAttemptComponent : Component
{
    [DataField]
    public EntityUid Origin;

    [DataField]
    public TimeSpan TimeOfSpreadAttempt;
}
