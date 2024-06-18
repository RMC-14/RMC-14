using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Armor;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMArmorSystem))]
public sealed partial class CMHardArmorComponent : Component;
