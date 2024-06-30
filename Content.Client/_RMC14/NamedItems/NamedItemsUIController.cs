using Content.Client._RMC14.LinkAccount;
using Content.Shared._RMC14.NamedItems;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._RMC14.NamedItems;

public sealed class NamedItemsUIController : UIController
{
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;

    public bool Available => _linkAccount.Tier is { NamedItems: true };
}
