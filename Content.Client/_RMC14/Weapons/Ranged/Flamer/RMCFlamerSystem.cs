using Content.Client.Popups;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;

namespace Content.Client._RMC14.Weapons.Ranged.Flamer;

public sealed class RMCFlamerSystem : SharedRMCFlamerSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new RMCFlamerPreviewOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<RMCFlamerPreviewOverlay>();
    }

    protected override void OnIgniterAttemptShoot(Entity<RMCIgniterComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        base.OnIgniterAttemptShoot(ent, ref args);

        if (!ent.Comp.Enabled)
        {
            var message = _input.TryGetKeyBinding(CMKeyFunctions.CMUniqueAction, out var bind)
                ? Loc.GetString(ent.Comp.PopupKey, ("key", bind.GetKeyString()))
                : Loc.GetString(ent.Comp.Popup);
            _popup.PopupClient(message, args.User, args.User);
        }
    }
}
