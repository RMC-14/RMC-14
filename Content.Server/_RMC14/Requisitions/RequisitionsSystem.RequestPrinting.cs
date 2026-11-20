using System.Linq;
using System.Text;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared.Access.Components;
using Content.Shared.Paper;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Requisitions;

public sealed partial class RequisitionsSystem
{
    private void PrintRequest(Entity<RequisitionsComputerComponent> computer, RequisitionsRequest request)
    {
        var paper = SpawnAtPosition(computer.Comp.RequestPaper, _transform.GetMoverCoordinates(computer));
        if (!TryComp(paper, out PaperComponent? paperComp))
            return;

        var form = EnsureComp<RequisitionsRequestPaperComponent>(paper);
        if (computer.Comp.Account is { } account)
            form.Account = account;
        form.Number = request.Number;
        form.MaxItems = computer.Comp.RequestPaperMaxItems;
        form.ContentsLength = computer.Comp.RequestPaperContentsLength;
        form.ReasonLength = computer.Comp.RequestPaperReasonLength;

        _metaSystem.SetEntityName(paper,
            Loc.GetString("rmc-requisitions-request-paper-name", ("number", request.Number)));
        WriteRequestPaper((paper, paperComp), form, request);
        _audio.PlayPvs(computer.Comp.PrintSound, computer);
    }

    private void UpdateRequestPapers(EntityUid account, RequisitionsRequest request)
    {
        var query = EntityQueryEnumerator<RequisitionsRequestPaperComponent, PaperComponent>();
        while (query.MoveNext(out var uid, out var form, out var paper))
        {
            if (form.Account == account && form.Number == request.Number && !TerminatingOrDeleted(uid))
                WriteRequestPaper((uid, paper), form, request);
        }
    }

    private void WriteRequestPaper(
        Entity<PaperComponent> paper,
        RequisitionsRequestPaperComponent form,
        RequisitionsRequest request)
    {
        var access = GetRequestAccess(request);
        var maxLength = Math.Max(0, paper.Comp.ContentSize);
        var reason = string.Join(" ", request.Reason.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries));
        var reasonLength = Math.Max(0, form.ReasonLength);
        if (reason.Length > reasonLength)
        {
            if (reasonLength > 0 && char.IsHighSurrogate(reason[reasonLength - 1]))
                reasonLength--;

            reason = reason[..reasonLength] + "…";
        }

        var available = maxLength - FormatRequestPaper(request, access, reason, string.Empty).Length;
        available = Math.Min(available, Math.Max(0, form.ContentsLength));
        var body = FormatRequestPaperItems(request, form.MaxItems, available);
        var text = FormatRequestPaper(request, access, reason, body);
        if (text.Length > maxLength)
            text = Loc.GetString("rmc-requisitions-request-paper-reference", ("number", request.Number));

        if (text.Length > maxLength)
            text = string.Empty;

        _paperSystem.SetContent(paper, text);
    }

    private string FormatRequestPaperItems(RequisitionsRequest request, int maxItems, int available)
    {
        var lines = new List<string>();
        var detailed = new StringBuilder();
        for (var i = 0; i < Math.Min(request.Items.Count, Math.Max(0, maxItems)); i++)
        {
            var item = request.Items[i];
            var issued = item.Fulfilled >= item.Amount;
            var amount = issued ? item.Amount : item.Amount - item.Fulfilled;
            var entry = item.Entry;
            var name = entry.Name is { } nameId ? Loc.GetString(nameId) : _prototype.Index(entry.Crate).Name;
            if (entry.Unit)
                name = RequisitionsUnitCatalog.GetUnitName(entry.Crate, name, _prototype,
                    EntityManager.ComponentFactory);
            var line = Loc.GetString(issued
                    ? "rmc-requisitions-request-paper-item-issued" : "rmc-requisitions-request-paper-item",
                ("amount", amount),
                ("name", FormattedMessage.EscapeText(name)),
                ("cost", entry.Cost * amount));
            lines.Add(line);
            detailed.AppendLine(line);
            if (issued || entry.Unit)
                continue;
            var packContents = RequisitionsCatalog.GetContents(entry, _prototype, EntityManager.ComponentFactory);
            detailed.AppendLine(Loc.GetString("rmc-requisitions-request-pack-contents",
                ("contents", FormattedMessage.EscapeText(packContents))));
        }

        var expanded = Loc.GetString("rmc-requisitions-request-paper-details",
            ("contents", detailed.ToString().TrimEnd()));
        if (lines.Count == request.Items.Count && expanded.Length <= available)
            return expanded;

        var compact = Loc.GetString("rmc-requisitions-request-paper-items", ("contents", string.Join("\n", lines)));
        if (lines.Count == request.Items.Count && compact.Length <= available)
            return compact;

        var contents = new StringBuilder();
        var result = string.Empty;
        for (var i = 0; i <= lines.Count; i++)
        {
            var omitted = request.Items.Count - i;
            var overflow = omitted > 0
                ? "\n" + Loc.GetString("rmc-requisitions-request-paper-overflow", ("count", omitted))
                : string.Empty;
            var candidate = Loc.GetString("rmc-requisitions-request-paper-items", ("contents", contents + overflow));
            if (candidate.Length > available)
                break;

            result = candidate;
            if (i == lines.Count)
                break;

            if (contents.Length > 0)
                contents.Append('\n');

            contents.Append(lines[i]);
        }

        return result;
    }

    private string GetRequestAccess(RequisitionsRequest request)
    {
        var access = new HashSet<string>();
        foreach (var item in request.Items)
        {
            var prototype = _prototype.Index(item.Entry.Crate);
            if (!prototype.TryGetComponent<AccessReaderComponent>(out var reader, EntityManager.ComponentFactory) ||
                !reader.Enabled)
            {
                continue;
            }

            foreach (var group in reader.AccessLists)
            {
                foreach (var level in group)
                {
                    if (_prototype.TryIndex(level, out var accessLevel))
                        access.Add(accessLevel.GetAccessLevelName());
                }
            }
        }

        return access.Count == 0
            ? Loc.GetString("rmc-requisitions-request-no-access")
            : string.Join(", ", access);
    }

    private string FormatRequestPaper(RequisitionsRequest request, string access, string reason, string contents)
    {
        var complete = request.Items.All(item => item.Fulfilled >= item.Amount);
        var completion = complete
            ? Loc.GetString("rmc-requisitions-request-paper-completed")
            : request.Closed ? Loc.GetString("rmc-requisitions-request-paper-closed") : string.Empty;
        var summary = complete
            ? Loc.GetString("rmc-requisitions-request-paper-issued-total",
                ("amount", request.Items.Sum(item => item.Amount)))
            : Loc.GetString(request.Closed
                    ? "rmc-requisitions-request-paper-unissued-total" : "rmc-requisitions-request-paper-remaining-total",
                ("amount", request.Items.Sum(item => item.Amount - item.Fulfilled)),
                ("cost", request.Items.Sum(item => item.Entry.Cost * (item.Amount - item.Fulfilled))));
        return Loc.GetString("rmc-requisitions-request-paper",
            ("number", request.Number),
            ("requester", FormattedMessage.EscapeText(request.RequestedBy)),
            ("rank", FormattedMessage.EscapeText(request.Rank)),
            ("reason", FormattedMessage.EscapeText(reason)),
            ("completion", completion.Length > 0 ? "\n" + completion : string.Empty),
            ("summary", summary),
            ("access", FormattedMessage.EscapeText(access)),
            ("contents", contents));
    }
}
