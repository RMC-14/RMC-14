rmc-requisitions-request-window-title = Supply Ordering Console
rmc-requisitions-contents-item = { $amount }× { $name }
rmc-requisitions-contents-random = { $item } (random selection)
rmc-requisitions-contents-unspecified = Contents are determined by the supplied item or its description.
rmc-requisitions-request-paper-overflow = Products not shown: { $count }. The full list is available at Requisitions.
rmc-requisitions-request-pack-contents = [color=#59604c]Product contents: { $contents }[/color]
rmc-requisitions-request-paper-item = [bold]{ $amount }×[/bold] { $name }: ${ $cost }
rmc-requisitions-request-paper-items = [mono size=10]{ $contents }[/mono]
rmc-requisitions-request-paper-details = [mono size=11]{ $contents }[/mono]
rmc-requisitions-request-paper-reference = Request #{ $number }. Details are available at Requisitions.
rmc-requisitions-request-paper-item-issued = [color=#59604c]{ $amount }× { $name } [bold](issued)[/bold][/color]
rmc-requisitions-request-paper-completed = [color=#59604c][bold]REQUISITION FULFILLED[/bold][/color]
rmc-requisitions-request-paper-closed = [color=#86652f][bold]REQUISITION CLOSED[/bold][/color]
rmc-requisitions-request-paper-unissued-total = [bold]Not issued:[/bold] { $amount } units / ${ $cost }
rmc-requisitions-request-paper-issued-total = [bold]Issued:[/bold] { $amount } units
rmc-requisitions-request-paper-remaining-total = [bold]Outstanding:[/bold] { $amount } units / ${ $cost }
rmc-requisitions-request-no-access = None
rmc-requisitions-tab-requests = Requests
rmc-requisitions-tab-requests-count = Requests ({ $count })
rmc-requisitions-request-reason = Reason for request:
rmc-requisitions-request-reason-placeholder = Explain why these supplies are needed.
rmc-requisitions-request-reason-required = Enter a reason before printing your request.
rmc-requisitions-request-submit = Print request
rmc-requisitions-request-submit-confirm = Print the form?
rmc-requisitions-request-approve = Approve
rmc-requisitions-request-approve-confirm = Process selected items?
rmc-requisitions-request-deny = Close request
rmc-requisitions-request-deny-confirm = Close the outstanding request?
rmc-requisitions-request-title = Requisition #{ $number }
rmc-requisitions-request-requester = Requested by: { $name } ({ $rank })
rmc-requisitions-request-reason-text = Reason: { $reason }
rmc-requisitions-request-item = { $amount }× { $name }: ${ $cost }
rmc-requisitions-request-list-empty = No matching requests. Insert a requisition form to load it into this console.
rmc-requisitions-request-unavailable = The requisitions account is unavailable. Your cart has been kept.
rmc-requisitions-request-not-allowed = Submitting requests is unavailable. Your cart has been kept.
rmc-requisitions-request-no-elevator = The cargo elevator is unavailable. The request remains in the queue.
rmc-requisitions-request-invalid-reason = Enter a reason within the permitted length.
rmc-requisitions-request-queue-full = The request queue is full. Please contact Requisitions.
rmc-requisitions-request-invalid-items = The request contains invalid items or exceeds the quantity limit. Your cart has been kept.
rmc-requisitions-request-unknown-rank = Unassigned
rmc-requisitions-request-submitted = Request form #{ $number } printed. Deliver it to Requisitions to be loaded into a console.
rmc-requisitions-request-cannot-approve = Nothing could be issued or purchased. Check the selected quantities and stock. The request remains queued.
rmc-requisitions-request-paper-name = Supply requisition form #{ $number }
rmc-requisitions-request-paper =
    { "" }[color=#86652f][bold]▌ REQUISITIONS DEPARTMENT[/bold][/color]
    { "" }[color=#b18644]________________________________[/color]
    { "" }[head=2]Supply requisition[/head]
    { "" }[mono size=10]REQ-103 / #{ $number }[/mono]{ $completion }

    { "" }[bold]Requested by:[/bold] { $requester }
    { "" }[bold]Position:[/bold] { $rank }
    { "" }[bold]Justification:[/bold] { $reason }

    { "" }[color=#86652f][bold]REQUESTED SUPPLIES[/bold][/color]
    { $contents }

    { $summary }
    { "" }[bold]Crate access:[/bold] { $access }
    { "" }[color=#b18644]________________________________[/color]
    { "" }[mono size=10]Requisitions endorsement / stamp:

    { "" }[color=#666666]A stamp on this form does not authorize a purchase.[/color][/mono]
rmc-requisitions-request-invoice-header = [head=2]{ $details }[/head]
rmc-requisitions-request-manifest =
    Requisition #{ $number }
    Requested by: { $requester }
    Approved by: { $approver }
