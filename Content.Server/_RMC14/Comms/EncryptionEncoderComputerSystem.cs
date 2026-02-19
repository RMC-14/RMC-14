using Content.Server.Interaction;
using Content.Shared.Interaction;
using Content.Shared._RMC14.Comms;
using Content.Shared.Storage;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._RMC14.Comms;

public sealed class EncryptionEncoderComputerSystem : EntitySystem
{
    [Dependency] private readonly SharedCommsEncryptionSystem _encryption = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private static readonly string[] ChallengePhrases = [
        "WEYLAND", "_YUTANI", "COMPANY", "ALMAYER", "GENESIS", "SCIENCE", "ANDROID",
        "WHISKEY", "CHARLIE", "FOXTROT", "JULIETT", "MARINES", "TRACTOR", "UNIFORM",
        "RAIDERS", "ROSETTA", "SCANNER", "SHADOWS", "SHUTTLE", "TACHYON", "WARSHIP", "ROSTOCK"
    ];

    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EncryptionEncoderComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EncryptionEncoderComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);
        SubscribeLocalEvent<EncryptionEncoderComputerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<EncryptionEncoderComputerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EncryptionEncoderComputerComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<EncryptionEncoderComputerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<EncryptionEncoderComputerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        Subs.BuiEvents<EncryptionEncoderComputerComponent>(EncryptionEncoderComputerUI.Key,
            subs =>
            {
                subs.Event<EncryptionEncoderComputerSubmitCodeMsg>(OnSubmitCode);
                subs.Event<EncryptionEncoderComputerPrintMsg>(OnPrint);
                subs.Event<EncryptionEncoderComputerRefillMsg>(OnRefill);
                subs.Event<EncryptionEncoderComputerGenerateMsg>(OnGenerate);
                subs.Event<EncryptionEncoderChangeOffsetMsg>(OnChangeOffset);
            });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < 10f)
            return;

        _accumulator = 0f;

        var query = EntityQueryEnumerator<EncryptionEncoderComputerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_ui.IsUiOpen(uid, EncryptionEncoderComputerUI.Key))
                UpdateEncoderState((uid, comp));
        }
    }

    private void OnComponentInit(EntityUid uid, EncryptionEncoderComputerComponent component, ComponentInit args)
    {
        const string SlotId = "punchcard_slot";
        _itemSlotsSystem.AddItemSlot(uid, SlotId, component.PunchcardSlot);
    }

    private void OnComponentRemove(EntityUid uid, EncryptionEncoderComputerComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PunchcardSlot);
    }

    private void OnEntRemoved(EntityUid uid, EncryptionEncoderComputerComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.PunchcardSlot.ID)
            return;

        component.CurrentWord = "";
        component.CurrentOffset = 0;
        UpdateEncoderState((uid, component));
    }

    private void OnMapInit(Entity<EncryptionEncoderComputerComponent> ent, ref MapInitEvent args)
    {
        UpdateEncoderState(ent);
    }

    private void OnBUIOpened(Entity<EncryptionEncoderComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateEncoderState(ent);
    }

    private void OnActivate(Entity<EncryptionEncoderComputerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _ui.TryOpenUi(ent.Owner, EncryptionEncoderComputerUI.Key, args.User);
    }

    public void SubmitCode(Entity<EncryptionEncoderComputerComponent> ent, string code)
    {
        CommsEncryptionComponent? encryptionComp = null;
        if (!string.IsNullOrEmpty(ent.Comp.CurrentHex))
        {
            var decoded = DecodeHexWithOffset(ent.Comp.CurrentHex, ent.Comp.CurrentOffset);
            var isKnown = ChallengePhrases.Contains(decoded.ToUpper());

            var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
            if (!encryptionQuery.MoveNext(out var uid, out encryptionComp))
            {
                ent.Comp.LastSubmittedCode = $"{_loc.GetString("rmc-ui-coder-ping-arrow")} {_loc.GetString("rmc-ui-decoder-no-encryption-system")}";
                Dirty(ent);
                return;
            }

            var knownLetters = _encryption.GetKnownPongLetters(encryptionComp);

            var decoderQuery = EntityQueryEnumerator<EncryptionDecoderComputerComponent>();
            if (!decoderQuery.MoveNext(out _, out var decoderComp) || _timing.CurTime >= decoderComp.ChallengeExpiry)
            {
                var pongChallengeExpired = "PONG";
                var pongDisplayExpired = "";
                for (var i = 0; i < pongChallengeExpired.Length; i++)
                {
                    pongDisplayExpired += i < knownLetters ? pongChallengeExpired[i] : '?';
                }
                ent.Comp.LastSubmittedCode = $"{_loc.GetString("rmc-ui-coder-ping-arrow")} {pongDisplayExpired}";
                ent.Comp.KnownLetters = knownLetters;
                Dirty(ent);
                return;
            }

            var pongChallenge = "PONG";
            var pong = isKnown ? pongChallenge : new string('?', pongChallenge.Length);
            ent.Comp.LastSubmittedCode = $"{_loc.GetString("rmc-ui-coder-ping-arrow")} {pong}";
            ent.Comp.KnownLetters = isKnown ? pongChallenge.Length : 0;

            if (isKnown)
            {
                _encryption.RestoreClarity((uid, encryptionComp), true);
            }
        }
        else if (!string.IsNullOrEmpty(ent.Comp.CurrentWord))
        {
            var shifted = ShiftWord(ent.Comp.CurrentWord, ent.Comp.CurrentOffset);
            var hex = ToHexCodes(shifted);

            // Find the encryption component
            var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
            if (!encryptionQuery.MoveNext(out var uid, out encryptionComp))
            {
                ent.Comp.LastSubmittedCode = $"{_loc.GetString("rmc-ui-coder-ping-arrow")} {_loc.GetString("rmc-ui-decoder-no-encryption-system")}";
                Dirty(ent);
                return;
            }

            var knownLetters = _encryption.GetKnownPongLetters(encryptionComp);

            // Check if challenge has expired
            var decoderQuery = EntityQueryEnumerator<EncryptionDecoderComputerComponent>();
            if (!decoderQuery.MoveNext(out _, out var decoderComp) || _timing.CurTime >= decoderComp.ChallengeExpiry)
            {
                var pongChallengeExpired = "PONG";
                var pongDisplayExpired = "";
                for (var i = 0; i < pongChallengeExpired.Length; i++)
                {
                    pongDisplayExpired += i < knownLetters ? pongChallengeExpired[i] : '?';
                }
                ent.Comp.LastSubmittedCode = $"{_loc.GetString("rmc-ui-coder-ping-arrow")} {pongDisplayExpired}";
                ent.Comp.KnownLetters = knownLetters;
                Dirty(ent);
                return;
            }

            var pongChallenge = "PONG";
            var pong = ChallengePhrases.Contains(ent.Comp.CurrentWord.ToUpper()) ? pongChallenge : new string('?', pongChallenge.Length);
            ent.Comp.LastSubmittedCode = $"{_loc.GetString("rmc-ui-coder-ping-arrow")} {pong}";
            ent.Comp.KnownLetters = ChallengePhrases.Contains(ent.Comp.CurrentWord.ToUpper()) ? pongChallenge.Length : 0;

            if (ChallengePhrases.Contains(ent.Comp.CurrentWord.ToUpper()))
            {
                _encryption.RestoreClarity((uid, encryptionComp), true);
            }
        }
        else
        {
            // Old logic for backward compatibility
            ent.Comp.LastSubmittedCode = code.ToUpper();

            // Find the encryption component
            var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
            if (!encryptionQuery.MoveNext(out var uid, out encryptionComp))
                return;

            // Check if challenge has expired
            var decoderQuery = EntityQueryEnumerator<EncryptionDecoderComputerComponent>();
            if (!decoderQuery.MoveNext(out _, out var decoderComp) || _timing.CurTime >= decoderComp.ChallengeExpiry)
            {
                var knownLettersExpired = _encryption.GetKnownPongLetters(encryptionComp);
                var pongChallengeExpired = "PONG";
                var pongDisplayExpired = "";
                for (var i = 0; i < pongChallengeExpired.Length; i++)
                {
                    pongDisplayExpired += i < knownLettersExpired ? pongChallengeExpired[i] : "?";
                }
                ent.Comp.LastSubmittedCode = $"{_loc.GetString("rmc-ui-coder-ping-arrow")} {_loc.GetString("rmc-ui-coder-pong-colon")} {pongDisplayExpired}";
                ent.Comp.KnownLetters = knownLettersExpired;
                Dirty(ent);
                return;
            }

            var knownLetters = _encryption.GetKnownPongLetters(encryptionComp);
            ent.Comp.KnownLetters = knownLetters;

            // Create the display string
            var pongDisplay = "";
            var pongChallenge = "PONG";
            for (var i = 0; i < pongChallenge.Length; i++)
            {
                if (i < knownLetters)
                    pongDisplay += pongChallenge[i];
                else
                    pongDisplay += "?";
            }

            if (code.ToUpper() == encryptionComp.ChallengePhrase.ToUpper())
            {
                _encryption.RestoreClarity((uid, encryptionComp), true);
                pongDisplay = pongChallenge;
            }

            ent.Comp.LastSubmittedCode = $"{_loc.GetString("rmc-ui-coder-ping-arrow")} {_loc.GetString("rmc-ui-coder-pong-colon")} {pongDisplay}";
        }

    }

    private string ShiftWord(string word, int offset)
    {
        var result = "";
        foreach (var c in word)
        {
            if (char.IsUpper(c))
            {
                var shifted = (c - 'A' + offset) % 26 + 'A';
                result += (char)shifted;
            }
            else
            {
                result += c;
            }
        }
        return result;
    }

    private string ToHexCodes(string s)
    {
        return string.Join(" ", s.Select(c => $"0x{(int)c:X2}"));
    }

    private string DecodeHexWithOffset(string hexCode, int offset)
    {
        if (string.IsNullOrEmpty(hexCode))
            return string.Empty;

        var parts = hexCode.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var text = "";
        foreach (var part in parts)
        {
            if (part.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase) &&
                int.TryParse(part[2..], System.Globalization.NumberStyles.HexNumber, null, out var val))
            {
                text += (char)val;
            }
        }

        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var result = "";
        foreach (var c in text)
        {
            if (char.IsLetter(c))
            {
                var baseChar = char.IsUpper(c) ? 'A' : 'a';
                var shift = (c - baseChar - offset) % 26;
                if (shift < 0) shift += 26;
                var shifted = (char)(baseChar + shift);
                result += shifted;
            }
            else
            {
                result += c;
            }
        }

        return result;
    }

    private void GenerateNewWord(Entity<EncryptionEncoderComputerComponent> ent)
    {
        ent.Comp.CurrentWord = "PONG";
        ent.Comp.CurrentOffset = 0;
        Dirty(ent);
    }

    private void OnChangeOffset(Entity<EncryptionEncoderComputerComponent> ent, ref EncryptionEncoderChangeOffsetMsg args)
    {
        ent.Comp.CurrentOffset = (ent.Comp.CurrentOffset + args.Delta + 26) % 26;
        UpdateEncoderState(ent);
    }

    private void OnSubmitCode(Entity<EncryptionEncoderComputerComponent> ent, ref EncryptionEncoderComputerSubmitCodeMsg args)
    {
        SubmitCode(ent, args.Code);
        UpdateEncoderState(ent);
    }

    private void OnPrint(Entity<EncryptionEncoderComputerComponent> ent, ref EncryptionEncoderComputerPrintMsg args)
    {
        if (ent.Comp.PunchcardCount <= 0)
        {
            UpdateEncoderState(ent);
            return;
        }

        var punchcard = EntityManager.SpawnEntity("RMCPunchcard", Transform(ent).Coordinates);
        if (TryComp<PunchcardComponent>(punchcard, out var punchComp))
        {
            punchComp.Data = ToHexCodes(ShiftWord(ent.Comp.CurrentWord, ent.Comp.CurrentOffset));
            Dirty(punchcard, punchComp);
        }

        ent.Comp.PunchcardCount--;
        Dirty(ent);
        UpdateEncoderState(ent);
    }

    private void OnRefill(Entity<EncryptionEncoderComputerComponent> ent, ref EncryptionEncoderComputerRefillMsg args)
    {
        var item = ent.Comp.PunchcardSlot.Item;
        if (item == null || !TryComp<StackComponent>(item, out var stack))
        {
            if (_hands.TryGetActiveItem(args.Actor, out var held) &&
                held != null &&
                TryComp<StackComponent>(held.Value, out var heldStack))
            {
                ent.Comp.PunchcardCount += heldStack.Count;

                _hands.TryDrop((args.Actor, CompOrNull<HandsComponent>(args.Actor)),
                    held.Value,
                    checkActionBlocker: false,
                    doDropInteraction: false);
                EntityManager.QueueDeleteEntity(held.Value);

                Dirty(ent);
                UpdateEncoderState(ent);
                return;
            }

            UpdateEncoderState(ent);
            return;
        }

        ent.Comp.PunchcardCount += stack.Count;
        EntityManager.QueueDeleteEntity(item.Value);
        Dirty(ent);

        UpdateEncoderState(ent);
    }

    private void OnGenerate(Entity<EncryptionEncoderComputerComponent> ent, ref EncryptionEncoderComputerGenerateMsg args)
    {
        GenerateNewWord(ent);
        UpdateEncoderState(ent);
    }

    private void UpdateEncoderState(Entity<EncryptionEncoderComputerComponent> ent)
    {
        var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
        if (!encryptionQuery.MoveNext(out _, out var encryptionComp))
            return;

        ent.Comp.KnownLetters = _encryption.GetKnownPongLetters(encryptionComp);

        var state = new EncryptionEncoderComputerBuiState(
            ent.Comp.LastSubmittedCode,
            ent.Comp.KnownLetters,
            ent.Comp.CurrentWord,
            ent.Comp.CurrentOffset,
            ent.Comp.CurrentHex
        );
        _ui.SetUiState(ent.Owner, EncryptionEncoderComputerUI.Key, state);
        Dirty(ent);
    }

    private void OnEntInserted(Entity<EncryptionEncoderComputerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.PunchcardSlot.ID)
            return;

        if (TryComp<PunchcardComponent>(args.Entity, out var punchcard))
        {
            if (punchcard.Data.Contains(':'))
            {
                var parts = punchcard.Data.Split(':');
                if (parts.Length >= 2 && int.TryParse(parts[1], out var offset))
                {
                    ent.Comp.CurrentWord = parts[0].ToUpper();
                    ent.Comp.CurrentOffset = offset;
                }
                else
                {
                    ent.Comp.CurrentWord = punchcard.Data.ToUpper();
                }
            }
            else if (punchcard.Data.All(c => char.IsLetter(c) || c == '_'))
            {
                ent.Comp.CurrentWord = punchcard.Data.ToUpper();
            }
            else
            {
                ent.Comp.CurrentHex = punchcard.Data;
            }
        }

        UpdateEncoderState(ent);
    }
}

