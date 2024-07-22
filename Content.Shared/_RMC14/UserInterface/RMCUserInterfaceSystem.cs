using Content.Shared.UserInterface;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.UserInterface;

public sealed class RMCUserInterfaceSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActivatableUIBlacklistComponent, ActivatableUIOpenAttemptEvent>(OnUIBlacklistAttempt);
        SubscribeLocalEvent<UserBlacklistActivatableUIComponent, UserOpenActivatableUIAttemptEvent>(OnUIBlacklistUserAttempt);
    }

    private void OnUIBlacklistAttempt(Entity<ActivatableUIBlacklistComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CanOpenUI((ent, ent), args.User, null))
            args.Cancel();
    }

    private void OnUIBlacklistUserAttempt(Entity<UserBlacklistActivatableUIComponent> ent, ref UserOpenActivatableUIAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp(args.Target, out ActivatableUIComponent? activatable) &&
            activatable.Key is { } key &&
            ent.Comp.Keys.Contains(key))
        {
            args.Cancel();
        }
    }

    public bool CanOpenUI(Entity<ActivatableUIBlacklistComponent?> ent, Entity<UserBlacklistActivatableUIComponent?> user, Enum? key)
    {
        if (Resolve(ent, ref ent.Comp, false) &&
            _whitelist.IsBlacklistPass(ent.Comp.Blacklist, user))
        {
            return false;
        }

        if (key != null &&
            Resolve(user, ref user.Comp, false) &&
            user.Comp.Keys.Contains(key))
        {
            return false;
        }

        return true;
    }
}
