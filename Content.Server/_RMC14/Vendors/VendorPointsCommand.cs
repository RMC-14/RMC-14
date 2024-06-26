using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using static Content.Shared._RMC14.Vendors.SharedCMAutomatedVendorSystem;

namespace Content.Server._RMC14.Vendors;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class VendorPointsCommand : ToolshedCommand
{
    private CMAutomatedVendorSystem? _automatedVendor;

    [CommandImplementation("get")]
    public int Get([PipedArgument] EntityUid marine)
    {
        return EntityManager.GetComponentOrNull<CMVendorUserComponent>(marine)?.Points ?? 0;
    }

    [CommandImplementation("getspecialist")]
    public int GetSpecialist([PipedArgument] EntityUid marine)
    {
        var comp = EntityManager.GetComponentOrNull<CMVendorUserComponent>(marine);
        return comp?.ExtraPoints?.GetValueOrDefault(SpecialistPoints) ?? 0;
    }

    [CommandImplementation("set")]
    public EntityUid Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine,
        [CommandArgument] ValueRef<int> points)
    {
        _automatedVendor ??= GetSys<CMAutomatedVendorSystem>();
        var user = EnsureComp<CMVendorUserComponent>(marine);
        _automatedVendor.SetPoints((marine, user), points.Evaluate(ctx));
        return marine;
    }

    [CommandImplementation("set")]
    public IEnumerable<EntityUid> Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines,
        [CommandArgument] ValueRef<int> points)
    {
        return marines.Select(marine => Set(ctx, marine, points));
    }

    [CommandImplementation("setspecialist")]
    public EntityUid SetSpecialist(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid marine,
        [CommandArgument] ValueRef<int> points)
    {
        _automatedVendor ??= GetSys<CMAutomatedVendorSystem>();
        var user = EnsureComp<CMVendorUserComponent>(marine);
        _automatedVendor.SetExtraPoints((marine, user), SpecialistPoints, points.Evaluate(ctx));
        return marine;
    }

    [CommandImplementation("setspecialist")]
    public IEnumerable<EntityUid> SetSpecialist(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> marines,
        [CommandArgument] ValueRef<int> points)
    {
        return marines.Select(marine => SetSpecialist(ctx, marine, points));
    }
}
