using Content.Shared._CM14.Input;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;

namespace Content.Shared._CM14.Weapons.Ranged;

public abstract class SharedPumpActionSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PumpActionComponent, ExaminedEvent>(OnExamined, before: [typeof(SharedGunSystem)]);
        SubscribeLocalEvent<PumpActionComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<PumpActionComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<PumpActionComponent, GunShotEvent>(OnGunShot);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.CMPumpShotgun,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } entity)
                        TryPump(entity);
                }, handle: false))
            .Register<SharedPumpActionSystem>();
    }

    protected virtual void OnExamined(Entity<PumpActionComponent> ent, ref ExaminedEvent args)
    {
        // TODO CM14 the server has no idea what this keybind is supposed to be for the client
        args.PushMarkup("[bold]Press [color=cyan]Space[/color] to pump before shooting.[/bold]", 1);
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<SharedPumpActionSystem>();
    }

    private void OnGetVerbs(Entity<PumpActionComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var user = args.User;
        if (!_actionBlocker.CanInteract(user, args.Target))
            return;

        args.Verbs.Add(new InteractionVerb
        {
            Act = () => TryPump(user, ent),
            Text = "Pump"
        });
    }

    protected virtual void OnAttemptShoot(Entity<PumpActionComponent> ent, ref AttemptShootEvent args)
    {
        if (!ent.Comp.Pumped)
            args.Cancelled = true;
    }

    private void OnGunShot(Entity<PumpActionComponent> ent, ref GunShotEvent args)
    {
        ent.Comp.Pumped = false;
        Dirty(ent);
    }

    private void TryPump(EntityUid user, Entity<PumpActionComponent> ent)
    {
        if (!ent.Comp.Running ||
            ent.Comp.Pumped ||
            !_actionBlocker.CanInteract(user, ent))
        {
            return;
        }

        ent.Comp.Pumped = true;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.Sound, ent, user);
    }

    private void TryPump(EntityUid user)
    {
        if (TryComp(user, out HandsComponent? hands) &&
            TryComp(hands.ActiveHandEntity, out PumpActionComponent? pump))
        {
            var ammo = new GetAmmoCountEvent();
            RaiseLocalEvent(hands.ActiveHandEntity.Value, ref ammo);

            if (ammo.Count <= 0)
            {
                _popup.PopupClient("You don't have any ammo left!", user, user);
                return;
            }

            TryPump(user, (hands.ActiveHandEntity.Value, pump));
        }
    }
}
