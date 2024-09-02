namespace ConsumerWithOTEL;

public sealed class OpenTelemetrySettings
{
    /// <summary>
    /// Indicates whether OpenTelemetry is enabled.
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// The OpenTelemetry collector endpoint.
    /// </summary>
    public Uri CollectorEndpoint { get; init; } = new("http://localhost:4317");
}