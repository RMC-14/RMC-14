using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.Construction.EntitySystems;

public sealed class ConstructionInteractionVerbSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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
                var ev = new ConstructionInteractionEvent(user);
                RaiseLocalEvent(ent, ev);

                if (ev.Handled)
                    return;

                _popup.PopupClient(Loc.GetString(ent.Comp.VerbFailedText, ("entityName", ent.Owner)), user);
            },
        };

        args.Verbs.Add(v);
    }
}
