================================================================================
NOTIFICATION SYSTEM — VISUAL DIAGRAMS & CODE SNIPPETS
================================================================================

================================================================================
1. ARCHITECTURE DIAGRAM
================================================================================

┌─────────────────────────────────────────────────────────────────────────────┐
│                          EVENT SOURCES                                      │
│                  (Order Service, Payment Service, etc.)                      │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    NOTIFICATIONSERVICE (Orchestrator)                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Send(userId, type, priority, payload)                              │   │
│  │  1. Fetch user from IUserRepository                                │   │
│  │  2. Get active channels via IUserPreferenceService                 │   │
│  │  3. Create Notification objects (Status = QUEUED)                  │   │
│  │  4. Render templates via ITemplateRenderer                         │   │
│  │  5. Save to INotificationRepository                                │   │
│  │  6. Enqueue to IMessageQueue                                       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      IMESSAGEQUEUE (Decoupling Point)                        │
│                    [Notification] [Notification] [...]                       │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CHANNELDISPATCHER (Background Worker)                     │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ProcessQueueAsync()                                                 │   │
│  │  1. Dequeue notification                                            │   │
│  │  2. Get channel via NotificationChannelFactory                      │   │
│  │  3. Send via INotificationChannel.SendAsync()                       │   │
│  │  4. On failure: retry with exponential backoff (IRetryPolicy)       │   │
│  │  5. Update status via StatusTracker                                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                    ┌────────────┼────────────┐
                    ▼            ▼            ▼
        ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
        │  EmailChannel    │ │  SmsChannel      │ │  PushChannel     │
        │ (INotification   │ │ (INotification   │ │ (INotification   │
        │  Channel)        │ │  Channel)        │ │  Channel)        │
        └────────┬─────────┘ └────────┬─────────┘ └────────┬─────────┘
                 │                    │                    │
                 ▼                    ▼                    ▼
        ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
        │  EmailVendor     │ │  SmsVendor       │ │  PushVendor      │
        │ (SendGrid, SES)  │ │ (Twilio, SNS)    │ │ (FCM, APNs)      │
        └──────────────────┘ └──────────────────┘ └──────────────────┘

================================================================================
2. CLASS DIAGRAM (Text Format)
================================================================================

INTERFACES
──────────
INotificationChannel
  + Channel: Channel { get; }
  + SendAsync(notification, user): Task<bool>

ITemplateRenderer
  + Render(notification): string

IMessageQueue
  + Enqueue(notification): void
  + Dequeue(): Notification?
  + IsEmpty: bool { get; }

IRetryPolicy
  + MaxRetries: int { get; }
  + ShouldRetry(retryCount): bool
  + GetDelay(retryCount): TimeSpan

IUserPreferenceService
  + GetActiveChannels(user, notificationType): IEnumerable<Channel>
  + Subscribe(userId, category): void
  + Unsubscribe(userId, category): void
  + UpdateChannelOptIn(userId, channel, optIn): void

IUserRepository
  + GetById(userId): User?
  + Save(user): void

INotificationRepository
  + Save(notification): void
  + GetById(notificationId): Notification?
  + UpdateStatus(notificationId, status): void

ITemplateRepository
  + Get(type, channel): MessageTemplate?

IMPLEMENTATIONS
───────────────
EmailChannel : INotificationChannel
  - _vendor: EmailVendor
  + SendAsync(notification, user): Task<bool>

SmsChannel : INotificationChannel
  - _vendor: SmsVendor
  + SendAsync(notification, user): Task<bool>

PushChannel : INotificationChannel
  - _vendor: PushVendor
  + SendAsync(notification, user): Task<bool>

InAppChannel : INotificationChannel
  - _vendor: InAppVendor
  + SendAsync(notification, user): Task<bool>

NotificationChannelFactory
  - _channels: Dictionary<Channel, INotificationChannel>
  + Get(channel): INotificationChannel

