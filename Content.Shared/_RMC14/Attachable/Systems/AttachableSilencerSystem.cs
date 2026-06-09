using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableSilencerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableSilencerComponent, AttachableRelayedEvent<GunRefreshModifiersEvent>>(OnSilencerRefreshModifiers);
        SubscribeLocalEvent<AttachableSilencerComponent, AttachableRelayedEvent<GunMuzzleFlashAttemptEvent>>(OnSilencerMuzzleFlash);
    }

    private void OnSilencerRefreshModifiers(Entity<AttachableSilencerComponent> ent, ref AttachableRelayedEvent<GunRefreshModifiersEvent> args)
    {
        args.Args.SoundGunshot = ent.Comp.Sound;
    }

    private void OnSilencerMuzzleFlash(Entity<AttachableSilencerComponent> ent, ref AttachableRelayedEvent<GunMuzzleFlashAttemptEvent> args)
    {
        if (ent.Comp.HideMuzzleFlash)
            args.Args.Cancelled = true;
    }
}
