using Content.Shared._RMC14.Comms;
using Content.Shared.Storage;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._RMC14.Comms;

public sealed class DecoderComputerSystem : EntitySystem
{
    [Dependency] private readonly SharedCommsEncryptionSystem _encryption = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly string[] ChallengePhrases = [
        "WEYLAND", "_YUTANI", "COMPANY", "ALMAYER", "GENESIS", "SCIENCE", "ANDROID",
        "WHISKEY", "CHARLIE", "FOXTROT", "JULIETT", "MARINES", "TRACTOR", "UNIFORM",
        "RAIDERS", "ROSETTA", "SCANNER", "SHADOWS", "SHUTTLE", "TACHYON", "WARSHIP", "ROSTOCK"
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DecoderComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DecoderComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);

        Subs.BuiEvents<DecoderComputerComponent>(DecoderComputerUI.Key,
            subs =>
            {
                subs.Event<DecoderComputerSubmitCodeMsg>(OnSubmitCode);
                subs.Event<DecoderComputerPrintMsg>(OnPrint);
                subs.Event<DecoderComputerRefillMsg>(OnRefill);
                subs.Event<DecoderComputerGenerateMsg>(OnGenerate);
            });
    }

    private void OnMapInit(Entity<DecoderComputerComponent> ent, ref MapInitEvent args)
    {
        GenerateNewChallenge(ent);
    }

    private void OnBUIOpened(Entity<DecoderComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateDecoderState(ent);
    }

    private void OnSubmitCode(Entity<DecoderComputerComponent> ent, ref DecoderComputerSubmitCodeMsg args)
    {
        AttemptDecode(ent, args.Code);
        UpdateDecoderState(ent);
    }

    private void OnPrint(Entity<DecoderComputerComponent> ent, ref DecoderComputerPrintMsg args)
    {
        if (ent.Comp.PunchcardCount <= 0)
        {
            ent.Comp.StatusMessage = "No punchcards left.";
            UpdateDecoderState(ent);
            return;
        }

        var punchcard = EntityManager.SpawnEntity("RMCPunchcard", Transform(ent).Coordinates);
        if (TryComp<PunchcardComponent>(punchcard, out var punchComp))
        {
            punchComp.Data = ent.Comp.CurrentChallengeCode;
            Dirty(punchcard, punchComp);

            // Mispunch chance: 1/7
            if (_random.Next(7) == 0)
            {
                var parts = punchComp.Data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    var index = _random.Next(parts.Length);
                    parts[index] = "0x00";
                    punchComp.Data = string.Join(" ", parts);
                    Dirty(punchcard, punchComp);
                }
            }
        }

        ent.Comp.PunchcardCount--;
        ent.Comp.StatusMessage = "Punchcard printed.";
        Dirty(ent);
        UpdateDecoderState(ent);
    }

    private void OnRefill(Entity<DecoderComputerComponent> ent, ref DecoderComputerRefillMsg args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage))
        {
            ent.Comp.StatusMessage = "No storage component.";
            UpdateDecoderState(ent);
            return;
        }

        foreach (var item in storage.Container.ContainedEntities)
        {
            if (TryComp<PunchcardStackComponent>(item, out var stack))
            {
                ent.Comp.PunchcardCount += stack.Count;
                EntityManager.QueueDeleteEntity(item);
                ent.Comp.StatusMessage = $"Refilled with {stack.Count} punchcards.";
                Dirty(ent);
                break;
            }
        }

        UpdateDecoderState(ent);
    }

    private void OnGenerate(Entity<DecoderComputerComponent> ent, ref DecoderComputerGenerateMsg args)
    {
        GenerateNewChallenge(ent);
        UpdateDecoderState(ent);
    }

    public bool AttemptDecode(Entity<DecoderComputerComponent> ent, string submittedCode)
    {
        if (submittedCode.ToUpper() != ent.Comp.CurrentChallengeWord.ToUpper())
        {
            ent.Comp.StatusMessage = "Decode failed. Invalid code.";
            Dirty(ent);
            return false;
        }

        // Find the encryption component
        var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
        if (!encryptionQuery.MoveNext(out var uid, out var encryptionComp))
        {
            ent.Comp.StatusMessage = "No encryption system found.";
            Dirty(ent);
            return false;
        }

        // Restore clarity
        _encryption.RestoreClarity((uid, encryptionComp), true);
        ent.Comp.StatusMessage = "Decode successful. Communications clarity restored.";
        ent.Comp.HasGracePeriod = true;
        ent.Comp.GracePeriodEnd = _timing.CurTime + TimeSpan.FromMinutes(2); // 2 minutes total grace
        Dirty(ent);

        // Generate new challenge
        GenerateNewChallenge(ent);

        return true;
    }

    private void GenerateNewChallenge(Entity<DecoderComputerComponent> ent)
    {
        // Generate a random challenge phrase
        var random = new Random();
        var word = ChallengePhrases[random.Next(ChallengePhrases.Length)];
        var offset = random.Next(26);
        var shifted = ShiftWord(word, offset);
        var hexCodes = ToHexCodes(shifted);

        ent.Comp.CurrentChallengeCode = hexCodes;
        ent.Comp.CurrentChallengeWord = word;
        ent.Comp.ChallengeExpiry = _timing.CurTime + TimeSpan.FromSeconds(30);
        ent.Comp.StatusMessage = "Ready for decode";
        ent.Comp.HasGracePeriod = false;
        Dirty(ent);

        // Set the global challenge phrase
        var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
        if (encryptionQuery.MoveNext(out var uid, out var encryptionComp))
        {
            encryptionComp.ChallengePhrase = hexCodes;
            Dirty(uid, encryptionComp);
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

    private void UpdateDecoderState(Entity<DecoderComputerComponent> ent)
    {
        var state = new DecoderComputerBuiState(
            ent.Comp.CurrentChallengeCode,
            ent.Comp.HasGracePeriod,
            ent.Comp.GracePeriodEnd,
            ent.Comp.StatusMessage,
            ent.Comp.PunchcardCount
        );

        _ui.SetUiState(ent.Owner, DecoderComputerUI.Key, state);
        Dirty(ent);
    }
}
