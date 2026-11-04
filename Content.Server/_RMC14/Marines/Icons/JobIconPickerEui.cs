using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Icons;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines.Icons;

[UsedImplicitly]
public sealed class JobIconPickerEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    private readonly NetEntity _target;
    public Action? OnClosed;

    public JobIconPickerEui(NetEntity target)
    {
        _target = target;
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        _adminManager.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _adminManager.OnPermsChanged -= OnPermsChanged;
        OnClosed?.Invoke();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.VarEdit))
            return;

        if (!_entManager.TryGetEntity(_target, out var marine))
            return;

        var marineSystem = _entManager.System<SharedMarineSystem>();
        switch (msg)
        {
            case JobIconPickerSelectMessage select:
                marineSystem.SetMarineIcon(marine.Value, new SpriteSpecifier.Rsi(select.Rsi, select.State));
                break;
            case JobIconPickerClearMessage:
                marineSystem.ClearMarineIcon(marine.Value);
                break;
        }
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.VarEdit))
            Close();
    }
}
