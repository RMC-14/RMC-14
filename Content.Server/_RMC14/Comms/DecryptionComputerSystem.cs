using Content.Shared._RMC14.Comms;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Comms;

public sealed class DecryptionComputerSystem : EntitySystem
{
    [Dependency] private readonly SharedCommsEncryptionSystem _encryption = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DecryptionComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DecryptionComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);

        Subs.BuiEvents<DecryptionComputerComponent>(DecryptionComputerUI.Key,
            subs =>
            {
                subs.Event<DecryptionComputerSubmitCodeMsg>(OnSubmitCode);
                subs.Event<DecryptionComputerQuickRestoreMsg>(OnQuickRestore);
            });
    }

    private void OnMapInit(Entity<DecryptionComputerComponent> ent, ref MapInitEvent args)
    {
        GenerateNewChallenge(ent);
    }

    private void OnBUIOpened(Entity<DecryptionComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateDecryptionState(ent);
    }

    private void OnSubmitCode(Entity<DecryptionComputerComponent> ent, ref DecryptionComputerSubmitCodeMsg args)
    {
        AttemptDecryption(ent, args.Code);
        UpdateDecryptionState(ent);
    }

    private void OnQuickRestore(Entity<DecryptionComputerComponent> ent, ref DecryptionComputerQuickRestoreMsg args)
    {
        QuickRestore(ent);
        UpdateDecryptionState(ent);
    }

    public bool AttemptDecryption(Entity<DecryptionComputerComponent> ent, string submittedCode)
    {
        if (submittedCode.ToUpper() != ent.Comp.CurrentChallengeCode.ToUpper())
        {
            ent.Comp.StatusMessage = "Decryption failed. Invalid code.";
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
        ent.Comp.StatusMessage = "Decryption successful. Communications clarity restored.";
        ent.Comp.HasGracePeriod = true;
        ent.Comp.GracePeriodEnd = _timing.CurTime + TimeSpan.FromMinutes(2); // 2 minutes total grace
        Dirty(ent);

        // Generate new challenge
        GenerateNewChallenge(ent);

        return true;
    }

    public bool QuickRestore(Entity<DecryptionComputerComponent> ent)
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

    private void GenerateNewChallenge(Entity<DecryptionComputerComponent> ent)
    {
        // Generate a random 8-character code with letters and numbers
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = "";
        for (var i = 0; i < 8; i++)
        {
            code += chars[random.Next(chars.Length)];
        }

        ent.Comp.CurrentChallengeCode = code;
        ent.Comp.StatusMessage = "Ready for decryption";
        ent.Comp.HasGracePeriod = false;
        Dirty(ent);
    }

    private void UpdateDecryptionState(Entity<DecryptionComputerComponent> ent)
    {
        var state = new DecryptionComputerBuiState(
            ent.Comp.CurrentChallengeCode,
            ent.Comp.HasGracePeriod,
            ent.Comp.GracePeriodEnd,
            ent.Comp.StatusMessage
        );

        _ui.SetUiState(ent.Owner, DecryptionComputerUI.Key, state);
        Dirty(ent);
    }
}
