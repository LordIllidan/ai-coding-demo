using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Notifications;

/// <summary>Maps a policy status transition to the push notification shown on the mobile app.</summary>
public static class PolicyStatusNotificationMapper
{
    public static PushNotification? Map(PolicyNumber policyNumber, PolicyStatus status)
    {
        var body = status switch
        {
            PolicyStatus.Active => $"Polisa {policyNumber.Value} została aktywowana.",
            PolicyStatus.Cancelled => $"Polisa {policyNumber.Value} została anulowana.",
            PolicyStatus.Expired => $"Polisa {policyNumber.Value} wygasła.",
            _ => null,
        };

        if (body is null)
        {
            return null;
        }

        return new PushNotification(
            Title: "Zmiana statusu zgłoszenia",
            Body: body,
            Data: new Dictionary<string, string>
            {
                ["policyNumber"] = policyNumber.Value,
                ["status"] = status.ToString(),
            });
    }
}
