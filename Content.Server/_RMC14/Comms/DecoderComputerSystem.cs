using Content.Shared._RMC14.Comms;
using Content.Shared.Storage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Comms;

public sealed class DecoderComputerSystem : EntitySystem
{
    [Dependency] private readonly SharedCommsEncryptionSystem _encryption = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DecoderComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DecoderComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);

        Subs.BuiEvents<DecoderComputerComponent>(DecoderComputerUI.Key,
            subs =>
            {
                subs.Event<DecoderComputerSubmitCodeMsg>(OnSubmitCode);
                subs.Event<DecoderComputerQuickRestoreMsg>(OnQuickRestore);
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

    private void OnQuickRestore(Entity<DecoderComputerComponent> ent, ref DecoderComputerQuickRestoreMsg args)
    {
        QuickRestore(ent);
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
        if (submittedCode.ToUpper() != ent.Comp.CurrentChallengeCode.ToUpper())
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

    public bool QuickRestore(Entity<DecoderComputerComponent> ent)
    {
        // Find the encryption component
        var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
        if (!encryptionQuery.MoveNext(out var uid, out var encryptionComp))
        {
            ent.Comp.StatusMessage = "No encryption system found.";
            Dirty(ent);
            return false;
        }

        // Quick restore +5%
        _encryption.RestoreClarity((uid, encryptionComp), false);
        ent.Comp.StatusMessage = "Quick restoration applied. +5% clarity.";
        Dirty(ent);

        return true;
    }

    private void GenerateNewChallenge(Entity<DecoderComputerComponent> ent)
    {
        // Generate a random 8-character hex code
        var chars = "0123456789ABCDEF";
        var random = new Random();
        var code = "";
        for (var i = 0; i < 8; i++)
        {
            code += chars[random.Next(chars.Length)];
        }

        ent.Comp.CurrentChallengeCode = code;
        ent.Comp.StatusMessage = "Ready for decode";
        ent.Comp.HasGracePeriod = false;
        Dirty(ent);
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
