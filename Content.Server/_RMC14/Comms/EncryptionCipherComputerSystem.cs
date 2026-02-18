using Content.Server.Interaction;
using Content.Shared.Interaction;
using Content.Shared._RMC14.Comms;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System.Linq;

namespace Content.Server._RMC14.Comms;

public sealed class EncryptionCipherComputerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;


    private static readonly string[] ChallengePhrases = [
        "WEYLAND", "_YUTANI", "COMPANY", "ALMAYER", "GENESIS", "SCIENCE", "ANDROID",
        "WHISKEY", "CHARLIE", "FOXTROT", "JULIETT", "MARINES", "TRACTOR", "UNIFORM",
        "RAIDERS", "ROSETTA", "SCANNER", "SHADOWS", "SHUTTLE", "TACHYON", "WARSHIP", "ROSTOCK"
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EncryptionCipherComputerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EncryptionCipherComputerComponent, BoundUIOpenedEvent>(OnBUIOpened);
        SubscribeLocalEvent<EncryptionCipherComputerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<EncryptionCipherComputerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EncryptionCipherComputerComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<EncryptionCipherComputerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<EncryptionCipherComputerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        Subs.BuiEvents<EncryptionCipherComputerComponent>(EncryptionCipherComputerUI.Key,
            subs =>
            {
                subs.Event<EncryptionCipherChangeSettingMsg>(OnChangeSetting);
                subs.Event<EncryptionCipherPrintMsg>(OnPrint);
                subs.Event<EncryptionCipherRefillMsg>(OnRefill);
                subs.Event<EncryptionCipherShiftLetterMsg>(OnShiftLetter);
            });
    }

    private void OnComponentInit(EntityUid uid, EncryptionCipherComputerComponent component, ComponentInit args)
    {
        const string SlotId = "punchcard_slot";
        _itemSlotsSystem.AddItemSlot(uid, SlotId, component.PunchcardSlot);
    }

    private void OnComponentRemove(EntityUid uid, EncryptionCipherComputerComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PunchcardSlot);
    }

    private void OnEntInserted(EntityUid uid, EncryptionCipherComputerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.PunchcardSlot.ID)
            return;

        var item = component.PunchcardSlot.Item;
        if (item != null && TryComp<PunchcardComponent>(item, out var punchcard))
        {
            component.InputCode = punchcard.Data;
            component.DecipheredWord = DecipherCode(component.InputCode, component.CipherSetting);
        }
        else
        {
            component.InputCode = "";
            component.DecipheredWord = "";
        }

        UpdateCipherState((uid, component));
    }

    private void OnEntRemoved(EntityUid uid, EncryptionCipherComputerComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.PunchcardSlot.ID)
            return;

        component.InputCode = "";
        component.DecipheredWord = "";
        UpdateCipherState((uid, component));
    }

    private void OnMapInit(Entity<EncryptionCipherComputerComponent> ent, ref MapInitEvent args)
    {
        UpdateCipherState(ent);
    }

    private void OnBUIOpened(Entity<EncryptionCipherComputerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateCipherState(ent);
    }

    private void OnActivate(Entity<EncryptionCipherComputerComponent> ent, ref ActivateInWorldEvent args)
    {
        _ui.TryOpenUi(ent.Owner, EncryptionCipherComputerUI.Key, args.User);
    }

    private void OnSetInput(Entity<EncryptionCipherComputerComponent> ent, ref EncryptionCipherSetInputMsg args)
    {
        ent.Comp.InputCode = args.Code.ToUpper();
        ent.Comp.DecipheredWord = DecipherCode(ent.Comp.InputCode, ent.Comp.CipherSetting);
        ent.Comp.StatusMessage = _loc.GetString("rmc-ui-cipher-input-set");
        UpdateCipherState(ent);
    }

    private void OnChangeSetting(Entity<EncryptionCipherComputerComponent> ent, ref EncryptionCipherChangeSettingMsg args)
    {
        ent.Comp.CipherSetting = (ent.Comp.CipherSetting + args.Delta + 27) % 27; // 0-26
        ent.Comp.DecipheredWord = DecipherCode(ent.Comp.InputCode, ent.Comp.CipherSetting);
        UpdateCipherState(ent);
    }

    private void OnShiftLetter(Entity<EncryptionCipherComputerComponent> ent, ref EncryptionCipherShiftLetterMsg args)
    {
        if (string.IsNullOrEmpty(ent.Comp.InputCode))
            return;

        var parts = ent.Comp.InputCode.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (args.Index < 0 || args.Index >= parts.Count)
            return;

        // Parse hex like 0x41
        var part = parts[args.Index];
        if (!part.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            return;

        if (!int.TryParse(part.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var value))
            return;

        // Only shift uppercase A-Z
        if (value < 'A' || value > 'Z')
            return;

        var range = 26;
        var baseChar = 'A';
        var offset = (value - baseChar + args.Delta) % range;
        if (offset < 0) offset += range;
        var newVal = baseChar + offset;
        parts[args.Index] = $"0x{newVal:X2}";

        ent.Comp.InputCode = string.Join(" ", parts);
        ent.Comp.DecipheredWord = DecipherCode(ent.Comp.InputCode, ent.Comp.CipherSetting);
        Dirty(ent);
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
            punchComp.Data = $"{ent.Comp.DecipheredWord}:{ent.Comp.CipherSetting}";
            Dirty(punchcard, punchComp);
        }

        ent.Comp.PunchcardCount--;
        ent.Comp.StatusMessage = "Punchcard printed.";
        Dirty(ent);
        UpdateCipherState(ent);
    }

    private void OnRefill(Entity<EncryptionCipherComputerComponent> ent, ref EncryptionCipherRefillMsg args)
    {
        var item = ent.Comp.PunchcardSlot.Item;
        if (item == null || !TryComp<PunchcardStackComponent>(item, out var stack))
        {
            ent.Comp.StatusMessage = "No punchcard stack in slot.";
            UpdateCipherState(ent);
            return;
        }

        ent.Comp.PunchcardCount += stack.Count;
        EntityManager.QueueDeleteEntity(item.Value);
        ent.Comp.StatusMessage = $"Refilled with {stack.Count} punchcards.";
        Dirty(ent);

        UpdateCipherState(ent);
    }

    private string DecipherCode(string hexCode, int setting)
    {
        var text = ParseHexCodes(hexCode);
        if (string.IsNullOrEmpty(text))
            return "";

        var result = "";
        foreach (var c in text)
        {
            if (char.IsLetter(c))
            {
                var baseChar = char.IsUpper(c) ? 'A' : 'a';
                var shift = (c - baseChar - setting) % 26;
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

    private string ParseHexCodes(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return "";
        var parts = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = "";
        foreach (var part in parts)
        {
            if (part.StartsWith("0x") && int.TryParse(part[2..], System.Globalization.NumberStyles.HexNumber, null, out var val))
            {
                result += (char)val;
            }
            else
            {
                // fallback, assume it's char
                result += part;
            }
        }
        return result;
    }

    private void UpdateCipherState(Entity<EncryptionCipherComputerComponent> ent)
    {
        ent.Comp.ValidWord = ChallengePhrases.Contains(ent.Comp.DecipheredWord.ToUpper());

        var state = new EncryptionCipherComputerBuiState(
            ent.Comp.InputCode,
            ent.Comp.CipherSetting,
            ent.Comp.DecipheredWord,
            ent.Comp.StatusMessage,
            ent.Comp.PunchcardCount,
            ent.Comp.ValidWord
        );

        _ui.SetUiState(ent.Owner, EncryptionCipherComputerUI.Key, state);
        Dirty(ent);
    }
}
