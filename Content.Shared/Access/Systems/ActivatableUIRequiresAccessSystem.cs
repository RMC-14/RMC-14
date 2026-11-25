using Content.Shared.Access.Components;
using Content.Shared.Popups;
using Content.Shared.UserInterface;

namespace Content.Shared.Access.Systems;
public sealed partial class ActivatableUIRequiresAccessSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivatableUIRequiresAccessComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
    }

    private void OnUIOpenAttempt(Entity<ActivatableUIRequiresAccessComponent> activatableUI, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_access.IsAllowed(args.User, activatableUI))
        {
            args.Cancel();
            if (activatableUI.Comp.PopupMessage != null)
                _popup.PopupClient(Loc.GetString(activatableUI.Comp.PopupMessage), activatableUI, args.User);
        }
    }
}

