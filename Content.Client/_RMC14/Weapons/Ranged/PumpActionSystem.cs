using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Input;

namespace Content.Client._RMC14.Weapons.Ranged;

public sealed class PumpActionSystem : SharedPumpActionSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void OnExamined(Entity<PumpActionComponent> ent, ref ExaminedEvent args)
    {
        if (!_input.TryGetKeyBinding(CMKeyFunctions.CMUniqueAction, out var bind))
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.Examine), 1);
    }

    protected override void OnAttemptShoot(Entity<PumpActionComponent> ent, ref AttemptShootEvent args)
    {
        base.OnAttemptShoot(ent, ref args);

        if (!ent.Comp.Pumped)
        {
            var message = _input.TryGetKeyBinding(CMKeyFunctions.CMUniqueAction, out var bind)
                ? Loc.GetString(ent.Comp.PopupKey, ("key", bind.GetKeyString()))
                : Loc.GetString(ent.Comp.Popup);
            _popup.PopupClient(message, args.User, args.User);
        }
    }
}
