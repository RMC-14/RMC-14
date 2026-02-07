using Content.Shared._RMC14.Comms;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RMC14.Comms;

public sealed class EncryptionCipherComputerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EncryptionCipherComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EncryptionCipherComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);
        SubscribeLocalEvent<EncryptionCipherComputerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<EncryptionCipherComputerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        Subs.BuiEvents<EncryptionCipherComputerComponent>(EncryptionCipherComputerUI.Key,
            subs =>
            {
                subs.Event<EncryptionCipherChangeSettingMsg>(OnChangeSetting);
                subs.Event<EncryptionCipherPrintMsg>(OnPrint);
                subs.Event<EncryptionCipherRefillMsg>(OnRefill);
            });
    }

    private void OnMapInit(Entity<EncryptionCipherComputerComponent> ent, ref MapInitEvent args)
    {
        UpdateCipherState(ent);
    }

    private void OnBUIOpened(Entity<EncryptionCipherComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateCipherState(ent);
    }

    private void OnSetInput(Entity<EncryptionCipherComputerComponent> ent, ref EncryptionCipherSetInputMsg args)
    {
        ent.Comp.InputCode = args.Code.ToUpper();
        ent.Comp.DecipheredWord = DecipherCode(ent.Comp.InputCode, ent.Comp.CipherSetting);
        ent.Comp.StatusMessage = "Input set. Adjust cipher setting.";
        UpdateCipherState(ent);
    }

    private void OnChangeSetting(Entity<EncryptionCipherComputerComponent> ent, ref EncryptionCipherChangeSettingMsg args)
    {
        ent.Comp.CipherSetting = (ent.Comp.CipherSetting + args.Delta + 27) % 27; // 0-26
        ent.Comp.DecipheredWord = DecipherCode(ent.Comp.InputCode, ent.Comp.CipherSetting);
        UpdateCipherState(ent);
    }

    private void OnPrint(Entity<EncryptionCipherComputerComponent> ent, ref EncryptionCipherPrintMsg args)
    {
        if (ent.Comp.PunchcardCount <= 0)
        {
            ent.Comp.StatusMessage = "No punchcards left.";
            UpdateCipherState(ent);
            return;
        }

        var punchcard = EntityManager.SpawnEntity("RMCPunchcard", Transform(ent).Coordinates);
        if (TryComp<PunchcardComponent>(punchcard, out var punchComp))
        {
            punchComp.Data = ent.Comp.DecipheredWord;
            Dirty(punchcard, punchComp);
        }

        ent.Comp.PunchcardCount--;
        ent.Comp.StatusMessage = "Punchcard printed.";
        Dirty(ent);
        UpdateCipherState(ent);
    }

    private void OnRefill(Entity<EncryptionCipherComputerComponent> ent, ref EncryptionCipherRefillMsg args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage))
        {
            ent.Comp.StatusMessage = "No storage component.";
            UpdateCipherState(ent);
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

        UpdateCipherState(ent);
    }

    private string DecipherCode(string code, int setting)
    {
        if (string.IsNullOrEmpty(code))
            return "";

        var result = "";
        foreach (var c in code)
        {
            if (char.IsLetter(c))
            {
                var baseChar = char.IsUpper(c) ? 'A' : 'a';
                var shifted = (c - baseChar + setting) % 26 + baseChar;
                result += (char)shifted;
            }
            else
            {
                result += c;
            }
        }
        return result;
    }

    private void UpdateCipherState(Entity<EncryptionCipherComputerComponent> ent)
    {
        var state = new EncryptionCipherComputerBuiState(
            ent.Comp.InputCode,
            ent.Comp.CipherSetting,
            ent.Comp.DecipheredWord,
            ent.Comp.StatusMessage,
            ent.Comp.PunchcardCount
        );

        _ui.SetUiState(ent.Owner, EncryptionCipherComputerUI.Key, state);
        Dirty(ent);
    }

    private void OnEntInserted(Entity<EncryptionCipherComputerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage) || args.Container != storage.Container)
            return;

        if (TryComp<PunchcardComponent>(args.Entity, out var punchcard))
        {
            ent.Comp.InputCode = punchcard.Data;
            ent.Comp.DecipheredWord = DecipherCode(ent.Comp.InputCode, ent.Comp.CipherSetting);
            UpdateCipherState(ent);
        }
    }

    private void OnEntRemoved(Entity<EncryptionCipherComputerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage) || args.Container != storage.Container)
            return;

        if (TryComp<PunchcardComponent>(args.Entity, out var punchcard))
        {
            ent.Comp.InputCode = "";
            ent.Comp.DecipheredWord = "";
            UpdateCipherState(ent);
        }
    }
}
