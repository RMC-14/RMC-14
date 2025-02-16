using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Sprite;

namespace Content.Shared._RMC14.Pointing;

public sealed class RMCPointingSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCSpriteSystem _rmcSprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPointingComponent, RMCSpawnPointingArrowEvent>(OnGetPointingArrow);
    }

    private void OnGetPointingArrow(Entity<RMCPointingComponent> ent, ref RMCSpawnPointingArrowEvent ev)
    {
        if (!TryComp(ent, out SquadMemberComponent? member))
        {
            ev.Spawned = Spawn(ent.Comp.Arrow, ev.Coordinates);
            return;
        }

        ev.Spawned = Spawn(ent.Comp.SquadArrow, ev.Coordinates);
        _rmcSprite.SetColor(ev.Spawned.Value, member.BackgroundColor);
    }
}
