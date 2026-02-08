using Content.Server.Interaction;
using Content.Shared.Interaction;
using Content.Shared._RMC14.Comms;
using Content.Shared.Storage;
using Robust.Shared.Containers;
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

            // Mispunch chance: 1/7
            if (_random.Next(7) == 0)
            {
                var chars = punchComp.Data.ToCharArray();
                if (chars.Length > 0)
                {
                    var index = _random.Next(chars.Length);
                    chars[index] = '0';
                    punchComp.Data = new string(chars);
                    Dirty(punchcard, punchComp);
                }
            }
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
