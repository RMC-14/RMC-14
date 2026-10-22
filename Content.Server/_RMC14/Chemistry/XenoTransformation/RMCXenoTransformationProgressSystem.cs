using Content.Server._RMC14.Language.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Chemistry.Effects.Special;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Chemistry.XenoTransformation;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Gibbing;
using Content.Shared._RMC14.Jittering;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using LanguagePrototype = Content.Shared._RMC14.Language.Prototypes.LanguagePrototype;

namespace Content.Server._RMC14.Chemistry.XenoTransformation;

public sealed class RMCXenoTransformationProgressSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly RMCGibSystem _gib = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly RMCJitterinessSystem _jitteriness = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCXenoTransformationSystem _transformation = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<LanguagePrototype> XenoLanguage = "Xeno";

    private static readonly LocId[] Stage2Messages =
    [
        "rmc-xeno-transformation-scratchy",
        "rmc-xeno-transformation-heart-faster",
        "rmc-xeno-transformation-throat-catch",
    ];

    private static readonly LocId[] Stage3Thoughts =
    [
        "rmc-xeno-transformation-thought-one",
        "rmc-xeno-transformation-thought-evolve",
        "rmc-xeno-transformation-thought-capture",
        "rmc-xeno-transformation-thought-perfect",
    ];

    private static readonly LocId[] Stage4Skin =
    [
        "rmc-xeno-transformation-skin-very-tight",
        "rmc-xeno-transformation-blood-boils",
    ];

    private static readonly LocId[] Stage4Whispers =
    [
        "rmc-xeno-transformation-whisper-one",
        "rmc-xeno-transformation-whisper-evolve",
        "rmc-xeno-transformation-whisper-capture",
        "rmc-xeno-transformation-whisper-perfect",
    ];

    private static readonly LocId[] Stage5Whispers =
    [
        "rmc-xeno-transformation-whisper-close",
        "rmc-xeno-transformation-whisper-evolve-now",
        "rmc-xeno-transformation-whisper-capture-all",
        "rmc-xeno-transformation-whisper-more",
        "rmc-xeno-transformation-whisper-hiss",
        "rmc-xeno-transformation-whisper-clear",
    ];

    private static readonly LocId[] Stage5Chest =
    [
        "rmc-xeno-transformation-emptiness",
        "rmc-xeno-transformation-grieve",
        "rmc-xeno-transformation-heart-painful",
    ];

    private readonly List<Entity<RMCXenoTransformationComponent>> _toAlienize = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _toAlienize.Clear();

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCXenoTransformationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.NextTick)
                continue;

            comp.NextTick = time + comp.TickEvery;

            if (_mobState.IsDead(uid))
            {
                if (_random.Prob(comp.LongevityDecayChance) && --comp.Longevity <= 0)
                    _transformation.Cure((uid, comp), false);

                continue;
            }

            if (StageAct((uid, comp)))
                _toAlienize.Add((uid, comp));
        }

        foreach (var ent in _toAlienize)
        {
            Alienize(ent);
        }
    }

    private bool StageAct(Entity<RMCXenoTransformationComponent> ent)
    {
        var (uid, comp) = ent;
        if (!comp.Carrier)
        {
            comp.Age++;
            if (comp.Age > comp.StageMinimumAge)
            {
                comp.Stage = Math.Min(comp.Stage + 1, comp.MaxStage);
                comp.Age = 0;
            }
        }

        UpdateCipheringHive(ent);

        switch (comp.Stage)
        {
            case 1:
                if (!comp.Started)
                {
                    comp.Started = true;
                    Popup(uid, "rmc-xeno-transformation-begun", PopupType.Small);
                }
                break;
            case 2:
                foreach (var message in Stage2Messages)
                {
                    if (_random.Prob(0.01f))
                        Popup(uid, message, PopupType.Small);
                }
                break;
            case 3:
                if (!comp.HivemindGranted)
                {
                    comp.HivemindGranted = true;
                    GrantHivemind(ent);
                }

                if (_random.Prob(0.05f))
                {
                    Popup(uid, "rmc-xeno-transformation-something-throat", PopupType.MediumCaution);
                    Brute(uid, 1);
                }

                if (_random.Prob(0.08f))
                {
                    Popup(uid, "rmc-xeno-transformation-skin-tight", PopupType.MediumCaution);
                    Brute(uid, 2);
                }

                if (_random.Prob(0.04f))
                {
                    Popup(uid, "rmc-xeno-transformation-stabbing-head", PopupType.MediumCaution);
                    _sizeStun.TryKnockOut(uid, TimeSpan.FromSeconds(4));
                }

                if (_random.Prob(0.04f))
                    Popup(uid, "rmc-xeno-transformation-move-inside", PopupType.MediumCaution);

                if (_random.Prob(0.05f))
                    Popup(uid, _random.Pick(Stage3Thoughts), PopupType.Small);
                break;
            case 4:
                if (_random.Prob(0.1f))
                {
                    Popup(uid, _random.Pick(Stage4Skin), PopupType.MediumCaution);
                    Brute(uid, 3);
                }

                if (_random.Prob(0.05f))
                    Whisper(uid, _random.Pick(Stage4Whispers));

                if (_random.Prob(0.08f))
                    Popup(uid, "rmc-xeno-transformation-something-inside", PopupType.MediumCaution);
                break;
            case 5:
                if (!HasComp<RMCUnconsciousComponent>(uid))
                {
                    // TODO RMC14: pain, cm13 apply_pain(PAIN_CHESTBURST_WEAK)
                    _popup.PopupEntity(Loc.GetString("rmc-xeno-transformation-shaking-others", ("target", uid)), uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);
                    Popup(uid, "rmc-xeno-transformation-shaking-self", PopupType.LargeCaution);
                    _sizeStun.TryKnockOut(uid, TimeSpan.FromSeconds(6));
                    _jitteriness.MakeJittery(uid, 105);
                    Brute(uid, 1);
                }

                if (_random.Prob(0.05f))
                    Whisper(uid, _random.Pick(Stage5Whispers));

                if (_random.Prob(0.1f))
                {
                    Popup(uid, "rmc-xeno-transformation-calloused", PopupType.MediumCaution);
                    // TODO RMC14: pain, cm13 apply_pain(PAIN_CHESTBURST_WEAK)
                    Popup(uid, _random.Pick(Stage5Chest), PopupType.MediumCaution);
                    var damage = _rmcDamageable.DistributeFreshDamage(ToxinGroup, 10);
                    _damageable.TryChangeDamage(uid, damage, true, interruptsDoAfters: false);
                }

                if (_random.Prob(0.01f))
                    return true;
                break;
        }

        return false;
    }

    private void UpdateCipheringHive(Entity<RMCXenoTransformationComponent> ent)
    {
        if (!_bloodstream.TryGetChemicalSolution(ent, out _, out var solution))
            return;

        foreach (var (reagent, _) in solution.Contents)
        {
            foreach (var property in _reagent.GetProperties(reagent.Prototype))
            {
                if (property.GetType() != typeof(Ciphering))
                    continue;

                if (Ciphering.GetHiveBySlot(EntityManager, (int) property.Potency) is { } hive)
                    ent.Comp.Hive = hive;
            }
        }
    }

    private void GrantHivemind(Entity<RMCXenoTransformationComponent> ent)
    {
        var uid = ent.Owner;
        _dazed.TryDaze(uid, TimeSpan.FromSeconds(6), true);
        Popup(uid, "rmc-xeno-transformation-mind-tears", PopupType.LargeCaution);

        var understanding = EnsureComp<RMCUniversalUnderstandingComponent>(uid);
        understanding.ExpiresAt = TimeSpan.MaxValue;

        foreach (var language in new List<ProtoId<LanguagePrototype>>(_language.GetSpokenLanguages(uid)))
        {
            if (language != XenoLanguage)
                _language.RemoveLanguage(uid, language, removeUnderstood: false);
        }

        _language.AddLanguage(uid, XenoLanguage);
        _language.SetLanguage(uid, XenoLanguage);

        if (!HasComp<IntrinsicRadioTransmitterComponent>(uid))
            AddComp(uid, new IntrinsicRadioTransmitterComponent { Channels = new HashSet<string>() });

        Comp<IntrinsicRadioTransmitterComponent>(uid).Channels.Add(SharedChatSystem.HivemindChannel.Id);
        EnsureComp<ActiveRadioComponent>(uid).Channels.Add(SharedChatSystem.HivemindChannel.Id);
        EnsureComp<RMCHumanHivemindComponent>(uid);

        // TODO RMC14: test this
        if (GetHive(ent) is { } hive)
            _hive.SetHive(uid, hive);
    }

    private void Alienize(Entity<RMCXenoTransformationComponent> ent)
    {
        var uid = ent.Owner;
        if (TerminatingOrDeleted(uid))
            return;

        var xeno = Spawn(_random.Pick(ent.Comp.Castes), _transform.GetMoverCoordinates(uid));
        _hive.SetHive(xeno, GetHive(ent));

        if (_mind.TryGetMind(uid, out var mindId, out _))
        {
            _mind.TransferTo(mindId, xeno);
            _mind.UnVisit(mindId);
        }

        _popup.PopupEntity(Loc.GetString("rmc-xeno-transformation-perfection"), xeno, xeno, PopupType.Large);

        _gib.ScatterInventoryItems(uid);
        _body.GibBody(uid);
    }

    private EntityUid? GetHive(Entity<RMCXenoTransformationComponent> ent)
    {
        if (ent.Comp.Hive is { } hive && !TerminatingOrDeleted(hive) && HasComp<HiveComponent>(hive))
            return hive;

        var rules = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (rules.MoveNext(out var rule))
        {
            if (HasComp<HiveComponent>(rule.Hive))
                return rule.Hive;
        }

        return null;
    }

    private void Popup(EntityUid uid, LocId message, PopupType type)
    {
        _popup.PopupEntity(Loc.GetString(message), uid, uid, type);
    }

    private void Whisper(EntityUid uid, LocId message)
    {
        _chat.TrySendInGameICMessage(uid, Loc.GetString(message), InGameICChatType.Whisper, false);
    }

    private void Brute(EntityUid uid, float amount)
    {
        var damage = _rmcDamageable.DistributeFreshDamage(BruteGroup, amount);
        _damageable.TryChangeDamage(uid, damage, true, interruptsDoAfters: false);
    }
}
