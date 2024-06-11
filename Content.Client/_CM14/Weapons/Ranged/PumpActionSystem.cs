using Content.Shared._CM14.Input;
using Content.Shared._CM14.Weapons.Ranged;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Input;

namespace Content.Client._CM14.Weapons.Ranged;

public sealed class PumpActionSystem : SharedPumpActionSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void OnExamined(Entity<PumpActionComponent> ent, ref ExaminedEvent args)
    {
        if (!_input.TryGetKeyBinding(CMKeyFunctions.CMUniqueAction, out var bind))
            return;

        args.PushMarkup($"[bold]Press your [color=cyan]unique action[/color] keybind (Spacebar by default) to pump before shooting.[/bold]", 1);
    }

    protected override void OnAttemptShoot(Entity<PumpActionComponent> ent, ref AttemptShootEvent args)
    {
        base.OnAttemptShoot(ent, ref args);

        if (!ent.Comp.Pumped)
        {
            var message = _input.TryGetKeyBinding(CMKeyFunctions.CMUniqueAction, out var bind)
                ? $"You need to pump the gun with {bind.GetKeyString()} first!"
                : "You need to pump the gun first!";
            _popup.PopupClient(message, args.User, args.User);
        }
    }
}
