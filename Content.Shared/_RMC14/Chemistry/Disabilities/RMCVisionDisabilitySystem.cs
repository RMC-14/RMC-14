using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Clothing;
using Content.Shared.Examine;
using Content.Shared.Flash;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

// TODO RMC14: cm13 blindness also hides visible emotes & messages, disables smartgun aim assist,
// stops penlight pupil checks & stops checking other people for injuries
public sealed class RMCVisionDisabilitySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string BlurKey = "Blinded";

    private static readonly ProtoId<TagPrototype> PrescriptionTag = "RMCPrescriptionGlasses";
    private static readonly string[] PrescriptionSlots = ["eyes", "mask", "head"];

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan NearsightedBlur = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan CarrotJuiceInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan CarrotJuiceResetGap = TimeSpan.FromSeconds(3);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBlindDisabilityComponent, FlashAttemptEvent>(OnBlindFlashAttempt);
        SubscribeLocalEvent<HumanoidAppearanceComponent, ExamineAttemptEvent>(OnHumanoidExamineAttempt);
    }

    private void OnBlindFlashAttempt(Entity<RMCBlindDisabilityComponent> ent, ref FlashAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnHumanoidExamineAttempt(Entity<HumanoidAppearanceComponent> ent, ref ExamineAttemptEvent args)
    {
        if (HasComp<RMCBlindDisabilityComponent>(args.Examiner))
            args.Cancel();
    }

    public void CarrotJuiceTick(EntityUid uid)
    {
        if (!TryComp(uid, out RMCNearsightedComponent? nearsighted))
            return;

        var time = _timing.CurTime;
        var elapsed = time - nearsighted.LastCarrotJuice;
        if (elapsed > CarrotJuiceResetGap)
        {
            nearsighted.CarrotJuiceStart = time;
            elapsed = TimeSpan.Zero;
        }

        nearsighted.LastCarrotJuice = time;

        var data = 1 + (time - nearsighted.CarrotJuiceStart) / CarrotJuiceInterval;
        if (data < 21)
            return;

        var chance = Math.Clamp((data - 10) / 100, 0, 1);
        var tickChance = 1 - Math.Pow(1 - chance, elapsed / CarrotJuiceInterval);
        if (_random.Prob((float) tickChance))
            RemCompDeferred<RMCNearsightedComponent>(uid);
    }

    private bool HasPrescription(EntityUid uid)
    {
        foreach (var slot in PrescriptionSlots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out var item) && _tag.HasTag(item.Value, PrescriptionTag))
                return true;
        }

        return _inventory.TryGetSlotEntity(uid, "head", out var helmet) && HasLoweredPrescriptionAccessory(helmet.Value);
    }

    private bool HasLoweredPrescriptionAccessory(EntityUid helmet)
    {
        if (!HasComp<HelmetAccessoryHolderComponent>(helmet) ||
            !TryComp(helmet, out StorageComponent? storage))
        {
            return false;
        }

        foreach (var accessory in storage.Container.ContainedEntities)
        {
            if (_tag.HasTag(accessory, PrescriptionTag) &&
                TryComp(accessory, out ItemToggleComponent? toggle) &&
                toggle.Activated)
            {
                return true;
            }
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCNearsightedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.NextCheck)
                continue;

            comp.NextCheck = time + CheckInterval;

            // TODO RMC14: cm13 skips this at 80+ pain or when the eyes arent healthy
            if (HasPrescription(uid))
            {
                _statusEffects.TryRemoveStatusEffect(uid, BlurKey);
                continue;
            }

            if (_statusEffects.TryGetTime(uid, BlurKey, out var blur) && blur.Value.Item2 - time >= NearsightedBlur)
                continue;

            _statusEffects.TryAddStatusEffect<RMCBlindedComponent>(uid, BlurKey, NearsightedBlur, true);
        }
    }
}
