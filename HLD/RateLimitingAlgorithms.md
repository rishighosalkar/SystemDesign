# Rate Limiting Algorithms — System Design Notes

## 1. Why Rate Limiting?

Rate limiting controls the number of requests a client can make to a service within a time window. It is critical for:

- **Protecting resources** — prevents server overload and cascading failures
- **Fair usage** — ensures no single client monopolizes capacity
- **Cost control** — caps expensive downstream calls (DB, third-party APIs)
- **Security** — mitigates brute-force attacks, DDoS, and credential stuffing
- **SLA enforcement** — enforces contractual API quotas per tier

### Where Rate Limiters Live

```
Client → [API Gateway / Load Balancer] → Rate Limiter → Application Server → DB
                                              ↑
                                     Redis / In-Memory Store
```

- **Edge level** — API Gateway (AWS API Gateway, Kong, Envoy)
- **Middleware level** — application-side interceptor
- **Distributed** — backed by Redis/Memcached for multi-node consistency

---

## 2. Fixed Window Counter

### Concept

Divides time into fixed-size windows (e.g., 60s). Each window has a counter. Request is allowed if counter < limit; otherwise rejected.

```
Timeline:  |----Window 1----|----Window 2----|----Window 3----|
Counter:        12/100            0/100            0/100
```

### Pros & Cons

| Pros | Cons |
|------|------|
| Simple to implement | **Boundary burst problem** — 100 reqs at 0:59 + 100 at 1:00 = 200 in 2 seconds |
| Low memory (1 counter + 1 timestamp per key) | Not smooth; traffic spikes at window edges |
| O(1) per request | Can temporarily exceed intended rate by ~2x |

### When to Use

- Simple quota enforcement where occasional bursts at boundaries are acceptable
- Per-day or per-hour limits where precision isn't critical

### Code Example (C#)

```csharp
public class FixedWindowRateLimiter
{
    private readonly int _maxRequests;
    private readonly int _windowSeconds;
    private readonly Dictionary<string, (long WindowStart, int Count)> _windows = new();

    public FixedWindowRateLimiter(int maxRequests, int windowSeconds)
    {
        _maxRequests = maxRequests;
        _windowSeconds = windowSeconds;
    }

    public bool Allow(string key)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long windowStart = now - (now % _windowSeconds);

        if (!_windows.TryGetValue(key, out var entry) || entry.WindowStart != windowStart)
            _windows[key] = (windowStart, 0);

        var (start, count) = _windows[key];
        if (count < _maxRequests)
        {
            _windows[key] = (start, count + 1);
            return true;
        }
        return false;
    }
}
```

### Redis Implementation (Atomic)

```lua
-- KEYS[1] = rate_limit:{client_id}:{window}
-- ARGV[1] = max_requests, ARGV[2] = window_seconds
local current = redis.call('INCR', KEYS[1])
if current == 1 then
    redis.call('EXPIRE', KEYS[1], ARGV[2])
end
return current <= tonumber(ARGV[1])
```

### Boundary Burst Problem — Visual

```
Window 1 (0:00 - 0:59)          Window 2 (1:00 - 1:59)
                    |||||||||||  |||||||||||
                    100 reqs     100 reqs
                    at 0:50      at 1:00
                    ← 200 requests in 10 seconds! →
```

---

## 3. Sliding Window Log

### Concept

Stores the timestamp of every request in a sorted log. To check the limit, count entries within `[now - window_size, now]`. Removes expired entries.

```
Log: [1:00:01, 1:00:03, 1:00:07, 1:00:45, 1:00:58]
Window = 60s, now = 1:01:02
Valid entries (after 1:00:02): [1:00:03, 1:00:07, 1:00:45, 1:00:58] → 4 requests
```

### Pros & Cons

| Pros | Cons |
|------|------|
| **Precise** — no boundary burst problem | High memory — stores every timestamp |
| Smooth rate enforcement | O(n) cleanup per check |
| Accurate count at any point in time | Not practical at high throughput (millions of reqs) |

### Code Example (C#)

