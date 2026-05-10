namespace NotificationSystemLLD.Channels.Vendors;

// Simulates SendGrid / SES. In production: inject HttpClient + API key.
public class EmailVendor
{
    public Task<bool> SendAsync(string to, string subject, string body)
    {
        Console.WriteLine($"[EmailVendor] → {to} | Subject: {subject}");
        return Task.FromResult(true);
    }
}

// Simulates Twilio / SNS SMS.
public class SmsVendor
{
    public Task<bool> SendAsync(string phoneNumber, string message)
    {
        Console.WriteLine($"[SmsVendor] → {phoneNumber} | {message}");
        return Task.FromResult(true);
    }
}

// Simulates FCM / APNs.
public class PushVendor
{
    public Task<bool> SendAsync(string deviceToken, string message)
    {
        Console.WriteLine($"[PushVendor] → {deviceToken} | {message}");
        return Task.FromResult(true);
    }
}

// Stores in-app notification in DB (simulated here with console output).
public class InAppVendor
{
    public Task<bool> StoreAsync(int userId, string message)
    {
        Console.WriteLine($"[InAppVendor] → UserId:{userId} | {message}");
        return Task.FromResult(true);
    }
}