TemplateRenderer : ITemplateRenderer
  - _templateRepo: ITemplateRepository
  + Render(notification): string

ExponentialBackoffRetryPolicy : IRetryPolicy
  - _baseDelay: TimeSpan
  + MaxRetries: int { get; }
  + ShouldRetry(retryCount): bool
  + GetDelay(retryCount): TimeSpan

UserPreferenceService : IUserPreferenceService
  - _userRepo: IUserRepository
  + GetActiveChannels(user, notificationType): IEnumerable<Channel>
  + Subscribe(userId, category): void
  + Unsubscribe(userId, category): void
  + UpdateChannelOptIn(userId, channel, optIn): void

NotificationService
  - _userRepo: IUserRepository
  - _preferenceService: IUserPreferenceService
  - _renderer: ITemplateRenderer
  - _queue: IMessageQueue
  - _notificationRepo: INotificationRepository
  + Send(userId, type, priority, payload): void
  + GetStatus(notificationId): NotificationStatus?

ChannelDispatcher
  - _queue: IMessageQueue
  - _userRepo: IUserRepository
  - _channelFactory: NotificationChannelFactory
  - _retryPolicy: IRetryPolicy
  - _statusTracker: StatusTracker
  + ProcessQueueAsync(): Task

StatusTracker
  - _validTransitions: Dictionary<Status, HashSet<Status>>
  - _notificationRepo: INotificationRepository
  + Transition(notificationId, newStatus): void

================================================================================
3. STATE MACHINE DIAGRAM
================================================================================

                    ┌─────────────────────────────────────────┐
                    │                                         │
                    ▼                                         │
              ┌──────────┐                                    │
              │  QUEUED  │                                    │
              └────┬─────┘                                    │
                   │                                          │
         ┌─────────┴─────────┐                                │
         │                   │                                │
         ▼                   ▼                                │
    ┌────────┐          ┌────────┐                           │
    │  SENT  │          │ FAILED │◄──────────────────────────┘
    └────┬───┘          └────────┘
         │
    ┌────┴─────┐
    │           │
    ▼           ▼
┌──────────┐ ┌────────┐
│DELIVERED │ │ FAILED │
└────┬─────┘ └────────┘
     │
     ▼
  ┌────┐
  │READ│
  └────┘

Valid transitions:
  QUEUED → SENT, FAILED
  SENT → DELIVERED, FAILED
  DELIVERED → READ
  READ → (terminal)
  FAILED → (terminal)

Invalid transitions (throw exception):
  QUEUED → READ (must go through SENT, DELIVERED)
  SENT → QUEUED (no going back)
  READ → SENT (no going back)

================================================================================
4. CODE SNIPPETS
================================================================================

NOTIFICATIONSERVICE.SEND() — ORCHESTRATION
────────────────────────────────────────────

public void Send(int userId, NotificationType type, NotificationPriority priority,
                 Dictionary<string, string> payload)
{
    // 1. Fetch user
    var user = _userRepo.GetById(userId)
        ?? throw new KeyNotFoundException($"User {userId} not found.");

    // 2. Get active channels (subscription + preference filter)
    var activeChannels = _preferenceService.GetActiveChannels(user, type).ToList();

    if (activeChannels.Count == 0)
    {
        Console.WriteLine($"No active channels for user {userId} / {type}");
        return;
    }

    // 3. For each active channel, create notification
    foreach (var channel in activeChannels)
    {
        var notification = new Notification
        {
            UserId           = userId,
            NotificationType = type,
            Priority         = priority,
            Channel          = channel,
            Payload          = payload,
            Status           = NotificationStatus.Queued
        };

        // 4. Render template
        _renderer.Render(notification);

        // 5. Save and enqueue
        _notificationRepo.Save(notification);
        _queue.Enqueue(notification);

        Console.WriteLine($"Queued → User:{userId} | {type} | {channel}");
    }
}

USERPREFERENCESERVICE.GETACTIVECHANNELS() — TWO-LEVEL FILTER
─────────────────────────────────────────────────────────────

