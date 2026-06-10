namespace Outlet.Crm.Domain.Alerts;

public static class AlertErrors
{
    public const string MessageRequired =
        "Alert.MessageRequired: An alert requires a non-empty message.";

    public const string AlreadyAcknowledged =
        "Alert.AlreadyAcknowledged: This alert has already been acknowledged.";

    public static string NotFound(AlertId id) =>
        $"Alert.NotFound: Alert '{id.Value}' was not found.";
}
