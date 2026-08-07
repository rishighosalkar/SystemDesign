================================================================================
NOTIFICATION SYSTEM — MOCK INTERVIEW TRANSCRIPT
================================================================================

This is a realistic 60-minute interview. Study how concepts are explained
and how to handle follow-up questions.

================================================================================
MINUTE 0-5: CLARIFICATION & REQUIREMENTS
================================================================================

INTERVIEWER:
"Design a notification system. Users can subscribe to different notification
types across multiple channels. Notifications should be reliable and extensible."

YOU:
"Great! Let me clarify a few things to scope this correctly:

1. Is this a single service or distributed system?
   → I'll assume single service for this design.

2. Should I focus on architecture and design patterns, or implementation details?
   → I'll cover both, but focus on architecture.

3. Are there any specific channels beyond Email, SMS, Push, In-App?
   → I'll design for these 4, but make it easy to add more.

4. What's the expected scale?
   → I'll assume millions of users, thousands of notifications per second.
     The queue-based architecture will scale horizontally.

5. Should I worry about distributed transactions or eventual consistency?
   → I'll keep it simple: single service, eventual consistency is fine.

Is that good?"

INTERVIEWER:
"Perfect. Go ahead."

================================================================================
MINUTE 5-15: HIGH-LEVEL ARCHITECTURE
================================================================================

YOU:
"Let me draw the high-level flow:

[Event Source] → [NotificationService] → [Queue] → [Dispatcher] → [Channels]

The key insight is the queue. It decouples rendering from dispatch, which
gives us several benefits:

1. Async processing: rendering doesn't block on vendor calls
2. Resilience: if dispatcher crashes, queue persists (with persistent queue)
3. Backpressure: if vendor is slow, queue builds up, dispatcher processes at its pace
4. Monitoring: queue depth tells us system health

The flow is:
1. Event source calls NotificationService.Send()
2. NotificationService filters active channels based on user subscriptions + preferences
3. For each active channel, create a Notification object
4. Render the template (replace {{placeholders}} with payload values)
5. Enqueue to message queue
6. ChannelDispatcher (background worker) dequeues and sends
7. On failure, retry with exponential backoff
8. Update status in database

The key components are:
- NotificationService: orchestrator
- UserPreferenceService: two-level filtering
- TemplateRenderer: template resolution + placeholder substitution
- ChannelDispatcher: queue processing + retry
- StatusTracker: state machine for notification lifecycle
- NotificationChannelFactory: creates channels

Does this make sense so far?"

INTERVIEWER:
"Yes, good. Tell me about the entities."

================================================================================
MINUTE 15-25: ENTITIES & DATA MODEL
================================================================================

YOU:
"Let me walk through the key entities:

USER
  - id, name, email, phone_number, device_token
  - Has a list of Subscriptions
  - Has a NotificationPreference

SUBSCRIPTION
  - Represents a user's opt-in for a notification category
  - Subscribing to a category covers ALL event types within it
  - Example: User subscribes to OrderUpdates → gets OrderPlaced, OrderShipped,
             OrderDelivered, OrderCancelled automatically
  - Fields: user_id, category, is_active

NOTIFICATIONPREFERENCE
  - Global channel-level opt-in/out
  - Controls WHICH channels the user wants to receive notifications on
  - Example: User enabled Email + Push, disabled SMS
  - Fields: user_id, channel, opt_in (bool)

NOTIFICATION
  - Represents a single notification instance
  - Fields: user_id, type, channel, priority, status, payload, rendered_body, retry_count
  - Status lifecycle: QUEUED → SENT → DELIVERED → READ / FAILED

MESSAGETEMPLATE
  - Channel-specific template with placeholders
  - Example: 'Hi {{name}}, your order {{orderId}} is delivered.'
  - Keyed by (NotificationType, Channel)
  - Email has Subject + Body, SMS has only Body

The two-level filtering:
  Level 1 (Subscription): Is user subscribed to the category this event belongs to?
    → OrderDelivered belongs to OrderUpdates → subscribed? YES/NO
  Level 2 (Preference):   Which channels has user globally enabled?
    → Return channels where ChannelOptIn = true

Example:
  User subscribed to OrderUpdates? YES
  User's enabled channels: Email=true, SMS=true, Push=false, InApp=true
  → Send via Email, SMS, InApp (not Push)"

INTERVIEWER:
"Good. How do you handle the different channels?"

================================================================================
MINUTE 25-35: DESIGN PATTERNS & CHANNELS
================================================================================

