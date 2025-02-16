﻿using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Stasis;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMStasisBagSystem))]
public sealed partial class CMInStasisComponent : Component
{
    [DataField, AutoNetworkedField]
    public float IncubationMultiplier = 0.333f;

    [DataField, AutoNetworkedField]
    public int LessEffectiveStage = 4;
}
