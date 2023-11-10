using Content.Shared._CM14.Marines.Squads;
using Robust.Shared.Utility;

namespace Content.Shared._CM14.Marines;

public abstract class SharedMarineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarineComponent, GetMarineIconEvent>(OnMarineGetIcon,
            after: new[] { typeof(SquadSystem) });
    }

    private void OnMarineGetIcon(Entity<MarineComponent> ent, ref GetMarineIconEvent args)
    {
        if (ent.Comp.Icon is { } icon)
            args.Icons.Add(icon);
    }

    public void GetMarineIcons(EntityUid uid, List<SpriteSpecifier> icons)
    {
        var ev = new GetMarineIconEvent(icons);
        RaiseLocalEvent(uid, ref ev);
    }

    public void MakeMarine(EntityUid uid, SpriteSpecifier? icon)
    {
        var marine = EnsureComp<MarineComponent>(uid);
        marine.Icon = icon;
        Dirty(uid, marine);
    }
}
