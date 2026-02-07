using Content.Shared._RMC14.Comms;
using Content.Shared.Storage;
using Content.Shared._RMC14.Comms;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RMC14.Comms;

public sealed class EncryptionCoderSystem : EntitySystem
{
    [Dependency] private readonly SharedCommsEncryptionSystem _encryption = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EncryptionCoderComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EncryptionCoderComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);
        SubscribeLocalEvent<EncryptionCoderComputerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
    }

    private void OnBUIOpened(Entity<EncryptionCoderComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateCoderState(ent);
    }

    private void OnMapInit(Entity<EncryptionCoderComputerComponent> ent, ref MapInitEvent args)
    {
        UpdateCoderState(ent);
    }


    public void SubmitCode(Entity<EncryptionCoderComputerComponent> ent, string code)
    {
        ent.Comp.LastSubmittedCode = code.ToUpper();

        // Find the encryption component (should be global)
        var encryptionQuery = EntityQueryEnumerator<CommsEncryptionComponent>();
        if (!encryptionQuery.MoveNext(out _, out var encryptionComp))
            return;

        var knownLetters = _encryption.GetKnownPongLetters(encryptionComp);
        ent.Comp.KnownLetters = knownLetters;

        // Create the display string showing known letters of PONG
        var pongDisplay = "";
        var pong = encryptionComp.ChallengePhrase;
        for (var i = 0; i < pong.Length; i++)
        {
            if (i < knownLetters)
                pongDisplay += pong[i];
            else
                pongDisplay += "?";
        }

        if (code.ToUpper() == encryptionComp.ChallengePhrase.ToUpper())
        {
            _encryption.RestoreClarity((ent.Owner, encryptionComp), true);
            pongDisplay = pong; // full
        }

        ent.Comp.LastSubmittedCode = $"PING -> PONG: {pongDisplay}";
        ent.Comp.ClarityDescription = _encryption.GetClarityDescription(encryptionComp);

        Dirty(ent);
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
            ent.Comp.ClarityDescription
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
            SubmitCode(ent, punchcard.Data);
            UpdateCoderState(ent);
        }
    }
}
