using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;

namespace Content.Shared.CM14.Xenos.Rest;

public sealed class XenoRestSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoRestEvent>(OnXenoRest);
        SubscribeLocalEvent<XenoRestingComponent, UpdateCanMoveEvent>(OnXenoRestingCanMove);
    }

    private void OnXenoRestingCanMove(Entity<XenoRestingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnXenoRest(Entity<XenoComponent> ent, ref XenoRestEvent args)
    {
        if (HasComp<XenoRestingComponent>(ent))
        {
            RemComp<XenoRestingComponent>(ent);
            _appearance.SetData(ent, XenoVisualLayers.Base, XenoRestState.NotResting);
        }
        else
        {
            AddComp<XenoRestingComponent>(ent);
            _appearance.SetData(ent, XenoVisualLayers.Base, XenoRestState.Resting);
        }

        _actionBlocker.UpdateCanMove(ent);
    }
}
