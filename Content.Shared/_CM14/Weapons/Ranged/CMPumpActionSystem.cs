using Content.Shared._CM14.Input;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;

namespace Content.Shared._CM14.Weapons.Ranged;

public sealed class CMPumpActionSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMPumpActionComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<CMPumpActionComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<CMPumpActionComponent, GunShotEvent>(OnGunShot);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.CMPumpShotgun,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } entity)
                        TryPump(entity);
                }, handle: false))
            .Register<CMPumpActionSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<CMPumpActionSystem>();
    }

    private void OnGetVerbs(Entity<CMPumpActionComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
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

    private void OnAttemptShoot(Entity<CMPumpActionComponent> ent, ref AttemptShootEvent args)
    {
        args.Cancelled = !ent.Comp.Pumped;
    }

    private void OnGunShot(Entity<CMPumpActionComponent> ent, ref GunShotEvent args)
    {
        ent.Comp.Pumped = false;
        Dirty(ent);
    }

    private void TryPump(EntityUid user, Entity<CMPumpActionComponent> ent)
    {
        if (!ent.Comp.Running ||
            ent.Comp.Pumped ||
            !_actionBlocker.CanInteract(user, ent))
        {
            return;
        }

        if (TryComp(ent, out UseDelayComponent? delay) &&
            !_useDelay.TryResetDelay((ent, delay), true))
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
            TryComp(hands.ActiveHandEntity, out CMPumpActionComponent? pump))
        {
            TryPump(user, (hands.ActiveHandEntity.Value, pump));
        }
    }
}
