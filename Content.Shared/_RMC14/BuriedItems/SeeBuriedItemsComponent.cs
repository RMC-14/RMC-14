using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.BuriedItems;

/// <summary>
/// Marker component that allows an entity to see buried items.
/// Only entities with this component will have buried-item sprites revealed client-side.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SeeBuriedItemsComponent : Component
{
}
