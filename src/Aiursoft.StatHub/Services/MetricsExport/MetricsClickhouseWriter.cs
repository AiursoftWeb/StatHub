using Aiursoft.ClickhouseSdk;
using Aiursoft.ClickhouseSdk.Abstractions;
using Microsoft.Extensions.Options;

namespace Aiursoft.StatHub.Services.MetricsExport;

public sealed class MetricsClickhouseWriter(IOptions<MetricsExportOptions> options)
{
    private readonly MetricsExportOptions _options = options.Value;
    private bool _initialized;

    public async Task WriteAsync(IReadOnlyList<MetricsSample> samples)
    {
        if (!_options.Enabled || samples.Count == 0) return;
        await using var context = new MetricsContext(_options);
        if (!_initialized)
        {
            var database = ClickhouseIdentifier.Quote(ClickhouseConnectionUtility.GetDatabaseName(_options.ConnectionString));
            await using var initializer = new MetricsContext(new ClickhouseOptions
            {
                Enabled = true,
                ConnectionString = ClickhouseConnectionUtility.GetInitConnectionString(_options.ConnectionString)
            });
            await initializer.ExecuteScalarAsync<object>($"CREATE DATABASE IF NOT EXISTS {database}");
            // ReplacingMergeTree plus FINAL gives immediate idempotence, including uncertain write results.
            var columns = typeof(MetricsSample).GetProperties().Select(property =>
                $"{ClickhouseIdentifier.Quote(property.Name)} " +
                (property.PropertyType == typeof(DateTime)
                    ? "DateTime64(3, 'UTC')"
                    : property.PropertyType == typeof(double?) ? "Nullable(Float64)"
                    : ClickhouseTypeMapper.MapClrTypeToChType(property.PropertyType)));
            var ttl = _options.RetentionDays > 0 ? $" TTL SampleTime + INTERVAL {_options.RetentionDays} DAY" : string.Empty;
            await context.ExecuteScalarAsync<object>($"""
                CREATE TABLE IF NOT EXISTS ServerMetrics ({string.Join(", ", columns)})
                ENGINE = ReplacingMergeTree()
                PARTITION BY toDate(SampleTime)
                ORDER BY (SampleTime, ClientId, SampleId){ttl}
                """);
            // Additive evolution: historical/older-agent samples retain NULL pressure values.
            await context.ExecuteScalarAsync<object>("ALTER TABLE ServerMetrics ADD COLUMN IF NOT EXISTS PressureCpu Nullable(Float64), ADD COLUMN IF NOT EXISTS PressureMemory Nullable(Float64), ADD COLUMN IF NOT EXISTS PressureIo Nullable(Float64)");
            await context.ExecuteScalarAsync<object>("CREATE OR REPLACE VIEW ServerMetricsLatest AS SELECT * FROM ServerMetrics FINAL");
            _initialized = true;
        }

        foreach (var sample in samples) context.Samples.Add(sample);
        await context.SaveChangesAsync();
    }

    private sealed class MetricsContext : ClickhouseDbContext
    {
        public ClickhouseSet<MetricsSample> Samples { get; }

        public MetricsContext(ClickhouseOptions options) : base(options)
        {
            Samples = new ClickhouseSet<MetricsSample>(GetConnection, "ServerMetrics", sample => sample.ToRow());
        }

        public override Task SaveChangesAsync() => Samples.SaveChangesAsync();
    }
}