public IEnumerable<Channel> GetActiveChannels(User user, NotificationType notificationType)
{
    // Level 1: map event type → category, check subscription
    var category = NotificationCategoryMap.GetCategory(notificationType);
    var isSubscribed = user.Subscriptions
        .Any(s => s.Category == category && s.IsActive);

    if (!isSubscribed)
        return [];

    // Level 2: return only channels the user has globally enabled
    return user.Preference.ChannelOptIn
        .Where(kv => kv.Value)
        .Select(kv => kv.Key);
}

TEMPLATERENDERER.RENDER() — PLACEHOLDER SUBSTITUTION
──────────────────────────────────────────────────────

public string Render(Notification notification)
{
    // 1. Fetch template by (NotificationType, Channel)
    var template = _templateRepo.Get(notification.NotificationType, notification.Channel)
        ?? throw new InvalidOperationException(
            $"No template for {notification.NotificationType} / {notification.Channel}");

    // 2. Replace {{key}} with payload[key]
    var body = Regex.Replace(template.Body, @"\{\{(\w+)\}\}", m =>
        notification.Payload.TryGetValue(m.Groups[1].Value, out var val) ? val : m.Value);

    // 3. Store rendered body
    notification.RenderedBody = body;
    notification.TemplateId   = template.TemplateId;

    return body;
}

CHANNELDISPATCHER.PROCESSQUEUEASYNC() — RETRY LOOP
───────────────────────────────────────────────────

public async Task ProcessQueueAsync()
{
    while (!_queue.IsEmpty)
    {
        var notification = _queue.Dequeue();
        if (notification is null) continue;

        var user = _userRepo.GetById(notification.UserId);
        if (user is null) continue;

        var channel = _channelFactory.Get(notification.Channel);
        var success = false;

        // Retry loop with exponential backoff
        while (!success && _retryPolicy.ShouldRetry(notification.RetryCount))
        {
            if (notification.RetryCount > 0)
            {
                var delay = _retryPolicy.GetDelay(notification.RetryCount);
                Console.WriteLine($"Retry {notification.RetryCount} — waiting {delay.TotalSeconds}s");
                await Task.Delay(delay);
            }

            success = await channel.SendAsync(notification, user);

            if (!success) notification.RetryCount++;
        }

        var finalStatus = success ? NotificationStatus.Sent : NotificationStatus.Failed;
        _statusTracker.Transition(notification.NotificationId, finalStatus);
    }
}

STATUSTRACKER.TRANSITION() — STATE MACHINE
────────────────────────────────────────────

public void Transition(int notificationId, NotificationStatus newStatus)
{
    var notification = _notificationRepo.GetById(notificationId)
        ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

    // Validate transition
    if (!_validTransitions[notification.Status].Contains(newStatus))
        throw new InvalidOperationException(
            $"Invalid transition: {notification.Status} → {newStatus}");

    // Update
    var oldStatus = notification.Status;
    _notificationRepo.UpdateStatus(notificationId, newStatus);
    Console.WriteLine($"Notification {notificationId}: {oldStatus} → {newStatus}");
}

EXPONENTIALBACKOFFRETRYPOLICY.GETDELAY() — BACKOFF CALCULATION
───────────────────────────────────────────────────────────────

public TimeSpan GetDelay(int retryCount)
{
    // delay = baseDelay * 2^retryCount
    // Attempt 0: 2s * 2^0 = 2s
    // Attempt 1: 2s * 2^1 = 4s
    // Attempt 2: 2s * 2^2 = 8s
    return _baseDelay * Math.Pow(2, retryCount);
}

NOTIFICATIONCHANNELFACTORY.GET() — STRATEGY SELECTION
──────────────────────────────────────────────────────

public INotificationChannel Get(Channel channel)
{
    return _channels.TryGetValue(channel, out var ch)
        ? ch
        : throw new NotSupportedException($"Channel '{channel}' is not registered.");
}

