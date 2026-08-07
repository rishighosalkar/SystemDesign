================================================================================
NOTIFICATION SYSTEM — QUICK REFERENCE CARD (1-PAGE CHEAT SHEET)
================================================================================

CORE FLOW (30 seconds)
  Event → NotificationService → Filter channels → Render template → Enqueue
  → ChannelDispatcher → Send (with retry) → StatusTracker → Update status

KEY ENTITIES (1 minute)
  User: id, email, phone, subscriptions, preference
  Subscription: userId, category (OrderUpdates/PaymentAlerts/Otp), isActive
  Notification: userId, type, channel, payload, status, retryCount
  Preference: userId, channelOptIn (dict: Email/SMS/Push/InApp → bool)
  Template: templateId, notificationType, channel, subject, body

SUBSCRIPTION vs PREFERENCE (1 minute)
  Subscription = "Which topics do I want?" (category-level)
    → Subscribe to OrderUpdates → get ALL order events automatically
    → One subscription covers the entire topic lifecycle
  Preference = "Which channels do I want?" (channel-level, global)
    → Enable Email + Push, disable SMS → applies to ALL subscribed categories

TWO-LEVEL FILTER (1 minute)
  Level 1: Is user subscribed to the category this event belongs to?
    → OrderDelivered → OrderUpdates → subscribed? YES/NO
  Level 2: Which channels has user globally enabled?
    → Return channels where ChannelOptIn = true
  Only send if Level 1 passes; channels from Level 2 determine where.

SERVICES (2 minutes)
  NotificationService: Send(userId, type, priority, payload)
    → Get user → Filter channels → Create notifications → Render → Enqueue
  
  UserPreferenceService: GetActiveChannels(user, notificationType)
    → Map type → category → check subscription → return enabled channels
  
  TemplateRenderer: Render(notification)
    → Fetch template → Replace {{placeholders}} → Store rendered body
  
  ChannelDispatcher: ProcessQueueAsync()
    → Dequeue → Get channel via factory → Send (with retry) → Update status
  
  StatusTracker: Transition(id, newStatus)
    → Validate transition → Update repo → Log

DESIGN PATTERNS (1 minute)
  Strategy: INotificationChannel (Email, SMS, Push, InApp)
    → Dispatcher talks to interface, not concrete classes
  
  Factory: NotificationChannelFactory
    → Maps Channel enum → INotificationChannel
    → Adding WhatsApp = one new class + register in factory
  
  State: StatusTracker
    → Valid transitions: QUEUED → SENT → DELIVERED → READ / FAILED
    → Prevents invalid jumps

WHY NOT OBSERVER (30 seconds)
  Observer = decoupled event listeners
  Problem: No central coordination for retry, status tracking, preferences
  Solution: Orchestrator (NotificationService) controls flow

END-TO-END: ORDER_DELIVERED via EMAIL (2 minutes)
  1. Order Service calls notificationService.Send(userId=1, OrderDelivered, High, payload)
  2. Map OrderDelivered → OrderUpdates category
  3. User subscribed to OrderUpdates? YES
  4. Enabled channels: Email=true, SMS=true, Push=false, InApp=true → [Email, SMS, InApp]
  5. Create Notification per channel (Status=QUEUED)
  6. TemplateRenderer: fetch template, replace {{name}}, {{orderId}}, {{date}}
  7. Save to repo, enqueue
  8. ChannelDispatcher dequeues, gets EmailChannel from factory
  9. EmailChannel.SendAsync() calls EmailVendor
  10. StatusTracker.Transition(id, SENT)

RETRY MECHANISM (1 minute)
  Exponential backoff: delay = 2s * 2^retryCount
  Attempt 0: fail → wait 2s
  Attempt 1: fail → wait 4s
  Attempt 2: fail → wait 8s
  Attempt 3: fail → Status = FAILED
  Why: Reduces load, gives vendor time to recover

ADDING WHATSAPP (1 minute)
  1. Add Channel.WhatsApp to enum
  2. Create WhatsAppChannel : INotificationChannel
  3. Register in factory
  4. Add templates for (NotificationType, Channel.WhatsApp)
  Changes to existing code: ZERO

INTERFACES (1 minute)
  INotificationChannel: SendAsync(notification, user) → bool
  ITemplateRenderer: Render(notification) → string
  IMessageQueue: Enqueue, Dequeue, IsEmpty
  IRetryPolicy: ShouldRetry(count), GetDelay(count)
  IUserPreferenceService: GetActiveChannels, Subscribe(category), Unsubscribe(category), UpdateOptIn

DATABASE TABLES (1 minute)
  users: id, name, email, phone, device_token
  subscriptions: id, user_id, category, is_active   ← category, not per-type
  notification_preferences: user_id, channel, opt_in
  notifications: id, user_id, type, channel, status, rendered_body, retry_count
  message_templates: template_id, type, channel, subject, body

TRADEOFFS (2 minutes)
  Queue vs Direct:
    Queue: decoupled, async, survives crashes (if persistent) ✓
    Direct: lower latency ✓
    → Choose queue for reliability
  
  In-Memory vs Persistent Queue:
    In-Memory: simple, fast ✓
    Persistent: survives crashes ✓
    → In-memory for interview, SQS/RabbitMQ for production
  
  Exponential vs Linear Backoff:
    Exponential: reduces load ✓
    Linear: faster recovery ✓
    → Exponential is standard

SCALABILITY (1 minute)
  Multiple dispatcher instances → pull from shared queue
  Database sharding by user_id → reduce contention
  Template caching → reduce DB hits
  Channel-specific worker pools → different scaling needs

COMMON Q&A (3 minutes)
  Q: Duplicates?
  A: Idempotency key, check before sending
  
  Q: Unsubscribe race?
  A: Check subscription status before dispatch
  
  Q: Prioritization?
  A: Priority queue (CRITICAL > HIGH > LOW)
  
  Q: Missing template?
  A: Fail gracefully, mark as FAILED, alert ops
  
  Q: Delivery tracking?
  A: Vendor webhooks update status
  
  Q: Rate limiting?
  A: Per-channel limits before enqueuing
  
  Q: Database down?
  A: Persistent queue survives, in-memory loses messages
  
  Q: Testing?
  A: Mock vendors and repositories
  
  Q: Monitoring?
  A: Queue depth, latency, success rate
  
  Q: New channel in 5 min?
  A: Yes, factory pattern + templates

INTERVIEW FLOW (60-90 min)
  0-5 min: Clarify requirements
  5-15 min: High-level design (draw flow)
  15-30 min: Detailed design (entities, services, interfaces)
  30-45 min: Implementation walkthrough (code snippets)
  45-60 min: Extensibility & tradeoffs
  60-75 min: Deep dive (duplicates, rate limiting, monitoring)
  75-90 min: Q&A

REVISION CHECKLIST
  ☐ Can explain core flow in 30 seconds
  ☐ Can draw architecture diagram
  ☐ Can explain subscription (category) vs preference (channel)
  ☐ Can explain 3 design patterns
  ☐ Can walk through ORDER_DELIVERED example
  ☐ Can explain retry mechanism
  ☐ Can explain how to add WhatsApp
  ☐ Can discuss tradeoffs
  ☐ Can answer 10 common questions
  ☐ Can explain why queue is useful
  ☐ Can explain why Observer is not preferred

================================================================================
