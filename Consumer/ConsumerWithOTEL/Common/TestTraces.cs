using System.Diagnostics;
using System.Reflection;

namespace ConsumerWithOTEL.Common;

internal static class TestTraces
{
    public const string ActivitySourceName = nameof(TestTraces);

    public static readonly ActivitySource ActivitySource = new(
        ActivitySourceName,
        Assembly.GetExecutingAssembly().GetName().Version!.ToString());

    public static Activity? StartActivity(string activityName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity(
            name: activityName,
            kind: ActivityKind.Internal);
    }
}