using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableIFFSystem : EntitySystem
{
    [Dependency] private readonly AttachableHolderSystem _holder = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableIFFComponent, AttachableAlteredEvent>(OnAttachableIFFAltered);
        SubscribeLocalEvent<AttachableIFFComponent, AttachableRelayedEvent<AttachableGrantIFFEvent>>(OnAttachableIFFGrant);

        SubscribeLocalEvent<GunAttachableIFFComponent, AmmoShotEvent>(OnGunAttachableIFFAmmoShot);
        SubscribeLocalEvent<GunAttachableIFFComponent, ExaminedEvent>(OnGunAttachableIFFExamined);
    }

    private void OnAttachableIFFAltered(Entity<AttachableIFFComponent> ent, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                UpdateGunIFF(args.Holder);
                break;
            case AttachableAlteredType.Detached:
                UpdateGunIFF(args.Holder);
                break;
        }
    }

    private void OnAttachableIFFGrant(Entity<AttachableIFFComponent> ent, ref AttachableRelayedEvent<AttachableGrantIFFEvent> args)
    {
        args.Args.Grants = true;
    }

    private void OnGunAttachableIFFAmmoShot(Entity<GunAttachableIFFComponent> ent, ref AmmoShotEvent args)
    {
        _gunIFF.GiveAmmoIFF(ent, ref args, false, true);
    }

    private void OnGunAttachableIFFExamined(Entity<GunAttachableIFFComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(GunAttachableIFFComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-examine-text-iff"));
        }
    }

    private void UpdateGunIFF(EntityUid gun)
    {
        if (!TryComp(gun, out AttachableHolderComponent? holder))
            return;

        var ev = new AttachableGrantIFFEvent();
        _holder.RelayEvent((gun, holder), ref ev);

        if (_timing.ApplyingState)
            return;

        if (ev.Grants)
            EnsureComp<GunAttachableIFFComponent>(gun);
        else
            RemCompDeferred<GunAttachableIFFComponent>(gun);
    }
}
