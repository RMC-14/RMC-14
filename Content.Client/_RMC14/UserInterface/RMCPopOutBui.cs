namespace Content.Client._RMC14.UserInterface;

public abstract class RMCPopOutBui<T>(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey) where T : RMCPopOutWindow
{
    [ViewVariables]
    protected abstract T? Window { get; set; }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            Window?.DisposePopOut();
    }
}
