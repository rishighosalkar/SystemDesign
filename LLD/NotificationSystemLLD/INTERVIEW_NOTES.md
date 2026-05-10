================================================================================
NOTIFICATION SYSTEM — LLD INTERVIEW PREPARATION NOTES
================================================================================

================================================================================
1. PROBLEM STATEMENT
================================================================================

Design a notification system that:
- Sends notifications across multiple channels (Email, SMS, Push, In-App)
- Respects user preferences and subscriptions
- Handles failures gracefully with retries
- Tracks notification lifecycle
- Is extensible (easy to add new channels)

Scope: Single service, not distributed. Focus on clean OOP design.

================================================================================
2. FUNCTIONAL REQUIREMENTS
================================================================================

✓ Users subscribe to notification types per channel
✓ Users manage global channel preferences (opt-in/out)
✓ Support 4 channels: Email, SMS, Push, In-App
✓ Notifications have priority levels: CRITICAL, HIGH, LOW
✓ Templates are channel-specific with placeholder substitution
✓ Failed deliveries retry with exponential backoff (max 3 retries)
✓ Track notification status: QUEUED → SENT → DELIVERED → READ / FAILED
✓ Render templates with dynamic payload data

================================================================================
3. NON-FUNCTIONAL REQUIREMENTS
================================================================================

• Scalability: Queue-based architecture allows horizontal scaling of dispatchers
• Reliability: Retry mechanism + status tracking ensures delivery attempts
• Extensibility: New channels added without modifying existing code
• Decoupling: Queue separates rendering from dispatch
• Performance: Async dispatch, in-memory queue for interview scope
• Maintainability: Clear separation of concerns via interfaces

================================================================================
4. CORE FLOW
================================================================================

Event Source (Order Service, Payment Service, etc.)
    ↓
NotificationService (Orchestrator)
    ├─ Fetch user
    ├─ Get active channels (subscription + preference filter)
    ├─ Create Notification objects (Status = QUEUED)
    ↓
TemplateRenderer
    ├─ Resolve template by (NotificationType, Channel)
    ├─ Replace {{placeholders}} with payload values
    ├─ Store rendered body in notification
    ↓
MessageQueue (Decoupling point)
    ├─ Enqueue rendered notifications
    ↓
ChannelDispatcher (Background worker)
    ├─ Dequeue notification
    ├─ Get channel via Factory
    ├─ Send via channel (with retry + exponential backoff)
    ├─ Update status on success/failure
    ↓
StatusTracker (State machine)
    └─ Enforce valid transitions: QUEUED → SENT → DELIVERED → READ / FAILED

================================================================================
5. KEY ENTITIES
================================================================================

USER
  Purpose: Represents a user in the system
  Fields:
    - Id (int)
    - Name, Email, PhoneNumber, DeviceToken (for Push)
    - Subscriptions (List<Subscription>)
    - Preference (NotificationPreference)

SUBSCRIPTION
  Purpose: User's opt-in for a specific NotificationType on a specific Channel
  Fields:
    - SubscriptionId (int)
    - UserId (int)
    - NotificationType (enum)
    - Channel (enum)
    - IsActive (bool)
  Example: User 42 wants ORDER_DELIVERED via EMAIL and SMS

NOTIFICATION
  Purpose: Represents a single notification instance to be sent
  Fields:
    - NotificationId (int)
    - UserId (int)
    - NotificationType (enum)
    - Priority (enum)
    - Channel (enum)
    - Payload (Dictionary<string, string>) — template variables
    - TemplateId (string)
    - RenderedBody (string) — final rendered message
    - Status (enum)
    - RetryCount (int)
    - CreatedAt (DateTime)

NOTIFICATIONPREFERENCE
  Purpose: Global channel-level opt-in/out for a user
  Fields:
    - UserId (int)
    - ChannelOptIn (Dictionary<Channel, bool>)
  Example: User has globally disabled SMS — no SMS regardless of subscriptions

MESSAGETEMPLATE
  Purpose: Channel-specific template with placeholders
  Fields:
    - TemplateId (string)
    - Channel (enum)
    - NotificationType (enum)
    - Subject (string) — for Email
    - Body (string) — "Hi {{name}}, your order {{orderId}} is delivered."

================================================================================
6. IMPORTANT ENUMS
================================================================================

NotificationType
  - OrderPlaced
  - OrderDelivered
  - PaymentFailed
  - Otp

