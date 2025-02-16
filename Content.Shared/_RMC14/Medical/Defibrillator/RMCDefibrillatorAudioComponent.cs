﻿using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Defibrillator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMDefibrillatorSystem))]
public sealed partial class RMCDefibrillatorAudioComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Defibrillator;
}