```csharp
public class SlidingWindowLogLimiter
{
    private readonly int _maxRequests;
    private readonly int _windowSeconds;
    private readonly Dictionary<string, LinkedList<double>> _logs = new();

    public SlidingWindowLogLimiter(int maxRequests, int windowSeconds)
    {
        _maxRequests = maxRequests;
        _windowSeconds = windowSeconds;
    }

    public bool Allow(string key)
    {
        double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        if (!_logs.ContainsKey(key))
            _logs[key] = new LinkedList<double>();

        var log = _logs[key];

        // Evict expired entries
        while (log.Count > 0 && log.First!.Value <= now - _windowSeconds)
            log.RemoveFirst();

        if (log.Count < _maxRequests)
        {
            log.AddLast(now);
            return true;
        }
        return false;
    }
}
```

---

## 4. Sliding Window Counter (Hybrid)

### Concept

Combines Fixed Window + Sliding Window Log. Keeps counters for the current and previous window, then uses a **weighted average** based on overlap.

```
Previous Window (0:00-0:59): 84 requests
Current Window  (1:00-1:59): 36 requests
Now = 1:00:15 → 15s into current window → 75% of prev window overlaps

Weighted count = 84 * 0.75 + 36 = 63 + 36 = 99
Limit = 100 → ALLOWED
```

### Pros & Cons

| Pros | Cons |
|------|------|
| Smooths the boundary burst problem | Approximate (not exact) |
| Low memory — only 2 counters per key | Slight over/under counting possible |
| O(1) per request | Slightly more complex than fixed window |

### When to Use

- **Best general-purpose choice** — used by Cloudflare, Stripe, and most API gateways
- When you need better accuracy than fixed window without the memory cost of sliding log

### Code Example (C#)

```csharp
public class SlidingWindowCounterLimiter
{
    private readonly int _maxRequests;
    private readonly int _windowSeconds;
    private readonly Dictionary<string, (int PrevCount, int CurrCount, long PrevWindow, long CurrWindow)> _state = new();

    public SlidingWindowCounterLimiter(int maxRequests, int windowSeconds)
    {
        _maxRequests = maxRequests;
        _windowSeconds = windowSeconds;
    }

    public bool Allow(string key)
    {
        double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        long currWindowStart = (long)now - ((long)now % _windowSeconds);

        if (!_state.TryGetValue(key, out var s) || s.CurrWindow != currWindowStart)
        {
            // Rotate windows
            int prevCount = _state.ContainsKey(key) ? _state[key].CurrCount : 0;
            _state[key] = (prevCount, 0, _state.ContainsKey(key) ? _state[key].CurrWindow : currWindowStart - _windowSeconds, currWindowStart);
        }

        var state = _state[key];
        double elapsed = now - state.CurrWindow;
        double overlap = 1.0 - (elapsed / _windowSeconds);
        double weighted = state.PrevCount * overlap + state.CurrCount;

        if (weighted < _maxRequests)
        {
            _state[key] = (state.PrevCount, state.CurrCount + 1, state.PrevWindow, state.CurrWindow);
            return true;
        }
        return false;
    }
}
```

---

## 5. Token Bucket

### Concept

A bucket holds tokens (max = bucket capacity). Tokens are added at a fixed rate. Each request consumes 1 (or more) tokens. If the bucket is empty, the request is rejected.

```
Bucket Capacity: 10 tokens
Refill Rate: 2 tokens/sec

Time 0s:  [●●●●●●●●●●]  10 tokens — full
Time 0s:  Request → consumes 1 → [●●●●●●●●●○]  9 tokens
Time 1s:  +2 refilled → [●●●●●●●●●●]  10 tokens (capped at capacity)
Time 1s:  Burst of 10 → [○○○○○○○○○○]  0 tokens
Time 2s:  +2 refilled → [●●○○○○○○○○]  2 tokens
```

### Pros & Cons

| Pros | Cons |
|------|------|
| **Allows controlled bursts** up to bucket size | Two parameters to tune (rate + capacity) |
| Smooth long-term rate | Burst can overwhelm downstream if capacity is too high |
| Memory efficient — 2 values per key | Slightly more complex than fixed window |
| Industry standard (AWS, Stripe, GCP) | |

### When to Use

- **Most common choice for API rate limiting**
- When you want to allow short bursts but enforce an average rate
- AWS API Gateway, S3, EC2 API all use token bucket

