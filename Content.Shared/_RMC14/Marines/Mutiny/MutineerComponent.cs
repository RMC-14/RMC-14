using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Mutiny;

[RegisterComponent, NetworkedComponent]
public sealed partial class MutineerComponent : Component
{
    [DataField("icon")]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/job_icons"), "hudmutineer");
}