Channel
  - Email
  - Sms
  - Push
  - InApp

NotificationPriority
  - Critical
  - High
  - Low

NotificationStatus
  - Queued
  - Sent
  - Delivered
  - Read
  - Failed

================================================================================
7. SERVICES AND RESPONSIBILITIES
================================================================================

NOTIFICATIONSERVICE (Orchestrator)
  Responsibility: Entry point. Coordinates the entire flow.
  Methods:
    - Send(userId, notificationType, priority, payload)
      • Fetch user
      • Get active channels via PreferenceService
      • Create Notification objects
      • Render templates
      • Save to repository
      • Enqueue to message queue
    - GetStatus(notificationId) → NotificationStatus

USERPREFERENCESERVICE
  Responsibility: Two-level filtering for active channels
  Methods:
    - GetActiveChannels(user, notificationType)
      • Check: subscription exists AND IsActive=true
      • Check: global channel opt-in = true
      • Return intersection
    - Subscribe(userId, type, channel)
    - Unsubscribe(userId, type, channel)
    - UpdateChannelOptIn(userId, channel, optIn)

TEMPLATERENDERER
  Responsibility: Resolve and render templates
  Methods:
    - Render(notification)
      • Fetch template by (NotificationType, Channel)
      • Replace {{key}} with payload[key] using regex
      • Store rendered body in notification
      • Return rendered string

CHANNELDISPATCHER
  Responsibility: Pull from queue, dispatch with retry
  Methods:
    - ProcessQueueAsync()
      • While queue not empty:
        - Dequeue notification
        - Get channel via factory
        - Attempt send (with retry loop)
        - Update status via StatusTracker

STATUSTRACKER (State machine)
  Responsibility: Enforce valid status transitions
  Methods:
    - Transition(notificationId, newStatus)
      • Validate transition is allowed
      • Update repository
      • Log transition
  Valid transitions:
    QUEUED → SENT, FAILED
    SENT → DELIVERED, FAILED
    DELIVERED → READ
    READ → (terminal)
    FAILED → (terminal)

RETRYPOLICY
  Responsibility: Encapsulate retry logic
  Methods:
    - ShouldRetry(retryCount) → bool
    - GetDelay(retryCount) → TimeSpan
  Implementation: ExponentialBackoffRetryPolicy
    - MaxRetries = 3
    - BaseDelay = 2 seconds
    - Delay = BaseDelay * 2^retryCount

================================================================================
8. INTERFACES
================================================================================

INotificationChannel
  Why: Strategy pattern. Dispatcher talks to interface, not concrete channels.
  Methods:
    - Task<bool> SendAsync(notification, user)
  Implementations: EmailChannel, SmsChannel, PushChannel, InAppChannel

ITemplateRenderer
  Why: Decouples template resolution from rendering logic.
  Methods:
    - string Render(notification)

IMessageQueue
  Why: Decouples rendering from dispatch. Allows async processing.
  Methods:
    - void Enqueue(notification)
    - Notification? Dequeue()
    - bool IsEmpty

IRetryPolicy
  Why: Encapsulates retry strategy. Easy to swap policies.
  Methods:
    - bool ShouldRetry(retryCount)
    - TimeSpan GetDelay(retryCount)

IUserPreferenceService
  Why: Centralizes preference logic. Two-level filtering.
  Methods:
    - IEnumerable<Channel> GetActiveChannels(user, notificationType)
    - void Subscribe(userId, type, channel)
    - void Unsubscribe(userId, type, channel)
    - void UpdateChannelOptIn(userId, channel, optIn)

IUserRepository, INotificationRepository, ITemplateRepository
  Why: Abstraction for data access. Easy to swap in-memory for DB.

================================================================================
9. DESIGN PATTERNS USED
================================================================================

STRATEGY PATTERN
  Where: INotificationChannel + concrete channels
  Why:
    - Each channel has different send logic (Email ≠ SMS ≠ Push)
    - Dispatcher doesn't care which channel it's using
    - New channels added without modifying dispatcher
  Example:
    INotificationChannel channel = factory.Get(Channel.Email);
    await channel.SendAsync(notification, user);

FACTORY PATTERN
  Where: NotificationChannelFactory
  Why:
    - Centralizes channel creation
    - Dispatcher never does "new EmailChannel()"
    - Adding WhatsApp = register in factory, zero changes elsewhere
  Implementation:
    Dictionary<Channel, INotificationChannel> _channels;
    INotificationChannel Get(Channel channel) → _channels[channel]