### Code Example (C#)

```csharp
public class TokenBucketLimiter
{
    private readonly int _capacity;
    private readonly double _refillRate; // tokens per second
    private readonly Dictionary<string, (double Tokens, double LastRefill)> _buckets = new();

    public TokenBucketLimiter(int capacity, double refillRate)
    {
        _capacity = capacity;
        _refillRate = refillRate;
    }

    public bool Allow(string key, int tokens = 1)
    {
        double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        if (!_buckets.ContainsKey(key))
            _buckets[key] = (_capacity, now);

        var (currentTokens, lastRefill) = _buckets[key];

        // Refill tokens based on elapsed time
        double elapsed = now - lastRefill;
        currentTokens = Math.Min(_capacity, currentTokens + elapsed * _refillRate);

        if (currentTokens >= tokens)
        {
            _buckets[key] = (currentTokens - tokens, now);
            return true;
        }

        _buckets[key] = (currentTokens, now);
        return false;
    }
}
```

### Key Insight — Burst vs Sustained Rate

```
Capacity = 100, Refill = 10/sec

Scenario A (Burst):    100 requests at t=0 → ALL allowed, then 0 for 10s
Scenario B (Steady):   10 requests/sec → always allowed, never throttled
Scenario C (Mixed):    50 burst + 10/sec sustained → works smoothly
```

---

## 6. Leaky Bucket

### Concept

Requests enter a FIFO queue (the bucket). The bucket "leaks" (processes) at a fixed rate. If the bucket is full, new requests are dropped.

Think of it as water dripping from a bucket at a constant rate — no matter how fast you pour water in.

```
Incoming:  ████████████  (bursty traffic)
                ↓
         ┌──────────┐
Bucket:  │ ● ● ● ● │  (queue, max size = 4)
         └────┬─────┘
              ↓  leaks at fixed rate
Output:  ●  ●  ●  ●    (smooth, constant rate)
```

### Pros & Cons

| Pros | Cons |
|------|------|
| **Perfectly smooth output rate** | No burst tolerance — even legitimate bursts are queued |
| Prevents downstream overload | Adds latency (requests wait in queue) |
| Simple queue-based implementation | Old requests may become stale in queue |
| Predictable processing rate | Not ideal when bursts are acceptable |

### When to Use

- When downstream systems need a **constant, predictable** request rate
- Network traffic shaping (e.g., TCP congestion control)
- When you want to **smooth out** bursty traffic completely

### Code Example (C#)

```csharp
public class LeakyBucketLimiter
{
    private readonly int _capacity;
    private readonly double _leakRate; // requests processed per second
    private readonly Queue<string> _queue = new();
    private double _lastLeakTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    public LeakyBucketLimiter(int capacity, double leakRate)
    {
        _capacity = capacity;
        _leakRate = leakRate;
    }

    private void Leak()
    {
        double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        int leaked = (int)((now - _lastLeakTime) * _leakRate);
        for (int i = 0; i < Math.Min(leaked, _queue.Count); i++)
            _queue.Dequeue();
        if (leaked > 0)
            _lastLeakTime = now;
    }

    public bool Allow(string key)
    {
        Leak();
        if (_queue.Count < _capacity)
        {
            _queue.Enqueue(key);
            return true;
        }
        return false;
    }
}
```

---

## 7. Comparison Matrix

| Criteria | Fixed Window | Sliding Window Log | Sliding Window Counter | Token Bucket | Leaky Bucket |
|---|---|---|---|---|---|
| **Accuracy** | Low (boundary burst) | Exact | Approximate | Good | Exact |
| **Memory** | O(1) per key | O(n) per key | O(1) per key | O(1) per key | O(bucket_size) |
| **Time Complexity** | O(1) | O(n) | O(1) | O(1) | O(1) amortized |
| **Burst Handling** | Allows 2x at edges | No burst | Minimal burst | ✅ Controlled burst | ❌ No burst |
| **Smoothness** | Choppy | Smooth | Smooth | Moderate | Perfectly smooth |
| **Implementation** | Trivial | Moderate | Moderate | Moderate | Simple |
| **Best For** | Simple quotas | Precise limiting | General-purpose APIs | API rate limiting | Traffic shaping |

