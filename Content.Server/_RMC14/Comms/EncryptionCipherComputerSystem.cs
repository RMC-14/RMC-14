using Content.Shared._RMC14.Comms;
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

        Subs.BuiEvents<EncryptionCipherComputerComponent>(EncryptionCipherComputerUI.Key,
            subs =>
            {
                subs.Event<EncryptionCipherSetInputMsg>(OnSetInput);
                subs.Event<EncryptionCipherChangeSettingMsg>(OnChangeSetting);
                subs.Event<EncryptionCipherPrintOutputMsg>(OnPrintOutput);
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

    private void OnPrintOutput(Entity<EncryptionCipherComputerComponent> ent, ref EncryptionCipherPrintOutputMsg args)
    {
        // TODO: Print punchcard
        ent.Comp.StatusMessage = "Output printed.";
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
            ent.Comp.StatusMessage
        );

        _ui.SetUiState(ent.Owner, EncryptionCipherComputerUI.Key, state);
        Dirty(ent);
    }
}
