﻿using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.GhostAppearance;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCGhostAppearanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? SourceEntity;

    [DataField, AutoNetworkedField]
    public bool Updated = false;
}