STATE PATTERN
  Where: StatusTracker + NotificationStatus enum
  Why:
    - Explicit state machine prevents invalid transitions
    - QUEUED → READ is invalid (must go through SENT, DELIVERED)
    - Testable: transition table is data-driven
  Implementation:
    Dictionary<Status, HashSet<Status>> _validTransitions
    Transition(id, newStatus) validates before updating

================================================================================
10. WHY OBSERVER PATTERN IS NOT PREFERRED
================================================================================

Observer Pattern:
  - Decouples event producers from consumers
  - Great for UI: button click → multiple listeners react
  - Subscribers pull/push independently

Why NOT here:
  ✗ No central coordination → retry logic scattered across subscribers
  ✗ No guaranteed delivery → if subscriber crashes, notification lost
  ✗ No status tracking → hard to know if notification was sent
  ✗ No preference filtering → each subscriber must check preferences
  ✗ No template rendering → each subscriber must render

Better approach: Orchestrator (NotificationService) controls the flow
  ✓ Single point of coordination
  ✓ Guaranteed delivery via queue + retry
  ✓ Centralized status tracking
  ✓ Centralized preference filtering
  ✓ Centralized template rendering

================================================================================
11. END-TO-END FLOW: ORDER_DELIVERED via EMAIL
================================================================================

Step 1: Event Source
  Order Service calls:
    notificationService.Send(
      userId: 1,
      type: NotificationType.OrderDelivered,
      priority: NotificationPriority.High,
      payload: { "name": "Alice", "orderId": "ORD-9921", "date": "2025-07-10" }
    )

Step 2: NotificationService — Fetch & Filter
  - Fetch User 1 from repository
  - Call preferenceService.GetActiveChannels(user, OrderDelivered)
    • Check subscriptions: User 1 subscribed to OrderDelivered on Email? YES
    • Check preference: User 1 has Email opt-in? YES
    • Return [Channel.Email]

Step 3: Create Notification
  notification = new Notification
  {
    UserId = 1,
    NotificationType = OrderDelivered,
    Channel = Email,
    Payload = { "name": "Alice", "orderId": "ORD-9921", "date": "2025-07-10" },
    Status = Queued
  }

Step 4: TemplateRenderer
  - Fetch template: (OrderDelivered, Email)
    Subject: "Your order {{orderId}} has been delivered!"
    Body: "Hi {{name}}, your order {{orderId}} was delivered on {{date}}."
  - Replace placeholders:
    Subject: "Your order ORD-9921 has been delivered!"
    Body: "Hi Alice, your order ORD-9921 was delivered on 2025-07-10."
  - Store in notification.RenderedBody

Step 5: Save & Enqueue
  - notificationRepository.Save(notification) → Status = Queued
  - messageQueue.Enqueue(notification)

Step 6: ChannelDispatcher (Background)
  - Dequeue notification
  - Get channel: factory.Get(Channel.Email) → EmailChannel
  - Send: await emailChannel.SendAsync(notification, user)
    • EmailVendor.SendAsync(user.Email, subject, body)
    • Returns true on success
  - statusTracker.Transition(notificationId, Sent)
    • Validates: Queued → Sent is valid ✓
    • Updates repository
    • Logs: "Notification 1: Queued → Sent"

Step 7: Status Query
  - Client calls: notificationService.GetStatus(notificationId)
  - Returns: NotificationStatus.Sent

================================================================================
12. RETRY MECHANISM — EXPONENTIAL BACKOFF
================================================================================

Scenario: Email vendor is temporarily down

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

Why exponential backoff?
  - Avoids hammering vendor with requests
  - Gives vendor time to recover
  - Reduces load during outages
  - Interview note: "In production, you'd add jitter to prevent thundering herd"

================================================================================
13. EXTENSIBILITY: ADDING WHATSAPP CHANNEL
================================================================================

Current state:
  - 4 channels: Email, SMS, Push, InApp
  - All implement INotificationChannel
  - Factory maps Channel enum → INotificationChannel

To add WhatsApp:

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
  templateRepository.Add(
    (NotificationType.OrderDelivered, Channel.WhatsApp),
    new MessageTemplate { Body = "Order {{orderId}} delivered!" }
  );

Changes required:
  ✓ NotificationService: ZERO changes
  ✓ ChannelDispatcher: ZERO changes
  ✓ StatusTracker: ZERO changes
  ✓ TemplateRenderer: ZERO changes
  ✓ Existing channels: ZERO changes

