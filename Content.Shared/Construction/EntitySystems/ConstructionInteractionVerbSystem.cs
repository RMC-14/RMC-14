using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;

namespace Content.Shared.Construction.EntitySystems;

public sealed class ConstructionInteractionVerbSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConstructionInteractionVerbComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<ConstructionInteractionVerbComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;

        var v = new Verb
        {
            Priority = 1,
            Text = Loc.GetString(ent.Comp.VerbText),
            Impact = LogImpact.Low,
            DoContactInteraction = true,
            Act = () =>
            {
                var ev = new ConstructionInteractionEvent(user, ent.Comp.InteractionId);
                RaiseLocalEvent(ent, ev);

                if (_net.IsClient || ev.Handled)
                    return;

                _popup.PopupEntity(Loc.GetString(ent.Comp.VerbFailedText, ("entityName", ent.Owner)), ent, user);
            },
        };

        args.Verbs.Add(v);
    }
}
