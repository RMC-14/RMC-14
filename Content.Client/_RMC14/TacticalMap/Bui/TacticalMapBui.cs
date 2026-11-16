using Content.Client._RMC14.TacticalMap.UI;
using Content.Client._RMC14.UserInterface;

namespace Content.Client._RMC14.TacticalMap.Bui;

public abstract class TacticalMapBui(EntityUid owner, Enum uiKey) : RMCPopOutBui<TacticalMapWindow>(owner, uiKey)
{
    private TacticalMapViewBinder? _binder;

    protected override TacticalMapWindow? Window { get; set; }

    protected override void Open()
    {
        base.Open();
        Window = this.CreatePopOutableWindow<TacticalMapWindow>();

        var system = EntMan.System<TacticalMapSystem>();
        Window.View.Configure(false, true, true, system.LineLimit, system.LabelMaxLength);
        _binder = new TacticalMapViewBinder(Window.View, Owner, SendMessage);
        _binder.Refresh(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Window != null)
        {
            Window.View.SaveViewState();
            Window.SaveSize();
        }

        _binder?.Dispose();
        _binder = null;
        base.Dispose(disposing);
    }
}
