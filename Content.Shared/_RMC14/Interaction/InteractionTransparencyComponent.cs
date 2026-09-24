using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Interaction;

/// <summary>
/// Makes the entity that has this component transparent to clicks/hits
/// if the client entity is at the sprite coordinates of the entity that has this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InteractionTransparencyComponent : Component;
