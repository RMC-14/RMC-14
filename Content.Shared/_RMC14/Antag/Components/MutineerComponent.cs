using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Antag.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MutineerComponent : Component
{
    [DataField("oldIcon")]
    public SpriteSpecifier? OldIcon = null!;

    [DataField("isValid")]
    public bool IsValid = false;
}
