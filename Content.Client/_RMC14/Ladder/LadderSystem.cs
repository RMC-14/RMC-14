using Content.Shared._RMC14.Ladder;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Ladder;

public sealed class LadderSystem : SharedLadderSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    protected override void ShowRadialMenu(Entity<LadderComponent> ent, EntityUid user)
    {
        _uiSystem.OpenUi(ent.Owner, LadderBuiKey.Key);
    }
}
