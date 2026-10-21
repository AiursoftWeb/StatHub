using Aiursoft.StatHub.Services.MetricsExport;
using Aiursoft.StatHub.Services.Monitoring;
using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools.Switchable;
using Aiursoft.Scanner;
using Aiursoft.StatHub.Configuration;
using Aiursoft.StatHub.Data;
using Aiursoft.WebTools.Abstractions.Models;
using Aiursoft.StatHub.InMemory;
using Aiursoft.StatHub.MySql;
using Aiursoft.StatHub.Services.Authentication;
using Aiursoft.StatHub.Sqlite;
using Aiursoft.UiStack;
using Aiursoft.UiStack.Layout;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Mvc.Razor;
using Aiursoft.ClickhouseLoggerProvider;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Aiursoft.Canon.TaskQueue;
using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.Canon.ScheduledTasks;

namespace Aiursoft.StatHub;

public class Startup : IWebStartup
{
    public virtual void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        // AppSettings.
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // Relational database
        var (connectionString, dbType, allowCache) = configuration.GetDbSettings();
        services.AddSwitchableRelationalDatabase(
            dbType: EntryExtends.IsInUnitTests() ? "InMemory" : dbType,
            connectionString: connectionString,
            supportedDbs:
            [
                new MySqlSupportedDb(allowCache: allowCache, splitQuery: false),
                new SqliteSupportedDb(allowCache: allowCache, splitQuery: true),
                new InMemorySupportedDb()
            ]);

        services.AddLogging(builder =>
        {
            builder.AddClickhouse(options => configuration.GetSection("Logging:Clickhouse").Bind(options));
        });

        // Authentication and Authorization
        services.AddTemplateAuth(configuration);

        // Services
        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddAssemblyDependencies(typeof(Startup).Assembly);
        services.AddSingleton<NavigationState<Startup>>();

        services.AddOptions<MetricsExportOptions>()
            .Bind(configuration.GetSection("MetricsExport"))
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.ConnectionString),
                "MetricsExport:ConnectionString is required when metrics export is enabled.")
            .Validate(options => options.MaxBufferedSamples is > 0 and <= 1_000_000,
                "MetricsExport:MaxBufferedSamples must be between 1 and 1000000.")
            .Validate(options => options.RetentionDays >= 0, "MetricsExport:RetentionDays must not be negative.")
            .ValidateOnStart();
        services.AddSingleton<MetricsExportBuffer>();
        services.AddSingleton<MetricsClickhouseWriter>();
        services.AddSingleton<InMemoryDatabase>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<MonitoringCoordinator>();
        services.AddScoped<MonitoringService>();
        services.AddScoped<IAlertCenterSettingsProvider, AlertCenterSettingsProvider>();
        services.AddScoped<IMonitoringIncidentClient, MonitoringIncidentClient>();
        services.AddAiurProtocolClient();

        // Background job queue
        services.AddTaskQueueEngine();
        services.AddScheduledTaskEngine();
        services.RegisterBackgroundJob<Services.BackgroundJobs.DummyJob>();
        var monitoringJob = services.RegisterBackgroundJob<Services.BackgroundJobs.MonitoringJob>();
        services.RegisterScheduledTask(registration: monitoringJob, period: TimeSpan.FromSeconds(30), startDelay: TimeSpan.FromSeconds(10));
        var metricsExportJob = services.RegisterBackgroundJob<Services.BackgroundJobs.MetricsExportJob>();
        services.RegisterScheduledTask(registration: metricsExportJob, period: TimeSpan.FromMinutes(5), startDelay: TimeSpan.FromMinutes(5));
        var orphanAvatarCleanupJob = services.RegisterBackgroundJob<Services.BackgroundJobs.OrphanAvatarCleanupJob>();
        services.RegisterScheduledTask(registration: orphanAvatarCleanupJob, period: TimeSpan.FromHours(6), startDelay: TimeSpan.FromMinutes(5));

        // Controllers and localization
        services.AddControllersWithViews()
            .AddAiurProtocol()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            })
            .AddApplicationPart(typeof(Startup).Assembly)
            .AddApplicationPart(typeof(UiStackLayoutViewModel).Assembly)
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();
    }

    public virtual void Configure(WebApplication app)
    {
        app.UseExceptionHandler("/Error/Code500");
        app.UseStatusCodePagesWithReExecute("/Error/Code{0}");
        app.UseUIStack();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebSockets();
        app.MapDefaultControllerRoute();
    }
}