This is Open/Closed Principle: open for extension, closed for modification.

================================================================================
14. DATABASE TABLES
================================================================================

USERS
  - id (PK)
  - name
  - email
  - phone_number
  - device_token
  - created_at

SUBSCRIPTIONS
  - id (PK)
  - user_id (FK)
  - notification_type (enum)
  - channel (enum)
  - is_active (bool)
  - created_at
  - Unique constraint: (user_id, notification_type, channel)

NOTIFICATION_PREFERENCES
  - user_id (PK, FK)
  - channel (enum)
  - opt_in (bool)
  - Composite PK: (user_id, channel)

NOTIFICATIONS
  - id (PK)
  - user_id (FK)
  - notification_type (enum)
  - channel (enum)
  - priority (enum)
  - status (enum)
  - template_id
  - rendered_body (text)
  - retry_count
  - created_at
  - updated_at
  - Index: (user_id, status) for queries

MESSAGE_TEMPLATES
  - template_id (PK)
  - notification_type (enum)
  - channel (enum)
  - subject (nullable)
  - body (text)
  - Unique constraint: (notification_type, channel)

================================================================================
15. IMPORTANT APIs
================================================================================

POST /notifications/send
  Request:
    {
      "userId": 1,
      "notificationType": "OrderDelivered",
      "priority": "High",
      "payload": {
        "name": "Alice",
        "orderId": "ORD-9921",
        "date": "2025-07-10"
      }
    }
  Response: { "success": true }

POST /subscriptions
  Request:
    {
      "userId": 1,
      "notificationType": "OrderDelivered",
      "channel": "Email"
    }
  Response: { "subscriptionId": 42 }

DELETE /subscriptions/{subscriptionId}
  Response: { "success": true }

GET /notifications/{notificationId}/status
  Response: { "notificationId": 1, "status": "Sent" }

PUT /preferences/{userId}/channels/{channel}
  Request: { "optIn": false }
  Response: { "success": true }

GET /preferences/{userId}
  Response:
    {
      "userId": 1,
      "channelOptIn": {
        "Email": true,
        "Sms": true,
        "Push": false,
        "InApp": true
      }
    }

================================================================================
16. INTERVIEW DISCUSSION POINTS
================================================================================

TRADEOFFS

Queue vs Direct Dispatch:
  Queue (chosen):
    ✓ Decouples rendering from dispatch
    ✓ Allows async processing
    ✓ Survives dispatcher crashes (if persisted)
    ✗ Adds latency (notification not sent immediately)
  Direct:
    ✓ Lower latency
    ✗ Blocks on vendor calls
    ✗ Tight coupling

  Interview answer: "For critical notifications, we'd use queue. For real-time
  alerts, we might dispatch directly. Depends on SLA."

In-Memory vs Persistent Queue:
  In-Memory (chosen for interview):
    ✓ Simple, fast
    ✗ Loses messages on crash
  Persistent (production):
    ✓ Survives crashes
    ✗ More complex (SQS, RabbitMQ)

  Interview answer: "In production, we'd use SQS or RabbitMQ. For this design,
  in-memory is sufficient to show the architecture."

Retry Strategy:
  Exponential backoff (chosen):
    ✓ Reduces load during outages
    ✓ Gives vendor time to recover
    ✗ Slower recovery for transient failures
  Linear backoff:
    ✓ Faster recovery
    ✗ More aggressive on vendor
  No retry:
    ✓ Simplest
    ✗ Loses notifications

  Interview answer: "Exponential backoff is standard. For critical notifications,
  we might add jitter to prevent thundering herd."

SCALABILITY DISCUSSION

Current bottlenecks:
  - Single dispatcher thread
  - In-memory queue (limited by RAM)
  - Single database connection

How to scale:
  1. Multiple dispatcher instances
     - Each pulls from shared queue (SQS)
     - Horizontal scaling
  2. Database sharding
     - Shard by user_id
     - Reduces contention
  3. Template caching
     - Cache templates in memory
     - Reduces DB hits
  4. Channel-specific workers
     - Email worker pool
     - SMS worker pool
     - Different scaling needs

Interview answer: "The queue-based architecture naturally scales. Add more
dispatcher instances, they all pull from the same queue. For templates, we'd
cache. For database, we'd shard by user_id."

ASYNC QUEUE DISCUSSION