// Usage in ChannelDispatcher:
var channel = _channelFactory.Get(notification.Channel);
await channel.SendAsync(notification, user);

================================================================================
5. TEMPLATE RENDERING EXAMPLE
================================================================================

Template (from database):
  TemplateId: "tmpl_order_delivered_email"
  Channel: Email
  NotificationType: OrderDelivered
  Subject: "Your order {{orderId}} has been delivered!"
  Body: "Hi {{name}}, your order {{orderId}} was delivered on {{date}}."

Payload (from event):
  {
    "name": "Alice",
    "orderId": "ORD-9921",
    "date": "2025-07-10"
  }

Rendering process:
  1. Regex finds {{name}}, {{orderId}}, {{date}}
  2. Replace {{name}} with "Alice"
  3. Replace {{orderId}} with "ORD-9921"
  4. Replace {{date}} with "2025-07-10"

Result:
  Subject: "Your order ORD-9921 has been delivered!"
  Body: "Hi Alice, your order ORD-9921 was delivered on 2025-07-10."

================================================================================
6. RETRY TIMELINE EXAMPLE
================================================================================

Scenario: Email vendor is temporarily down

Timeline:
  T=0s:    Dispatcher dequeues notification
           Attempt 0: SendAsync() → fails
           retryCount = 0, ShouldRetry(0)? YES
           Increment retryCount to 1

  T=0s:    Calculate delay: 2s * 2^0 = 2s
           Wait 2 seconds

  T=2s:    Attempt 1: SendAsync() → fails
           retryCount = 1, ShouldRetry(1)? YES
           Increment retryCount to 2

  T=2s:    Calculate delay: 2s * 2^1 = 4s
           Wait 4 seconds

  T=6s:    Attempt 2: SendAsync() → fails
           retryCount = 2, ShouldRetry(2)? YES
           Increment retryCount to 3

  T=6s:    Calculate delay: 2s * 2^2 = 8s
           Wait 8 seconds

  T=14s:   Attempt 3: SendAsync() → fails
           retryCount = 3, ShouldRetry(3)? NO (3 < 3 is false)
           Exit retry loop

  T=14s:   Status = FAILED
           StatusTracker.Transition(id, FAILED)

Total time: 14 seconds
Total attempts: 3
Exponential backoff prevented hammering vendor

================================================================================
7. ADDING WHATSAPP — STEP BY STEP
================================================================================

Step 1: Add to Channel enum
────────────────────────────
public enum Channel
{
    Email,
    Sms,
    Push,
    InApp,
    WhatsApp  // ← NEW
}

Step 2: Create WhatsAppChannel
───────────────────────────────
public class WhatsAppChannel : INotificationChannel
{
    private readonly WhatsAppVendor _vendor;

    public Channel Channel => Channel.WhatsApp;

    public WhatsAppChannel(WhatsAppVendor vendor) => _vendor = vendor;

    public async Task<bool> SendAsync(Notification notification, User user) =>
        await _vendor.SendAsync(user.PhoneNumber, notification.RenderedBody);
}

Step 3: Register in factory (Program.cs)
─────────────────────────────────────────
var channels = new INotificationChannel[]
{
    new EmailChannel(new EmailVendor()),
    new SmsChannel(new SmsVendor()),
    new PushChannel(new PushVendor()),
    new InAppChannel(new InAppVendor()),
    new WhatsAppChannel(new WhatsAppVendor())  // ← NEW
};
var factory = new NotificationChannelFactory(channels);

Step 4: Add templates to database
──────────────────────────────────
INSERT INTO message_templates VALUES
  ('tmpl_order_delivered_whatsapp', 'OrderDelivered', 'WhatsApp', NULL,
   'Hi {{name}}, your order {{orderId}} has been delivered!');

