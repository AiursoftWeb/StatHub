namespace Aiursoft.StatHub.SDK.Models;

/// <summary>Constant-space, event-time health confirmation. Reading a report never advances timers.</summary>
public sealed class ResourceHealthTracker
{
    private readonly object _gate = new();
    private DateTime? _sampleAt, _receivedAt, _evaluatedAt, _yellowSince, _redSince, _notRedSince, _recoverySince;
    private DateTime _bootTime;
    private double _idle;
    private PressureInfo? _pressure;
    private AgentStatus _status = AgentStatus.Unknown;
    private string _reason = "Waiting for complete pressure measurements and a confirmed health state.";

    public void Observe(DstatResult sample, double? diskRatio, DateTime receivedAt, DateTime bootTime)
    {
        lock (_gate)
        {
            // The existing agent estimates boot time from uptime; sub-second refresh jitter is not a reboot.
            if ((bootTime - _bootTime).Duration() > TimeSpan.FromSeconds(5)) { Reset(); _bootTime = bootTime; }
            if (sample.SampleTime is not { } at || at.Kind != DateTimeKind.Utc ||
                receivedAt - at > TimeSpan.FromSeconds(30) || at - receivedAt > TimeSpan.FromSeconds(5) ||
                sample.CpuIdl is < 0 or > 100 || sample.Pressure is not { IsValid: true } ||
                diskRatio is null || !double.IsFinite(diskRatio.Value) || diskRatio is < 0 or > 1)
            {
                Reset();
                _reason = "Pressure data is incomplete or stale. Update the agent and check Linux PSI support.";
                return;
            }
            // Retransmissions and out-of-order reports are not new recovery evidence.
            if (_sampleAt.HasValue && at <= _sampleAt.Value) return;
            if ((_sampleAt.HasValue && at - _sampleAt.Value > TimeSpan.FromSeconds(20)) ||
                (_receivedAt.HasValue && receivedAt - _receivedAt.Value > TimeSpan.FromSeconds(20))) Reset();
            _idle = _sampleAt.HasValue
                ? _idle + (sample.CpuIdl - _idle) * (1 - Math.Exp(-(at - _sampleAt.Value).TotalSeconds / 60))
                : sample.CpuIdl;
            _sampleAt = at;
            _receivedAt = receivedAt;
            _pressure = sample.Pressure;

            var yellow = (_idle <= 15 && _pressure.Cpu >= 20) || _pressure.Memory >= 5 || _pressure.Io >= 10 || diskRatio >= 0.80;
            var red = (_idle <= 5 && _pressure.Cpu >= 50) || _pressure.Memory >= 20 || _pressure.Io >= 30 || diskRatio >= 0.92;
            var recovered = (_idle >= 20 || _pressure.Cpu <= 10) && _pressure.Memory <= 2 && _pressure.Io <= 5 && diskRatio <= 0.75;
            _yellowSince = yellow ? _yellowSince ?? at : null;
            _redSince = red ? _redSince ?? at : null;
            _notRedSince = !red ? _notRedSince ?? at : null;
            _recoverySince = recovered ? _recoverySince ?? at : null;

            if (_evaluatedAt.HasValue && at - _evaluatedAt.Value < TimeSpan.FromSeconds(10)) return;
            _evaluatedAt = at;

            if (Elapsed(_redSince, at, 120)) _status = AgentStatus.Critical;
            else if (Elapsed(_recoverySince, at, 300)) _status = AgentStatus.Healthy;
            else if (_status == AgentStatus.Critical && Elapsed(_notRedSince, at, 300)) _status = AgentStatus.Warning;
            else if (_status is AgentStatus.Healthy or AgentStatus.Unknown && Elapsed(_yellowSince, at, 120)) _status = AgentStatus.Warning;

            _reason = _status switch
            {
                AgentStatus.Healthy => "Recovery limits have been satisfied for five continuous minutes.",
                AgentStatus.Critical => "Severe resource pressure confirmed. Recovery requires sustained improvement.",
                AgentStatus.Warning => "Resource pressure confirmed. Recovery requires five minutes below the recovery limits.",
                _ => "Waiting for two minutes of pressure or five minutes below all recovery limits."
            };
        }
    }

    private static bool Elapsed(DateTime? since, DateTime at, int seconds) =>
        since.HasValue && at - since.Value >= TimeSpan.FromSeconds(seconds);

    private void Reset()
    {
        _sampleAt = _receivedAt = _evaluatedAt = _yellowSince = _redSince = _notRedSince = _recoverySince = null;
        _pressure = null;
        _status = AgentStatus.Unknown;
    }

    public (AgentStatus Status, string Reason, double? Idle, PressureInfo? Pressure) Snapshot(DateTime now)
    {
        lock (_gate)
        {
            if (!_receivedAt.HasValue || now - _receivedAt.Value > TimeSpan.FromSeconds(20))
                return (AgentStatus.Unknown, "Waiting for fresh, complete pressure measurements.", null, null);
            return (_status, _reason, _idle, _pressure);
        }
    }
}
