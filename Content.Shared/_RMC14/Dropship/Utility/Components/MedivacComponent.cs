using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility;

[RegisterComponent]
public sealed partial class MedivacComponent : Component
{
    public const string AnimationState = "medevac_system_active";
    public const string AnimationDelay = "medevac_system_delay";

    public bool IsActivated = false;
    [DataField]
    public TimeSpan DelayLength = TimeSpan.FromSeconds(3);
}
