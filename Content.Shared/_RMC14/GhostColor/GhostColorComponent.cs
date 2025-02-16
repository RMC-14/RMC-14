﻿using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.GhostColor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color? Color;
}
