namespace Content.Client._RMC14.UserInterface.Systems.Ghost;

internal sealed class RMCGhostTargetRequestState
{
    private uint _nextRequestId;
    private uint? _pendingRequestId;
    private bool _refreshQueued;

    public uint? Request()
    {
        if (_pendingRequestId != null)
        {
            _refreshQueued = true;
            return null;
        }

        _nextRequestId++;
        if (_nextRequestId == 0)
            _nextRequestId++;

        _pendingRequestId = _nextRequestId;
        return _pendingRequestId;
    }

    public bool TryComplete(uint requestId, out bool refreshQueued)
    {
        refreshQueued = false;
        if (_pendingRequestId != requestId)
            return false;

        _pendingRequestId = null;
        refreshQueued = _refreshQueued;
        _refreshQueued = false;
        return true;
    }

    public void Reset()
    {
        _pendingRequestId = null;
        _refreshQueued = false;
    }
}
