using Content.Server.Interaction;
using Content.Shared.Interaction;
using Content.Shared._RMC14.Comms;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._RMC14.Comms;

public sealed class EncryptionCoderSystem : EntitySystem
{
    [Dependency] private readonly SharedCommsEncryptionSystem _encryption = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly string[] ChallengePhrases = [
        "WEYLAND", "_YUTANI", "COMPANY", "ALMAYER", "GENESIS", "SCIENCE", "ANDROID",
        "WHISKEY", "CHARLIE", "FOXTROT", "JULIETT", "MARINES", "TRACTOR", "UNIFORM",
        "RAIDERS", "ROSETTA", "SCANNER", "SHADOWS", "SHUTTLE", "TACHYON", "WARSHIP", "ROSTOCK"
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EncryptionCoderComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EncryptionCoderComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);
        SubscribeLocalEvent<EncryptionCoderComputerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<EncryptionCoderComputerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);

        Subs.BuiEvents<EncryptionCoderComputerComponent>(EncryptionCoderComputerUI.Key,
            subs =>
            {
                subs.Event<EncryptionCoderComputerSubmitCodeMsg>(OnSubmitCode);
                subs.Event<EncryptionCoderComputerQuickRestoreMsg>(OnQuickRestore);
                subs.Event<EncryptionCoderComputerPrintMsg>(OnPrint);
                subs.Event<EncryptionCoderComputerRefillMsg>(OnRefill);
                subs.Event<EncryptionCoderComputerGenerateMsg>(OnGenerate);
                subs.Event<EncryptionCoderChangeOffsetMsg>(OnChangeOffset);
            });
    }

    private void OnBUIOpened(Entity<EncryptionCoderComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateCoderState(ent);
    }

    private void OnActivate(Entity<EncryptionCoderComputerComponent> ent, ref ActivateInWorldEvent args)
    {
        _ui.TryOpenUi(ent.Owner, EncryptionCoderComputerUI.Key, args.User);
    }

    private void OnMapInit(Entity<EncryptionCoderComputerComponent> ent, ref MapInitEvent args)
    {
        UpdateCoderState(ent);
    }


    public void SubmitCode(Entity<EncryptionCoderComputerComponent> ent, string code)
    {
        CommsEncryptionComponent? encryptionComp = null;
        if (!string.IsNullOrEmpty(ent.Comp.CurrentWord))
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
            var decoderQuery = EntityQueryEnumerator<DecoderComputerComponent>();
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
            else
            {
                _encryption.RestoreClarity((uid, encryptionComp), false);
            }
        }
        else
        {
            // Old logic for backward compatibility
            ent.Comp.LastSubmittedCode = code.ToUpper();

            // Find the encryption component
            var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
            if (!encryptionQuery.MoveNext(out _, out encryptionComp))
                return;

            // Check if challenge has expired
            var decoderQuery = EntityQueryEnumerator<DecoderComputerComponent>();
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
                _encryption.RestoreClarity((ent.Owner, encryptionComp), true);
                pongDisplay = pongChallenge;
            }

            ent.Comp.LastSubmittedCode = $"{_loc.GetString("rmc-ui-coder-ping-arrow")} {_loc.GetString("rmc-ui-coder-pong-colon")} {pongDisplay}";
        }

        if (encryptionComp != null)
        {
            ent.Comp.ClarityDescription = _encryption.GetClarityDescription(encryptionComp);
        }
        Dirty(ent);
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

    public bool QuickRestore(Entity<EncryptionCoderComputerComponent> ent)
    {
        // Find the encryption component
        var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
        if (!encryptionQuery.MoveNext(out var uid, out var encryptionComp))
        {
            return false;
        }

        // Quick restore +5%
        _encryption.RestoreClarity((uid, encryptionComp), false);

        return true;
    }

    private void GenerateNewWord(Entity<EncryptionCoderComputerComponent> ent)
    {
        ent.Comp.CurrentWord = "PONG";
        ent.Comp.CurrentOffset = 0;
        Dirty(ent);
    }

    private string GetPongDisplay(string submitted, string challenge)
    {
        var minLen = Math.Min(submitted.Length, challenge.Length);
        var result = "";
        for (var i = 0; i < minLen; i++)
        {
            if (submitted[i] == challenge[i])
                result += challenge[i];
            else
                result += "?";
        }
        return result;
    }

    private void OnChangeOffset(Entity<EncryptionCoderComputerComponent> ent, ref EncryptionCoderChangeOffsetMsg args)
    {
        ent.Comp.CurrentOffset = (ent.Comp.CurrentOffset + args.Delta + 26) % 26;
        UpdateCoderState(ent);
    }

    private void OnSubmitCode(Entity<EncryptionCoderComputerComponent> ent, ref EncryptionCoderComputerSubmitCodeMsg args)
    {
        SubmitCode(ent, args.Code);
        UpdateCoderState(ent);
    }

    private void OnQuickRestore(Entity<EncryptionCoderComputerComponent> ent, ref EncryptionCoderComputerQuickRestoreMsg args)
    {
        QuickRestore(ent);
        UpdateCoderState(ent);
    }

    private void OnPrint(Entity<EncryptionCoderComputerComponent> ent, ref EncryptionCoderComputerPrintMsg args)
    {
        if (ent.Comp.PunchcardCount <= 0)
        {
            // TODO: status message
            UpdateCoderState(ent);
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
        UpdateCoderState(ent);
    }

    private void OnRefill(Entity<EncryptionCoderComputerComponent> ent, ref EncryptionCoderComputerRefillMsg args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage))
        {
            UpdateCoderState(ent);
            return;
        }

        foreach (var item in storage.Container.ContainedEntities)
        {
            if (TryComp<PunchcardStackComponent>(item, out var stack))
            {
                ent.Comp.PunchcardCount += stack.Count;
                EntityManager.QueueDeleteEntity(item);
                Dirty(ent);
                break;
            }
        }

        UpdateCoderState(ent);
    }

    private void OnGenerate(Entity<EncryptionCoderComputerComponent> ent, ref EncryptionCoderComputerGenerateMsg args)
    {
        GenerateNewWord(ent);
        UpdateCoderState(ent);
    }

    private void UpdateCoderState(Entity<EncryptionCoderComputerComponent> ent)
    {
        var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
        if (!encryptionQuery.MoveNext(out _, out var encryptionComp))
            return;

        ent.Comp.KnownLetters = _encryption.GetKnownPongLetters(encryptionComp);
        ent.Comp.ClarityDescription = _encryption.GetClarityDescription(encryptionComp);

        var state = new EncryptionCoderComputerBuiState(
            ent.Comp.LastSubmittedCode,
            ent.Comp.KnownLetters,
            ent.Comp.ClarityDescription,
            ent.Comp.CurrentWord,
            ent.Comp.CurrentOffset
        );
        _ui.SetUiState(ent.Owner, EncryptionCoderComputerUI.Key, state);
        Dirty(ent);
    }

    private void OnEntInserted(Entity<EncryptionCoderComputerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage) || args.Container != storage.Container)
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
                // Old logic for hex codes
                SubmitCode(ent, punchcard.Data);
            }
            UpdateCoderState(ent);
        }
    }
}