---

## 8. Token Bucket vs Leaky Bucket — Key Difference

This is the most commonly asked comparison in system design interviews:

```
Token Bucket:
  - Controls the AVERAGE rate but ALLOWS BURSTS
  - Bucket stores TOKENS (permission to send)
  - Empty bucket = reject
  - Full bucket = max burst capacity

Leaky Bucket:
  - Enforces a CONSTANT output rate, NO BURSTS
  - Bucket stores REQUESTS (actual work items)
  - Full bucket = reject
  - Processes at fixed rate regardless of input
```

**Analogy:**
- Token Bucket = "You have a budget of 100 API calls. Spend them however you want, but you earn 10 more per second."
- Leaky Bucket = "I will process exactly 10 requests per second, no matter what. Extras wait in line or get dropped."

---

## 9. Distributed Rate Limiting

In a multi-server environment, local counters are insufficient. Common approaches:

### Centralized Store (Redis)

```
Server A ──┐
Server B ──┼──→ Redis (INCR + EXPIRE) ──→ Single source of truth
Server C ──┘
```

- Use Redis `INCR` with `EXPIRE` for fixed window
- Use Redis sorted sets (`ZADD`, `ZRANGEBYSCORE`, `ZCARD`) for sliding window log
- Use Lua scripts for atomicity

### Race Condition Mitigation

```lua
-- Atomic sliding window in Redis (Lua script)
local key = KEYS[1]
local window = tonumber(ARGV[1])
local limit = tonumber(ARGV[2])
local now = tonumber(ARGV[3])

redis.call('ZREMRANGEBYSCORE', key, 0, now - window)
local count = redis.call('ZCARD', key)
if count < limit then
    redis.call('ZADD', key, now, now .. math.random())
    redis.call('EXPIRE', key, window)
    return 1
end
return 0
```

### Sticky Sessions

Route the same client to the same server → local rate limiter works. Downside: uneven load distribution.

---

## 10. HTTP Response Headers (Standard Practice)

When rate limiting, always return informative headers:

```
HTTP/1.1 429 Too Many Requests
Retry-After: 30
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1672531260
```

| Header | Meaning |
|--------|---------|
| `429` | Standard HTTP status for rate limiting |
| `Retry-After` | Seconds until the client should retry |
| `X-RateLimit-Limit` | Max requests allowed in window |
| `X-RateLimit-Remaining` | Requests left in current window |
| `X-RateLimit-Reset` | Unix timestamp when the window resets |

---

## 11. System Design Interview Tips

1. **Clarify requirements first** — ask about scale (requests/sec), single vs multi-server, burst tolerance
2. **Start with Token Bucket** — it's the most versatile and commonly expected answer
3. **Mention Sliding Window Counter** as the best window-based alternative
4. **Always discuss distributed concerns** — Redis, race conditions, consistency
5. **Mention HTTP 429 + headers** — shows production awareness
6. **Discuss where to place the limiter** — API gateway vs middleware vs application layer
7. **Consider multi-tier limiting** — per-user + per-IP + global limits

### Decision Flowchart

```
Need rate limiting?
  │
  ├─ Need to allow bursts? ──→ YES ──→ Token Bucket
  │                           NO
  │                            ↓
  ├─ Need perfectly smooth output? ──→ YES ──→ Leaky Bucket
  │                                    NO
  │                                     ↓
  ├─ Need high precision? ──→ YES ──→ Sliding Window Log (if memory allows)
  │                           NO          or Sliding Window Counter
  │                            ↓
  └─ Simple quota? ──→ Fixed Window Counter
```

---

## 12. Real-World Usage

| Company/Service | Algorithm | Notes |
|---|---|---|
| **AWS API Gateway** | Token Bucket | Configurable burst + steady rate |
| **Stripe** | Sliding Window | Per-key rate limits with headers |
| **Cloudflare** | Sliding Window Counter | Edge-level, distributed |
| **Google Cloud** | Token Bucket | Per-project quotas |
| **Nginx** | Leaky Bucket | `limit_req` module uses leaky bucket |
| **GitHub API** | Fixed Window | 5000 req/hour per token |
| **Twitter/X API** | Fixed Window | 15-min windows per endpoint |