YOU:
"Great question. This is where design patterns come in.

I use the STRATEGY PATTERN for channels. Each channel (Email, SMS, Push, InApp)
implements the same interface:

  interface INotificationChannel
  {
    Task<bool> SendAsync(Notification notification, User user);
  }

Concrete implementations:
  - EmailChannel: calls EmailVendor (SendGrid, SES)
  - SmsChannel: calls SmsVendor (Twilio, SNS)
  - PushChannel: calls PushVendor (FCM, APNs)
  - InAppChannel: stores in database

The ChannelDispatcher doesn't care which channel it's using:

  var channel = factory.Get(notification.Channel);
  await channel.SendAsync(notification, user);

This is powerful because:
1. New channels added without modifying dispatcher
2. Each channel can have different logic (Email needs subject, SMS doesn't)
3. Easy to mock for testing

I also use the FACTORY PATTERN:

  class NotificationChannelFactory
  {
    Dictionary<Channel, INotificationChannel> _channels;
    INotificationChannel Get(Channel channel) => _channels[channel];
  }

The factory centralizes channel creation. To add WhatsApp:
1. Create WhatsAppChannel class
2. Register in factory
3. Add templates
4. Zero changes to existing code

This is the Open/Closed Principle: open for extension, closed for modification.

Finally, I use the STATE PATTERN for notification lifecycle:

  QUEUED → SENT → DELIVERED → READ
                ↘ FAILED

The StatusTracker enforces valid transitions. You can't go from QUEUED to READ
directly. This prevents invalid states and makes the state machine explicit.

Why NOT Observer Pattern?
  Observer is great for decoupled event listeners, but here we need:
  - Central coordination for retry logic
  - Status tracking
  - Preference filtering
  - Template rendering
  
  Observer would scatter this logic across subscribers with no central control.
  An orchestrator (NotificationService) is the right pattern."

INTERVIEWER:
"Good explanation. Walk me through a specific example."

================================================================================
MINUTE 35-50: END-TO-END FLOW
================================================================================

YOU:
"Perfect. Let me walk through ORDER_DELIVERED notification via EMAIL.

Step 1: Event Source
  Order Service calls:
    notificationService.Send(
      userId: 1,
      type: NotificationType.OrderDelivered,
      priority: NotificationPriority.High,
      payload: { 'name': 'Alice', 'orderId': 'ORD-9921', 'date': '2025-07-10' }
    )

Step 2: NotificationService - Fetch & Filter
  - Fetch User 1 from repository
  - Call preferenceService.GetActiveChannels(user, OrderDelivered)
    • Map OrderDelivered → NotificationCategory.OrderUpdates
    • Check subscription: User 1 subscribed to OrderUpdates? YES
    • Check preferences: Email=true, SMS=true, Push=false, InApp=true
    • Return [Email, SMS, InApp]
  
  Note: Push is filtered out because user has it globally disabled in preferences.

Step 3: Create Notification
  notification = new Notification
  {
    UserId = 1,
    NotificationType = OrderDelivered,
    Channel = Email,
    Payload = { 'name': 'Alice', 'orderId': 'ORD-9921', 'date': '2025-07-10' },
    Status = Queued
  }

Step 4: TemplateRenderer
  - Fetch template by (OrderDelivered, Email)
    Subject: 'Your order {{orderId}} has been delivered!'
    Body: 'Hi {{name}}, your order {{orderId}} was delivered on {{date}}.'
  
  - Replace placeholders using regex:
    Subject: 'Your order ORD-9921 has been delivered!'
    Body: 'Hi Alice, your order ORD-9921 was delivered on 2025-07-10.'
  
  - Store in notification.RenderedBody

Step 5: Save & Enqueue
  - notificationRepository.Save(notification) → Status = Queued
  - messageQueue.Enqueue(notification)

Step 6: ChannelDispatcher (Background Worker)
  - Dequeue notification
  - Get channel: factory.Get(Channel.Email) → EmailChannel
  - Send: await emailChannel.SendAsync(notification, user)
    • EmailVendor.SendAsync(user.Email, subject, body)
    • Returns true on success
  
  - statusTracker.Transition(notificationId, Sent)
    • Validates: Queued → Sent is valid ✓
    • Updates repository
    • Logs: 'Notification 1: Queued → Sent'

Step 7: Status Query
  - Client calls: notificationService.GetStatus(notificationId)
  - Returns: NotificationStatus.Sent

The entire flow is coordinated by NotificationService, which acts as an
orchestrator. Each component has a single responsibility."

INTERVIEWER:
"What if the email vendor is down?"

================================================================================
MINUTE 50-60: RETRY MECHANISM
================================================================================

YOU:
"Excellent question. This is where the retry mechanism comes in.

I use exponential backoff:

  Attempt 0 (immediate):
    - Send fails
    - retryCount = 0
    - ShouldRetry(0)? YES (0 < 3)
    - Increment retryCount to 1

  Attempt 1 (after delay):
    - Delay = 2s * 2^0 = 2 seconds
    - Wait 2s
    - Send fails
    - retryCount = 1
    - ShouldRetry(1)? YES (1 < 3)
    - Increment retryCount to 2

  Attempt 2 (after delay):
    - Delay = 2s * 2^1 = 4 seconds
    - Wait 4s
    - Send fails
    - retryCount = 2
    - ShouldRetry(2)? YES (2 < 3)
    - Increment retryCount to 3

  Attempt 3 (after delay):
    - Delay = 2s * 2^2 = 8 seconds
    - Wait 8s
    - Send fails
    - retryCount = 3
    - ShouldRetry(3)? NO (3 < 3 is false)
    - Status = Failed

Total time: 14 seconds, 3 attempts.

Why exponential backoff?
1. Avoids hammering vendor with requests
2. Gives vendor time to recover
3. Reduces load during outages
4. Standard practice in distributed systems

The retry logic is encapsulated in IRetryPolicy:

  interface IRetryPolicy
  {
    bool ShouldRetry(int retryCount);
    TimeSpan GetDelay(int retryCount);
  }

This makes it easy to swap strategies:
- ExponentialBackoffRetryPolicy (current)
- LinearBackoffRetryPolicy (faster recovery)
- NoRetryPolicy (for testing)

In production, I'd add jitter to prevent thundering herd:
  delay = baseDelay * 2^retryCount + random(0, 1000ms)

This prevents all instances from retrying at the same time."

INTERVIEWER:
"How do you add a new channel?"

================================================================================
MINUTE 60-70: EXTENSIBILITY
================================================================================

YOU:
"Great question. This is where the design really shines.

Let's say we want to add WhatsApp. Here's what we do:

Step 1: Add to Channel enum
  public enum Channel
  {
    Email,
    Sms,
    Push,
    InApp,
    WhatsApp  // ← NEW
  }

Step 2: Create WhatsAppChannel
  public class WhatsAppChannel : INotificationChannel
  {
    public Channel Channel => Channel.WhatsApp;
    
    public async Task<bool> SendAsync(Notification notification, User user) =>
      await _whatsAppVendor.SendAsync(user.PhoneNumber, notification.RenderedBody);
  }

Step 3: Register in factory
  var channels = new[]
  {
    new EmailChannel(...),
    new SmsChannel(...),
    new PushChannel(...),
    new InAppChannel(...),
    new WhatsAppChannel(...)  // ← NEW
  };
  var factory = new NotificationChannelFactory(channels);

Step 4: Add templates
  INSERT INTO message_templates VALUES
    ('tmpl_order_delivered_whatsapp', 'OrderDelivered', 'WhatsApp', NULL,
     'Hi {{name}}, your order {{orderId}} has been delivered!');

Changes to existing code:
  ✓ NotificationService: ZERO changes
  ✓ ChannelDispatcher: ZERO changes
  ✓ StatusTracker: ZERO changes
  ✓ TemplateRenderer: ZERO changes
  ✓ Existing channels: ZERO changes

This is the Open/Closed Principle in action. The system is open for extension
(add WhatsApp) but closed for modification (no changes to existing code).

This is possible because:
1. Strategy Pattern: each channel is a strategy
2. Factory Pattern: factory handles creation
3. Dependency Injection: components don't create their dependencies
4. Interface-based design: everything talks to interfaces, not concrete classes"

INTERVIEWER:
"Good. Let's talk about scalability."

================================================================================
MINUTE 70-80: SCALABILITY & TRADEOFFS
================================================================================

YOU:
"Good question. Let me discuss scalability and tradeoffs.

Current bottlenecks:
1. Single dispatcher thread
2. In-memory queue (limited by RAM)
3. Single database connection

How to scale:

1. Multiple Dispatcher Instances
   - Each pulls from shared queue (SQS, RabbitMQ)
   - Horizontal scaling: add more instances as load increases
   - Queue handles distribution

2. Database Sharding
   - Shard by user_id
   - Reduces contention
   - Each shard handles subset of users

3. Template Caching
   - Cache templates in memory
   - Reduces database hits
   - Invalidate on template update

4. Channel-Specific Worker Pools
   - Email workers: 100 instances
   - SMS workers: 50 instances (slower)
   - Push workers: 100 instances
   - Different scaling needs per channel

Tradeoffs:

Queue vs Direct Dispatch:
  Queue (chosen):
    ✓ Decouples rendering from dispatch
    ✓ Async processing
    ✓ Survives crashes (with persistent queue)
    ✗ Adds latency
  
  Direct:
    ✓ Lower latency
    ✗ Blocks on vendor calls
    ✗ Tight coupling

In-Memory vs Persistent Queue:
  In-Memory (chosen for interview):
    ✓ Simple, fast
    ✗ Loses messages on crash
  
  Persistent (production):
    ✓ Survives crashes
    ✗ More complex

Exponential vs Linear Backoff:
  Exponential (chosen):
    ✓ Reduces load during outages
    ✗ Slower recovery for transient failures
  
  Linear:
    ✓ Faster recovery
    ✗ More aggressive on vendor

For this design, the queue-based architecture naturally scales. Add more
dispatcher instances, they all pull from the same queue. For templates,
we cache. For database, we shard by user_id."

INTERVIEWER:
"What about monitoring?"

================================================================================
MINUTE 80-90: MONITORING & WRAP-UP
================================================================================

YOU:
"Great question. Monitoring is crucial.

Key Metrics:
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

Dashboards:
- Queue depth over time
- Success rate per channel
- Latency percentiles (p50, p95, p99)
- Retry count distribution

Alerts:
- Queue depth > 10,000
- Success rate < 95%
- Latency p95 > 5s
- Any channel success rate < 90%

This gives us visibility into system health and helps us identify issues early.

Summary of the design:
1. Orchestrator (NotificationService) coordinates flow
2. Queue decouples rendering from dispatch
3. Factory creates channels (Strategy pattern)
4. StatusTracker enforces state machine
5. Retry mechanism with exponential backoff
6. Easy to add new channels (Open/Closed Principle)
7. Scales horizontally with multiple dispatchers
8. Monitored with key metrics

Any questions?"

INTERVIEWER:
"How do you handle duplicate notifications?"

YOU:
"Good question. I'd add an idempotency key (UUID) to each notification.
Before processing, check if we've already sent this notification.
Store in cache or database. Prevents duplicate sends if message is reprocessed.

Example:
  if (cache.Contains(notification.IdempotencyKey))
    return;  // Already sent
  
  await channel.SendAsync(notification, user);
  cache.Add(notification.IdempotencyKey);

This is important for exactly-once delivery semantics."

INTERVIEWER:
"Great. I think we've covered the main points. Thanks for the thorough explanation."

YOU:
"Thank you! I enjoyed discussing the design. The key takeaways are:
1. Queue-based architecture for decoupling and scalability
2. Strategy + Factory patterns for extensibility
3. State machine for reliability
4. Exponential backoff for resilience
5. Monitoring for visibility

Happy to answer any other questions!"

================================================================================
KEY TAKEAWAYS FROM THIS INTERVIEW
================================================================================

✓ Started with clarification questions to scope the problem
✓ Drew high-level architecture first (Event → Service → Queue → Dispatcher)
✓ Explained entities and data model clearly
✓ Used design patterns to justify architectural decisions
✓ Walked through a concrete example (ORDER_DELIVERED via EMAIL)
✓ Explained retry mechanism with exponential backoff
✓ Showed how to add new channels (extensibility)
✓ Discussed scalability and tradeoffs
✓ Mentioned monitoring and metrics
✓ Answered follow-up questions confidently

Things that impressed the interviewer:
1. Clear communication with diagrams
2. Understanding of design patterns and why they're used
3. Concrete examples (ORDER_DELIVERED)
4. Scalability thinking
5. Tradeoff analysis
6. Extensibility (WhatsApp example)
7. Monitoring and observability

Things to avoid:
✗ Over-engineering (distributed locks, advanced rate limiting, etc.)
✗ Vague explanations ("it's scalable" without details)
✗ Ignoring tradeoffs
✗ Not mentioning monitoring
✗ Not explaining why design patterns are used

================================================================================
END OF MOCK INTERVIEW
================================================================================
