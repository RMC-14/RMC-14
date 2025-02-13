using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.CustomDelete;

/// <summary>
/// Some entities have components that require extra cleanup before deletion.
/// Attach this component to them and handle their deletion with <see cref="CustomDeleteEvent" />.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CustomDeleteComponent : Component
{

}

[Serializable, NetSerializable]
public sealed partial class CustomDeleteEvent : EntityEventArgs
{

}
