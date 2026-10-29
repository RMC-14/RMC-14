using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Stamina;
using Content.Shared._RMC14.Synth;
using Content.Shared.Chat.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Addiction;

public sealed class RMCAddictionSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _rmcEmote = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly RMCStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;

    private static readonly EntProtoId<StatusEffectComponent> Hallucination = "RMCStatusEffectHallucination";

    private static readonly ProtoId<EmotePrototype>[] HighEmotes = ["RMCTwitch", "RMCBlink", "RMCShiver"];
    private static readonly ProtoId<EmotePrototype>[] StageTwoEmotes = ["RMCWinceSlightly", "RMCGrimace"];
    private static readonly ProtoId<EmotePrototype>[] StageThreeEmotes = ["RMCWince", "RMCGrimace", "RMCGroan"];
    private static readonly ProtoId<EmotePrototype>[] StageFourEmotes = ["RMCGroanPainfully", "RMCContortPain"];

    private static readonly string[] StageOneCravings =
    [
        "You could use another hit of {0}.",
        "More {0} would be nice.",
        "Another dose of {0} would help.",
        "One more dose of {0} wouldn't hurt.",
        "Why not take one more {0}?",
    ];

    private static readonly string[] StageTwoCravings =
    [
        "It's just not the same without {0}.",
        "You could use another hit of {0}.",
        "You should take more {0}.",
        "Just one more {0}.",
        "Looks like you need more {0}.",
    ];

    private static readonly string[] StageThreeCravings =
    [
        "You need more {0}.",
        "It's hard to go on like this without {0}.",
        "You want more {0}. You need more.",
        "Just take another hit of {0}. Now.",
        "One more {0}.",
    ];

    private static readonly string[] StageFourCravings =
    [
        "You need another dose of {0}, now. NOW.",
        "You can't stand it. You have to go back to {0}. You have to go back.",
        "You need more {0}. YOU NEED MORE.",
        "MORE {1}",
        "TAKE MORE {1}.",
    ];

    private const int MaxStage = 4;
    private const float ProgressionThreshold = 300f;
    private const float DecayPerTick = 0.383f;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCAddictedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<RMCAddictedComponent, AfterAutoHandleStateEvent>(OnAddictedAfterState);
        SubscribeLocalEvent<RMCAddictedComponent, ComponentShutdown>(OnAddictedShutdown);
    }

    private void OnRefreshMovementSpeed(Entity<RMCAddictedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.SpeedMultiplier < 1f)
            args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
    }

    private void OnAddictedAfterState(Entity<RMCAddictedComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnAddictedShutdown(Entity<RMCAddictedComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<RMCAddictedComponent>();
        while (query.MoveNext(out var uid, out var addicted))
        {
            if (_mobState.IsDead(uid))
                continue;

            ProcessMob((uid, addicted));
        }
    }

    private void ProcessMob(Entity<RMCAddictedComponent> ent)
    {
        _bloodstream.TryGetChemicalSolution(ent, out _, out var solution);

        var speedMultiplier = 1f;
        foreach (var record in ent.Comp.Addictions)
        {
            if (solution != null && solution.GetTotalPrototypeQuantity(record.ReagentId) > FixedPoint2.Zero)
            {
                record.WithdrawalProgression = 0;
                HandleChem(ent, record);
                continue;
            }

            record.AddictionProgression -= DecayPerTick;
            if (record.AddictionProgression < 0)
            {
                if (record.Stage == 1)
                {
                    record.AddictionProgression = 0;
                }
                else
                {
                    record.Stage--;
                    record.AddictionProgression = ProgressionThreshold;
                }
            }

            record.WithdrawalProgression += record.Multiplier;
            speedMultiplier = MathF.Min(speedMultiplier, ApplyWithdrawalSymptoms(ent, record));
        }

        ent.Comp.SpeedMultiplier = speedMultiplier;
        Dirty(ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void HandleChem(EntityUid uid, RMCAddictionRecord record)
    {
        if (record.Stage < MaxStage)
        {
            record.AddictionProgression += record.Multiplier;
            if (record.AddictionProgression > ProgressionThreshold)
            {
                record.AddictionProgression = 0;
                record.Stage++;
            }
        }
        else
        {
            record.AddictionProgression = MathF.Min(record.AddictionProgression + 1, ProgressionThreshold);
        }

        switch (record.Stage)
        {
            case 2:
                if (_random.Prob(0.01f))
                    HallucinationFloor(uid, 50);
                break;
            case 3:
                if (_random.Prob(0.01f))
                    HallucinationFloor(uid, 75);
                break;
            case 4:
                if (_random.Prob(0.02f))
                    HallucinationFloor(uid, 75);

                // TODO RMC14: cm13 also deals 5 organ damage to a random heart/lungs/liver/kidneys at a 0.5% chance
                break;
        }

        if (_random.Prob(0.02f))
            Emote(uid, HighEmotes);
    }

    private float ApplyWithdrawalSymptoms(EntityUid uid, RMCAddictionRecord record)
    {
        switch (record.Stage)
        {
            case 1:
                if (record.AddictionProgression == 0)
                {
                    if (_random.Prob(0.2f))
                        StaminaFloor(uid, 12);

                    return 1f;
                }

                if (_random.Prob(0.2f))
                {
                    StaminaFloor(uid, MathF.Min(20, record.WithdrawalProgression * 0.5f));
                    if (_random.Prob(0.5f))
                        Craving(uid, record, StageOneCravings);
                }

                return SlowdownToMultiplier(TimeSpan.FromSeconds(0.5));
            case 2:
                if (_random.Prob(0.2f))
                {
                    StaminaFloor(uid, MathF.Min(40, record.WithdrawalProgression));
                    if (_random.Prob(0.5f))
                        Craving(uid, record, StageTwoCravings);

                    if (_random.Prob(0.25f))
                        Emote(uid, StageTwoEmotes);
                }

                return SlowdownToMultiplier(TimeSpan.FromSeconds(0.7));
            case 3:
                if (_random.Prob(0.03f))
                    HallucinationFloor(uid, 50);

                if (_random.Prob(0.2f))
                {
                    StaminaFloor(uid, MathF.Min(60, record.WithdrawalProgression * 1.5f));
                    if (_random.Prob(0.75f))
                        Craving(uid, record, StageThreeCravings, PopupType.MediumCaution);

                    if (_random.Prob(0.25f))
                        Emote(uid, StageThreeEmotes);
                }

                return SlowdownToMultiplier(TimeSpan.FromSeconds(1));
            default:
                if (_random.Prob(0.05f))
                    HallucinationFloor(uid, 75);

                if (_random.Prob(0.2f))
                {
                    StaminaFloor(uid, MathF.Min(80, record.WithdrawalProgression * 2f));
                    Craving(uid, record, StageFourCravings, PopupType.LargeCaution);

                    if (_random.Prob(0.25f))
                        Emote(uid, StageFourEmotes);
                }

                return SlowdownToMultiplier(TimeSpan.FromSeconds(1));
        }
    }

    private static float SlowdownToMultiplier(TimeSpan slowdown)
    {
        return (float) ((UpdateInterval - slowdown) / UpdateInterval);
    }

    private void StaminaFloor(EntityUid uid, float loss)
    {
        if (!TryComp<RMCStaminaComponent>(uid, out var stamina))
            return;

        var currentLoss = stamina.Max - stamina.Current;
        if (currentLoss < loss)
            _stamina.DoStaminaDamage((uid, stamina), loss - currentLoss);
    }

    private void HallucinationFloor(EntityUid uid, float seconds)
    {
        _status.TryUpdateStatusEffectDuration(uid, Hallucination, TimeSpan.FromSeconds(seconds));
    }

    private void Craving(EntityUid uid, RMCAddictionRecord record, string[] messages, PopupType popup = PopupType.SmallCaution)
    {
        var reagent = _rmcReagent.TryIndex(record.ReagentId, out var proto)
            ? proto.LocalizedName
            : record.ReagentId;

        var message = string.Format(_random.Pick(messages), reagent, reagent.ToUpperInvariant());
        _popup.PopupEntity(message, uid, uid, popup);
    }

    private void Emote(EntityUid uid, ProtoId<EmotePrototype>[] emotes)
    {
        _rmcEmote.TryEmoteWithChat(uid, _random.Pick(emotes), hideLog: true, forceEmote: true);
    }

    public void TryExposeToAddictive(EntityUid uid, string reagentId, float potency)
    {
        if (HasComp<RMCAddictionResistantComponent>(uid) ||
            _mobState.IsDead(uid) ||
            !HasComp<HumanoidAppearanceComponent>(uid) ||
            HasComp<SynthComponent>(uid))
        {
            return;
        }

        var addicted = EnsureComp<RMCAddictedComponent>(uid);
        if (addicted.Addictions.Exists(r => r.ReagentId == reagentId))
            return;

        addicted.Addictions.Add(new RMCAddictionRecord
        {
            ReagentId = reagentId,
            Stage = 1,
            AddictionProgression = 1,
            Multiplier = potency,
        });
        Dirty(uid, addicted);
    }

    public void TryReduceAddictions(EntityUid uid, float potency)
    {
        if (!TryComp<RMCAddictedComponent>(uid, out var addicted))
            return;

        var cmPotency = potency * 2f;
        if (cmPotency > 3f)
        {
            RemComp<RMCAddictedComponent>(uid);
            EnsureComp<RMCAddictionResistantComponent>(uid);
            return;
        }

        for (var i = addicted.Addictions.Count - 1; i >= 0; i--)
        {
            var record = addicted.Addictions[i];
            record.WithdrawalProgression -= cmPotency;
            record.AddictionProgression -= cmPotency;

            if (record.AddictionProgression < cmPotency)
            {
                addicted.Addictions.RemoveAt(i);
                EnsureComp<RMCAddictionResistantComponent>(uid);
            }
        }

        if (addicted.Addictions.Count == 0)
            RemComp<RMCAddictedComponent>(uid);
        else
            Dirty(uid, addicted);
    }
}
