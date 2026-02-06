using Content.Shared._RMC14.Comms;
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

        Subs.BuiEvents<EncryptionCoderComputerComponent>(EncryptionCoderComputerUI.Key,
            subs =>
            {
                subs.Event<EncryptionCoderSubmitCodeMsg>(OnSubmitCode);
            });
    }

    private void OnBUIOpened(Entity<EncryptionCoderComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateCoderState(ent);
    }

    private void OnMapInit(Entity<EncryptionCoderComputerComponent> ent, ref MapInitEvent args)
    {
        UpdateCoderState(ent);
    }

    private void OnSubmitCode(Entity<EncryptionCoderComputerComponent> ent, ref EncryptionCoderSubmitCodeMsg args)
    {
        SubmitCode(ent, args.Code);
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

        ent.Comp.LastSubmittedCode = $"{code} -> {pongDisplay}";
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
}