Why async?
  - Vendor calls are I/O bound (network latency)
  - Async allows dispatcher to handle multiple notifications concurrently
  - Better resource utilization

Example:
  // Sync (blocks)
  foreach (var notification in queue)
    channel.Send(notification);  // waits for vendor response

  // Async (concurrent)
  var tasks = queue.Select(n => channel.SendAsync(n));
  await Task.WhenAll(tasks);  // all in parallel

Interview answer: "Async is crucial for I/O-bound operations. We can dispatch
100 emails concurrently instead of sequentially. Huge throughput improvement."

WHY QUEUE IS USEFUL

1. Decoupling
   - Rendering doesn't wait for dispatch
   - Dispatch doesn't wait for rendering

2. Resilience
   - If dispatcher crashes, queue persists (with persistent queue)
   - Retry logic can be applied

3. Backpressure
   - If vendor is slow, queue builds up
   - Dispatcher can process at its own pace
   - Prevents overwhelming vendor

4. Monitoring
   - Queue depth = system health
   - If queue grows, dispatcher is slow

Interview answer: "Queue is the backbone. It decouples components, provides
resilience, and allows independent scaling."

COUPLING VS DECOUPLING

Tight coupling (bad):
  NotificationService → EmailChannel → EmailVendor
  If EmailVendor changes, NotificationService breaks

Loose coupling (good):
  NotificationService → INotificationChannel → EmailChannel → EmailVendor
  If EmailVendor changes, only EmailChannel breaks
  If we add WhatsApp, NotificationService doesn't change

How we achieved it:
  - Interfaces for all major components
  - Factory for channel creation
  - Dependency injection
  - Queue for async decoupling

Interview answer: "We use interfaces and dependency injection to decouple.
NotificationService doesn't know about concrete channels. Factory handles
creation. Queue decouples rendering from dispatch."

================================================================================
17. COMMON INTERVIEW QUESTIONS
================================================================================

Q1: How do you handle duplicate notifications?
A: Add idempotency key (UUID) to notification. Check if already sent before
   processing. Store in cache or database. Prevents duplicate sends if message
   is reprocessed.

Q2: What if a user unsubscribes while notification is in queue?
A: Two approaches:
   1. Check subscription status before sending (safe but adds latency)
   2. Accept race condition (notification might be sent after unsubscribe)
   For interview: "We'd check subscription status before dispatch. Small
   performance hit, but correct behavior."

Q3: How do you prioritize notifications?
A: Use priority queue instead of FIFO queue.
   - CRITICAL notifications dequeued first
   - HIGH next
   - LOW last
   For interview: "We'd maintain separate queues per priority or use a
   PriorityQueue<T>. CRITICAL notifications get processed first."

Q4: What if template is missing?
A: Fail gracefully:
   - Log error
   - Mark notification as FAILED
   - Alert ops team
   For interview: "We throw exception in TemplateRenderer, caught by
   dispatcher, status set to FAILED. Ops gets alerted."

Q5: How do you track delivery status?
A: Vendor webhooks:
   - Email vendor sends webhook when email is opened
   - SMS vendor sends webhook when SMS is delivered
   - We update notification status based on webhook
   For interview: "Vendors provide webhooks. We have an endpoint that receives
   them and updates status. Notification lifecycle: QUEUED → SENT → DELIVERED
   → READ."

Q6: How do you handle rate limiting?
A: Per-channel rate limiting:
   - Email: 100 per minute per user
   - SMS: 10 per minute per user
   - Check before enqueuing
   For interview: "We'd add rate limiter before enqueuing. Prevents spam.
   Returns error if limit exceeded."

Q7: What if database is down?
A: Depends on persistence:
   - In-memory queue: notifications lost
   - Persistent queue: notifications survive
   For interview: "With persistent queue (SQS), we can retry later. With
   in-memory, we lose them. In production, we'd use persistent queue."

Q8: How do you test this system?
A: Mock vendors, mock repositories:
   - Unit test each service
   - Integration test full flow
   - Mock INotificationChannel implementations
   For interview: "We'd mock vendors and repositories. Test happy path,
   retry logic, status transitions, preference filtering."

Q9: How do you monitor this system?
A: Metrics:
   - Queue depth
   - Dispatch latency
   - Success/failure rate per channel
   - Retry count distribution
   For interview: "We'd track queue depth, latency, success rate. Alert if
   queue grows or latency spikes."

