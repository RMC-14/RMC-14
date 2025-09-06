using Content.Shared.UserInterface;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.UserInterface;

public sealed class RMCUserInterfaceSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private readonly List<(Entity<UserInterfaceComponent?> Ent, Action<Entity<UserInterfaceComponent?>, RMCUserInterfaceSystem> Act)> _toRefresh = new();

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

    public void RefreshUIs<T>(Entity<UserInterfaceComponent?> uiEnt) where T : BoundUserInterface, IRefreshableBui
    {
        _toRefresh.Add((uiEnt, static (uiEnt, system) =>
        {
            try
            {
                if (system.TerminatingOrDeleted(uiEnt))
                    return;

                if (!system.Resolve(uiEnt, ref uiEnt.Comp))
                    return;

                foreach (var bui in uiEnt.Comp.ClientOpenInterfaces.Values)
                {
                    if (bui is T ui)
                        ui.Refresh();
                }
            }
            catch (Exception e)
            {
                system.Log.Error($"Error refreshing {nameof(T)}\n{e}");
            }
        }));
    }

    public override void Update(float frameTime)
    {
        try
        {
            foreach (var refresh in _toRefresh)
            {
                refresh.Act(refresh.Ent, this);
            }
        }
        finally
        {
            _toRefresh.Clear();
        }
    }
}