Changes to existing code:
  ✓ NotificationService: ZERO changes
  ✓ ChannelDispatcher: ZERO changes
  ✓ StatusTracker: ZERO changes
  ✓ TemplateRenderer: ZERO changes
  ✓ Existing channels: ZERO changes

This is the power of Strategy + Factory patterns!

================================================================================
8. DEPENDENCY INJECTION SETUP (Program.cs)
================================================================================

// Repositories
var userRepo         = new InMemoryUserRepository();
var templateRepo     = new InMemoryTemplateRepository();
var notificationRepo = new InMemoryNotificationRepository();

// Infrastructure
var queue       = new InMemoryMessageQueue();
var retryPolicy = new ExponentialBackoffRetryPolicy(maxRetries: 3, baseDelaySeconds: 2);

// Channels (Strategy implementations)
var emailChannel = new EmailChannel(new EmailVendor());
var smsChannel   = new SmsChannel(new SmsVendor());
var pushChannel  = new PushChannel(new PushVendor());
var inAppChannel = new InAppChannel(new InAppVendor());

// Factory
var channelFactory = new NotificationChannelFactory(
    [emailChannel, smsChannel, pushChannel, inAppChannel]);

// Services
var preferenceService = new UserPreferenceService(userRepo);
var renderer          = new TemplateRenderer(templateRepo);
var statusTracker     = new StatusTracker(notificationRepo);
var dispatcher        = new ChannelDispatcher(queue, userRepo, channelFactory, retryPolicy, statusTracker);

var notificationService = new NotificationService(
    userRepo, preferenceService, renderer, queue, notificationRepo);

// Usage
notificationService.Send(userId: 1, type: OrderDelivered, priority: High, payload);
await dispatcher.ProcessQueueAsync();

================================================================================
9. TESTING STRATEGY
================================================================================

Unit Tests
──────────
✓ UserPreferenceService.GetActiveChannels()
  - Mock user with subscriptions
  - Mock preferences
  - Assert correct channels returned

✓ TemplateRenderer.Render()
  - Mock template repository
  - Assert placeholders replaced correctly
  - Assert rendered body stored

✓ StatusTracker.Transition()
  - Mock notification repository
  - Assert valid transitions allowed
  - Assert invalid transitions throw

✓ ExponentialBackoffRetryPolicy.GetDelay()
  - Assert delay = 2s * 2^retryCount

Integration Tests
─────────────────
✓ NotificationService.Send() → full flow
  - Create user with subscriptions
  - Call Send()
  - Assert notifications queued
  - Assert templates rendered

✓ ChannelDispatcher.ProcessQueueAsync() → dispatch with retry
  - Mock channel to fail first attempt
  - Assert retry happens
  - Assert status updated

Mocking
───────
Mock INotificationChannel:
  public class MockChannel : INotificationChannel
  {
    public Channel Channel => Channel.Email;
    public int CallCount { get; private set; }
    public async Task<bool> SendAsync(Notification n, User u)
    {
      CallCount++;
      return CallCount > 1;  // Fail first, succeed second
    }
  }

================================================================================
10. MONITORING & METRICS
================================================================================

Key Metrics
───────────
1. Queue Depth
   - Current size of message queue
   - Alert if > 10,000 (backlog building up)

2. Dispatch Latency
   - Time from enqueue to send
   - Alert if > 5 seconds (dispatcher slow)

3. Success Rate
   - % of notifications sent successfully
   - Alert if < 95%

4. Retry Count Distribution
   - How many notifications needed retries?
   - Alert if > 10% need retries

5. Channel-Specific Metrics
   - Email success rate
   - SMS success rate
   - Push success rate
   - In-App success rate

Dashboards
──────────
- Queue depth over time
- Success rate per channel
- Latency percentiles (p50, p95, p99)
- Retry count distribution

Alerts
──────
- Queue depth > 10,000
- Success rate < 95%
- Latency p95 > 5s
- Any channel success rate < 90%

================================================================================
END OF DIAGRAMS & SNIPPETS
================================================================================
