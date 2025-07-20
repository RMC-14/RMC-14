using Content.Shared._RMC14.Input;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Input.Binding;

namespace Content.Shared._RMC14.Weapons.Common;

public sealed class UniqueActionSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UniqueActionComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.CMUniqueAction,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            TryUniqueAction(userUid);
                    },
                    handle: false))
            .Register<UniqueActionSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<UniqueActionSystem>();
    }

    private void OnGetVerbs(Entity<UniqueActionComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_actionBlocker.CanInteract(args.User, args.Target))
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => TryUniqueAction(user, ent.Owner),
            Text = "Unique action",
        });
    }

    private void TryUniqueAction(EntityUid userUid)
    {
        if (!_hands.TryGetActiveItem(userUid, out var held))
            return;

        TryUniqueAction(userUid, held.Value);
    }

    private void TryUniqueAction(EntityUid userUid, EntityUid targetUid)
    {
        if (!_actionBlocker.CanInteract(userUid, targetUid))
            return;

        RaiseLocalEvent(targetUid, new UniqueActionEvent(userUid));
    }
}