Q10: Can you add a new channel in 5 minutes?
A: Yes. Create channel class, register in factory, add templates.
   For interview: "Yes. Create WhatsAppChannel, register in factory, add
   templates. Zero changes to existing code. That's the power of Strategy
   pattern and Factory pattern."

================================================================================
18. FINAL SUMMARY — REVISION CHECKLIST
================================================================================

ARCHITECTURE
  ☐ Orchestrator (NotificationService) coordinates flow
  ☐ Queue decouples rendering from dispatch
  ☐ Factory creates channels
  ☐ Dispatcher processes queue with retry
  ☐ StatusTracker enforces state machine

DESIGN PATTERNS
  ☐ Strategy: INotificationChannel + concrete channels
  ☐ Factory: NotificationChannelFactory
  ☐ State: StatusTracker + valid transitions

KEY SERVICES
  ☐ NotificationService: entry point, orchestration
  ☐ UserPreferenceService: two-level filtering
  ☐ TemplateRenderer: resolve + render templates
  ☐ ChannelDispatcher: queue processing + retry
  ☐ StatusTracker: state machine

INTERFACES
  ☐ INotificationChannel: strategy for channels
  ☐ ITemplateRenderer: template resolution
  ☐ IMessageQueue: async decoupling
  ☐ IRetryPolicy: retry strategy
  ☐ IUserPreferenceService: preference logic

FLOW
  ☐ Event → NotificationService.Send()
  ☐ Filter active channels (subscription + preference)
  ☐ Create Notification (Status = QUEUED)
  ☐ Render template (replace placeholders)
  ☐ Enqueue to message queue
  ☐ Dispatcher dequeues and sends (with retry)
  ☐ Update status (QUEUED → SENT → DELIVERED / FAILED)

EXTENSIBILITY
  ☐ Add new channel: create class, register in factory, add templates
  ☐ Zero changes to existing code
  ☐ Open/Closed Principle

TRADEOFFS
  ☐ Queue vs direct dispatch: latency vs decoupling
  ☐ In-memory vs persistent queue: simplicity vs resilience
  ☐ Exponential backoff: reduces load vs slower recovery

SCALABILITY
  ☐ Multiple dispatcher instances
  ☐ Database sharding by user_id
  ☐ Template caching
  ☐ Channel-specific worker pools

COMMON QUESTIONS
  ☐ Duplicates: idempotency key
  ☐ Unsubscribe race: check before dispatch
  ☐ Prioritization: priority queue
  ☐ Missing template: fail gracefully
  ☐ Delivery tracking: vendor webhooks
  ☐ Rate limiting: per-channel limits
  ☐ Database down: persistent queue survives
  ☐ Testing: mock vendors and repos
  ☐ Monitoring: queue depth, latency, success rate
  ☐ New channel: 5 minutes with factory pattern

================================================================================
QUICK REFERENCE: 60-90 MINUTE INTERVIEW FLOW
================================================================================

0-5 min: Clarify requirements
  - "Can I assume single service, not distributed?"
  - "Should I focus on architecture or implementation?"
  - "Any specific channels or just the 4 mentioned?"

5-15 min: High-level design
  - Draw flow: Event → Service → Queue → Dispatcher → Vendor
  - Mention key components: NotificationService, ChannelDispatcher, Queue
  - Explain why queue is useful

15-30 min: Detailed design
  - Entities: User, Subscription, Notification, Preference, Template
  - Services: NotificationService, PreferenceService, TemplateRenderer
  - Interfaces: INotificationChannel, ITemplateRenderer, IMessageQueue
  - Design patterns: Strategy, Factory, State

30-45 min: Implementation walkthrough
  - Show NotificationService.Send() logic
  - Show ChannelDispatcher.ProcessQueueAsync() logic
  - Show retry mechanism with exponential backoff
  - Show StatusTracker state machine

45-60 min: Extensibility & tradeoffs
  - "How do you add WhatsApp?" → Factory pattern
  - "Queue vs direct dispatch?" → Tradeoffs
  - "How do you scale?" → Multiple dispatchers, sharding
  - "How do you test?" → Mock vendors

60-75 min: Deep dive (if time)
  - Duplicate handling
  - Rate limiting
  - Monitoring
  - Database schema

75-90 min: Q&A and wrap-up
  - Answer interviewer questions
  - Discuss tradeoffs
  - Mention what you'd do differently in production

================================================================================
END OF NOTES
================================================================================
