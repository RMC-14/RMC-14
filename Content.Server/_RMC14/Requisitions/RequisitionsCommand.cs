using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Requisitions;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class RequisitionsCommand : ToolshedCommand
{
    [CommandImplementation("addbudget")]
    public async void AddBudget([CommandArgument] int money)
    {
        Sys<RequisitionsSystem>().ChangeBudget(money);
    }

    [CommandImplementation("removebudget")]
    public async void RemoveBudget([CommandArgument] int money)
    {
        Sys<RequisitionsSystem>().ChangeBudget(-money);
    }
}
