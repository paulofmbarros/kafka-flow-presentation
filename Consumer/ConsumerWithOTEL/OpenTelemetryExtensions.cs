using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Resources;

namespace ConsumerWithOTEL;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Fetches <see cref="OpenTelemetrySettings"/> from the <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to fetch the settings from.</param>
    /// <returns>The fetched <see cref="OpenTelemetrySettings"/> instance, or an instance with default values if not found.</returns>
    public static OpenTelemetrySettings GetOpenTelemetrySettings(this IConfiguration configuration)
    {
        var openTelemetrySettings = configuration.GetSection("OpenTelemetry").Get<OpenTelemetrySettings>();
        return openTelemetrySettings ?? new OpenTelemetrySettings();
    }

    internal static Action<ResourceBuilder> CreateResourceConfigurator(
        IWebHostEnvironment environment,
        IConfiguration configuration)
        => resourceBuilder => resourceBuilder
            .AddService(
                serviceName: configuration.GetServiceName(),
                serviceVersion: Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
                serviceInstanceId: Environment.MachineName)
            .AddAttributes([new("environment", environment.EnvironmentName.ToLowerInvariant())]);

    private static string GetServiceName(this IConfiguration configuration)
        => configuration.GetValue<string>("spring:application:name")
           ?? throw new InvalidOperationException("spring:application:name is not configured");
}