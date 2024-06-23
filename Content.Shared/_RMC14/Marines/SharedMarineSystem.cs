using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines;

public abstract class SharedMarineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarineComponent, GetMarineIconEvent>(OnMarineGetIcon);
    }

    private void OnMarineGetIcon(Entity<MarineComponent> marine, ref GetMarineIconEvent args)
    {
        if (marine.Comp.Icon is { } icon)
            args.Icon = icon;
    }

    public GetMarineIconEvent GetMarineIcon(EntityUid uid)
    {
        var ev = new GetMarineIconEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev;
    }

    public void MakeMarine(EntityUid uid, SpriteSpecifier? icon)
    {
        var marine = EnsureComp<MarineComponent>(uid);
        marine.Icon = icon;
        Dirty(uid, marine);
    }
}
