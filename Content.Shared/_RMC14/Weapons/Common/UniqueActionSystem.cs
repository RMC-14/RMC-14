using Content.Shared._RMC14.Input;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using Robust.Shared.Input.Binding;

namespace Content.Shared._RMC14.Weapons.Common;

public sealed class UniqueActionSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;


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

        if (!_actionBlockerSystem.CanInteract(args.User, args.Target))
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
        if (!_entityManager.TryGetComponent(userUid, out HandsComponent? handsComponent) ||
            handsComponent.ActiveHandEntity == null)
            return;

        TryUniqueAction(userUid, handsComponent.ActiveHandEntity.Value);
    }

    private void TryUniqueAction(EntityUid userUid, EntityUid targetUid)
    {
        if (!_actionBlockerSystem.CanInteract(userUid, targetUid))
            return;

        RaiseLocalEvent(targetUid, new UniqueActionEvent(userUid));
    }
}
