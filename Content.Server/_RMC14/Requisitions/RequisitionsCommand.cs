using Content.Server.Administration;
using Content.Shared._RMC14.Requisitions;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Requisitions;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class RequisitionsCommand : ToolshedCommand
{
    [CommandImplementation("addbudget")]
    public void AddBudget(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] int money,
        [CommandArgument] RequisitionsBudgetAccount account = RequisitionsBudgetAccount.Cargo)
    {
        ChangeBudget(ctx, money, account);
    }

    [CommandImplementation("removebudget")]
    public void RemoveBudget(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] int money,
        [CommandArgument] RequisitionsBudgetAccount account = RequisitionsBudgetAccount.Cargo)
    {
        ChangeBudget(ctx, -money, account);
    }

    private void ChangeBudget(IInvocationContext ctx, int money, RequisitionsBudgetAccount account)
    {
        if (account is not RequisitionsBudgetAccount.Cargo and not RequisitionsBudgetAccount.BlackMarket)
        {
            ctx.WriteLine($"Invalid requisitions budget account '{account}'. Valid accounts: {nameof(RequisitionsBudgetAccount.Cargo)}, {nameof(RequisitionsBudgetAccount.BlackMarket)}.");
            return;
        }

        Sys<RequisitionsSystem>().ChangeBudget(money, account);
    }
}
