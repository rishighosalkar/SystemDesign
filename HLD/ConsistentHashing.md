# Consistent Hashing — System Design Notes

## 1. The Problem: Why Not Simple Hashing?

In a distributed system, we need to map data (keys) to servers. The naive approach:

```
server_index = hash(key) % N       (N = number of servers)
```

### What Happens When a Server is Added/Removed?

```
N = 4 servers:
  hash("user:101") % 4 = 2  → Server 2
  hash("user:202") % 4 = 0  → Server 0
  hash("user:303") % 4 = 1  → Server 1

N = 5 servers (added one):
  hash("user:101") % 5 = 1  → Server 1  ← MOVED
  hash("user:202") % 5 = 2  → Server 2  ← MOVED
  hash("user:303") % 5 = 3  → Server 3  ← MOVED
```

**~80% of keys get remapped** when going from 4 → 5 servers. For a cache cluster, this means a **cache avalanche** — nearly all cached data becomes invalid simultaneously, causing a thundering herd to the database.

### Impact at Scale

| Servers | Added 1 Server | Keys Remapped |
|---------|----------------|---------------|
| 4 → 5 | +1 | ~80% |
| 10 → 11 | +1 | ~90% |
| 100 → 101 | +1 | ~99% |

This is catastrophic for caches, sharded databases, and CDNs.

---

## 2. Consistent Hashing — Core Idea

Instead of `hash % N`, we place both **servers and keys on a circular ring** (hash space 0 to 2³² - 1). A key is assigned to the **first server encountered moving clockwise** on the ring.

```
                    0
                    │
            ┌───────┼───────┐
           S3       │       S1
          (300)     │     (100)
            │       │       │
            │       │       │
    ────────┘       │       └────────
                    │
            ┌───────┼───────┐
            │       │       │
           S2       │
          (200)     │
            │       │
            └───────┘

  Key "user:101" hashes to position 120 → walks clockwise → hits S2 (200)
  Key "user:202" hashes to position 250 → walks clockwise → hits S3 (300)
  Key "user:303" hashes to position 50  → walks clockwise → hits S1 (100)
```

### What Happens When a Server is Added/Removed?

**Adding S4 at position 150:**

```
Before:  Keys in range (100, 200] → S2
After:   Keys in range (100, 150] → S4 (new)
         Keys in range (150, 200] → S2 (unchanged)
```

Only keys between S1 and S4 are remapped. **On average, only K/N keys move** (K = total keys, N = servers).

**Removing S2:**

```
Before:  Keys in range (100, 200] → S2
After:   Keys in range (100, 300] → S3 (absorbs S2's range)
```

Only S2's keys are redistributed — everything else stays put.

### Remapping Comparison

| Operation | Simple Hash (% N) | Consistent Hashing |
|-----------|-------------------|-------------------|
| Add 1 server to 10 | ~90% keys move | ~10% keys move (K/N) |
| Remove 1 server from 10 | ~90% keys move | ~10% keys move (K/N) |
| Add 1 server to 100 | ~99% keys move | ~1% keys move |

---

## 3. The Non-Uniformity Problem

With only N physical nodes on the ring, the distribution is often **uneven**:

```
        0
        │
   S1 ──┤ (pos 10)
   S2 ──┤ (pos 20)
   S3 ──┤ (pos 30)
        │
        │  ← S3 owns this HUGE range (30 → 10, wrapping around)
        │
        │
```

S3 handles ~97% of the ring. S1 and S2 handle ~1.5% each. This is **hot partition** — one server is overloaded.

---

## 4. Virtual Nodes (Vnodes) — The Solution

Each physical server gets **multiple positions** on the ring (virtual nodes). With 150 vnodes per server, the distribution becomes nearly uniform.

```
        0
        │
  S1_v1 ┤ (pos 15)
  S3_v2 ┤ (pos 45)
  S2_v1 ┤ (pos 80)
  S1_v2 ┤ (pos 130)
  S3_v1 ┤ (pos 200)
  S2_v2 ┤ (pos 270)
  S2_v3 ┤ (pos 330)
  S1_v3 ┤ (pos 350)
        │
```

Now each server owns multiple smaller segments → **much more even distribution**.

### How Many Vnodes?

| Vnodes per Server | Std Deviation of Load |
|-------------------|-----------------------|
| 1 (no vnodes) | ~50-80% imbalance |
| 50 | ~10% imbalance |
| 100-150 | ~5% imbalance |
| 200+ | ~2-3% imbalance (diminishing returns) |

**Trade-off:** More vnodes = better balance but more memory and slower lookups.

### Heterogeneous Servers

Vnodes naturally support servers with different capacities:

```
Server A (16 GB RAM) → 200 vnodes
Server B (8 GB RAM)  → 100 vnodes
Server C (4 GB RAM)  → 50 vnodes
```

Server A handles ~2x the load of B and ~4x the load of C.

---

## 5. Code Example — Consistent Hash Ring (C#)

```csharp
using System.Security.Cryptography;
using System.Text;

public class ConsistentHashRing
{
    private readonly int _numVnodes;
    private readonly SortedDictionary<uint, string> _ring = new();
    private readonly HashSet<string> _nodes = new();

    public ConsistentHashRing(int numVnodes = 150) => _numVnodes = numVnodes;

    private static uint Hash(string key)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(key));
        return BitConverter.ToUInt32(bytes, 0);
    }

    public void AddNode(string node)
    {
        _nodes.Add(node);
        for (int i = 0; i < _numVnodes; i++)
            _ring[Hash($"{node}:vnode{i}")] = node;
    }

    public void RemoveNode(string node)
    {
        _nodes.Remove(node);
        for (int i = 0; i < _numVnodes; i++)
            _ring.Remove(Hash($"{node}:vnode{i}"));
    }

    public string? GetNode(string key)
    {
        if (_ring.Count == 0) return null;
        uint h = Hash(key);
        // Walk clockwise: find first key >= h
        foreach (var entry in _ring)
            if (entry.Key >= h) return entry.Value;
        return _ring.First().Value; // wrap around
    }
}

// --- Usage ---
var ring = new ConsistentHashRing(numVnodes: 150);
ring.AddNode("server-1");
ring.AddNode("server-2");
ring.AddNode("server-3");

foreach (var key in new[] { "user:101", "user:202", "user:303", "order:500", "session:abc" })
    Console.WriteLine($"{key} → {ring.GetNode(key)}");

// Add a new server — only ~1/4 of keys will remap
ring.AddNode("server-4");
Console.WriteLine("\nAfter adding server-4:");
foreach (var key in new[] { "user:101", "user:202", "user:303", "order:500", "session:abc" })
    Console.WriteLine($"{key} → {ring.GetNode(key)}");
```

### Key Implementation Details

- **SortedDictionary** — keeps ring positions sorted; clockwise walk via linear scan or LINQ
- **MD5 hash** — provides good uniform distribution; not for security, just distribution
- **Wrap-around** — if no key >= hash, take the first entry (ring wraps to 0)

---

## 6. Generic Version with O(log V) Lookup

```csharp
using System.Security.Cryptography;
using System.Text;

public class ConsistentHashRing<T> where T : notnull
{
    private readonly int _numVnodes;
    private readonly List<uint> _sortedKeys = new();
    private readonly Dictionary<uint, T> _ring = new();
    private readonly HashSet<T> _nodes = new();

    public ConsistentHashRing(int numVnodes = 150) => _numVnodes = numVnodes;

    private static uint Hash(string key)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(key));
        return BitConverter.ToUInt32(bytes, 0);
    }

    public void AddNode(T node)
    {
        _nodes.Add(node);
        for (int i = 0; i < _numVnodes; i++)
        {
            uint h = Hash($"{node}:vnode{i}");
            _ring[h] = node;
            _sortedKeys.Add(h);
        }
        _sortedKeys.Sort();
    }

    public void RemoveNode(T node)
    {
        _nodes.Remove(node);
        for (int i = 0; i < _numVnodes; i++)
        {
            uint h = Hash($"{node}:vnode{i}");
            _ring.Remove(h);
            _sortedKeys.Remove(h);
        }
    }

    public T? GetNode(string key)
    {
        if (_sortedKeys.Count == 0) return default;
        uint h = Hash(key);
        // Binary search for clockwise walk — O(log V)
        int idx = _sortedKeys.BinarySearch(h);
        if (idx < 0) idx = ~idx;          // bitwise complement = insertion point
        if (idx >= _sortedKeys.Count) idx = 0; // wrap around
        return _ring[_sortedKeys[idx]];
    }
}
```

`List<T>.BinarySearch` returns the bitwise complement of the insertion point when not found — giving us O(log V) clockwise lookup.

---

## 7. Replication with Consistent Hashing

For fault tolerance, data is replicated to the **next R distinct physical nodes** clockwise on the ring.

```
Replication Factor R = 3

Key "user:101" hashes to position 120

Walk clockwise:
  pos 130 → S1_v2 → Primary   (S1)
  pos 200 → S3_v1 → Replica 1 (S3)  ← skip if same physical node
  pos 270 → S2_v2 → Replica 2 (S2)

Data stored on: S1 (primary), S3 (replica), S2 (replica)
```

### Code for Replica Selection

```csharp
public List<string> GetReplicas(string key, int numReplicas = 3)
{
    if (_sortedKeys.Count == 0) return new();

    uint h = Hash(key);
    int idx = _sortedKeys.BinarySearch(h);
    if (idx < 0) idx = ~idx;

    var replicas = new List<string>();
    var seen = new HashSet<string>();

    for (int i = 0; i < _sortedKeys.Count && replicas.Count < numReplicas; i++)
    {
        int pos = (idx + i) % _sortedKeys.Count;
        string node = _ring[_sortedKeys[pos]];
        if (seen.Add(node))
            replicas.Add(node);
    }
    return replicas;
}
```

---

## 8. Handling Node Failures — Data Migration

### Node Goes Down

```
Before failure:
  S1 owns range (S3, S1]
  S2 owns range (S1, S2]
  S3 owns range (S2, S3]

S2 fails:
  S3 absorbs S2's range → S3 now owns (S1, S3]
  Only S2's keys need to be served by S3
  With replication, S3 already has copies → zero data loss
```

### Node Added (Scale Out)

```
Add S4 between S1 and S2:
  S4 takes over range (S1, S4] from S2
  S2's range shrinks to (S4, S2]

Migration:
  Only keys in (S1, S4] move from S2 → S4
  All other keys stay put
```

### Minimizing Migration Impact

1. **Background transfer** — copy data to new node before updating the ring
2. **Double-read period** — read from both old and new node during migration
3. **Consistent hashing + replication** — new node can serve reads from replicas immediately

---

## 9. Bounded-Load Consistent Hashing (Google, 2017)

Standard consistent hashing can still create hot spots when certain keys are extremely popular (e.g., a viral tweet). Google's bounded-load variant adds a **load ceiling** per server.

### Concept

Each server has a max load = `average_load × (1 + ε)` where ε is a tunable parameter (e.g., 0.25).

```
Average load = total_keys / num_servers = 1000 / 4 = 250
ε = 0.25
Max load per server = 250 × 1.25 = 312

Key "viral_post" → hashes to S1
  S1 current load = 312 (at capacity!)
  → Walk clockwise to S2 (load = 200) → ASSIGNED to S2
```

```csharp
private readonly Dictionary<string, int> _load = new();

public string? GetNodeBounded(string key, double loadFactor = 1.25)
{
    if (_sortedKeys.Count == 0) return null;

    int totalKeys = _load.Values.Sum() + 1;
    double avgLoad = (double)totalKeys / _nodes.Count;
    int maxLoad = (int)(avgLoad * loadFactor) + 1;

    uint h = Hash(key);
    int idx = _sortedKeys.BinarySearch(h);
    if (idx < 0) idx = ~idx;

    for (int i = 0; i < _sortedKeys.Count; i++)
    {
        int pos = (idx + i) % _sortedKeys.Count;
        string node = _ring[_sortedKeys[pos]];
        if (_load.GetValueOrDefault(node, 0) < maxLoad)
        {
            _load[node] = _load.GetValueOrDefault(node, 0) + 1;
            return node;
        }
    }
    return null; // all nodes at capacity
}
```

**Used by:** Google Cloud Load Balancer, HAProxy, Vimeo

---

## 10. Jump Consistent Hashing

An alternative to ring-based consistent hashing. Uses a mathematical formula instead of a ring — **zero memory overhead**.

```csharp
/// Google's Jump Consistent Hash — O(ln n) time, O(1) space.
public static int JumpHash(ulong key, int numBuckets)
{
    long b = -1, j = 0;
    while (j < numBuckets)
    {
        b = j;
        key = key * 2862933555777941757UL + 1;
        j = (long)((b + 1) * ((1L << 31) / (double)((key >> 33) + 1)));
    }
    return (int)b;
}
```

### Comparison with Ring-Based

| Aspect | Ring-Based | Jump Hash |
|--------|-----------|-----------|
| Memory | O(N × V) for vnodes | O(1) |
| Lookup | O(log(N × V)) | O(log N) |
| Add/Remove any node | ✅ Yes | ❌ Only append/remove last |
| Named nodes | ✅ Yes | ❌ Numbered buckets only |
| Use case | General distributed systems | Sequential scaling (add shard N+1) |

Jump hash is ideal for **sharded databases** where you only add new shards at the end.

---

## 11. Rendezvous Hashing (Highest Random Weight)

Another alternative — for each key, compute a score for every server. Assign to the **highest-scoring server**.

```csharp
/// O(N) per lookup but perfectly uniform and simple.
public static string RendezvousHash(string key, IEnumerable<string> servers)
{
    return servers.MaxBy(s => Hash($"{key}:{s}"))!;
}
```

### When to Use

- Small number of servers (< 50) where O(N) per lookup is acceptable
- When you need **perfect uniformity** without vnodes
- Used by: Microsoft Azure, Twitter's caching layer

---

## 12. Real-World Architectures

### Amazon DynamoDB (Dynamo Paper, 2007)

```
Client Request
     │
     ▼
Coordinator Node (determined by consistent hash of partition key)
     │
     ├──→ Replica 1 (next node clockwise)
     ├──→ Replica 2 (next next node clockwise)
     └──→ Replica 3
     
Quorum: W + R > N  (e.g., W=2, R=2, N=3)
```

- Uses consistent hashing with virtual nodes
- Each node owns multiple token ranges
- Preference list = first N distinct physical nodes clockwise
- Sloppy quorum + hinted handoff for availability

### Apache Cassandra

```
Token Ring: 0 ──────────────────── 2^63
            │                        │
         Node A                   Node D
        (token 0)              (token 3×2^61)
            │                        │
         Node B                   Node C
        (token 2^61)           (token 2×2^61)

Partition Key → Murmur3 hash → token → owning node
```

- Configurable vnodes (default: 256 per node)
- `num_tokens` in cassandra.yaml controls vnodes
- Replication strategy: SimpleStrategy or NetworkTopologyStrategy

### Memcached / Redis Cluster

```
Client Library (ketama algorithm)
     │
     ├── hash("key1") → Server A
     ├── hash("key2") → Server B
     └── hash("key3") → Server C

No central coordinator — client-side consistent hashing
```

- **Ketama** — the original consistent hashing library for memcached
- Redis Cluster uses **hash slots** (16384 fixed slots) — a variant of consistent hashing

### Content Delivery Networks (CDNs)

```
User Request → DNS → Edge PoP
                       │
                 Consistent Hash(URL)
                       │
              ┌────────┼────────┐
           Cache A   Cache B   Cache C
           
Same URL always routes to same cache server → maximizes cache hit rate
```

- Akamai (invented consistent hashing in 1997)
- CloudFront, Fastly, Cloudflare all use variants

---

## 13. Hash Function Selection

| Hash Function | Speed | Distribution | Use Case |
|---------------|-------|-------------|----------|
| MD5 | Moderate | Excellent | General purpose (not crypto) |
| MurmurHash3 | Fast | Excellent | Cassandra, Redis |
| xxHash | Very Fast | Excellent | High-throughput systems |
| SHA-256 | Slow | Excellent | When crypto properties needed |
| FNV-1a | Fast | Good | Simple implementations |

**Rule of thumb:** Use MurmurHash3 or xxHash for production. MD5 is fine for prototyping.

---

## 14. Common Pitfalls

### 1. Too Few Virtual Nodes

```
Problem:  10 servers × 1 vnode = 10 points on ring → very uneven
Solution: 10 servers × 150 vnodes = 1500 points → nearly uniform
```

### 2. Hot Keys (Celebrity Problem)

```
Problem:  "taylor_swift" key gets 1M req/sec → single server overloaded
Solutions:
  - Bounded-load consistent hashing (Section 9)
  - Key-level replication: replicate hot keys to multiple servers
  - Local caching: cache hot keys in application memory
  - Key splitting: "taylor_swift:shard_0", "taylor_swift:shard_1", ...
```

### 3. Cascading Failures on Node Removal

```
Problem:  S2 dies → S3 absorbs S2's load → S3 overloaded → S3 dies → cascade
Solutions:
  - Replication ensures load is spread across multiple successors
  - Circuit breakers + load shedding
  - Bounded-load hashing caps per-node load
```

### 4. Ring Metadata Synchronization

```
Problem:  Different clients have different views of the ring → inconsistent routing
Solutions:
  - Gossip protocol (Cassandra, DynamoDB)
  - Centralized config (ZooKeeper, etcd, Consul)
  - Versioned ring with crdt-based merging
```

---

## 15. System Design Interview Tips

1. **Start with the problem** — explain why `hash % N` fails at scale (cache avalanche)
2. **Draw the ring** — visual explanation is expected; show keys walking clockwise
3. **Introduce virtual nodes** — explain the uniformity problem and how vnodes solve it
4. **Discuss replication** — walk clockwise to next R distinct physical nodes
5. **Mention real systems** — DynamoDB, Cassandra, Memcached (ketama), CDNs
6. **Address hot keys** — bounded-load hashing or key splitting
7. **Distributed coordination** — gossip protocol vs centralized config store

### Quick Reference Formula

```
Keys remapped on add/remove = K / N
  K = total keys
  N = number of servers

With vnodes:
  Ring positions = N × V  (V = vnodes per server)
  Lookup time = O(log(N × V))
  Memory = O(N × V)
```

---

## 16. Comparison of Distributed Hashing Approaches

| Approach | Remap on Change | Memory | Lookup | Supports Remove? | Used By |
|----------|----------------|--------|--------|-------------------|---------|
| Modular (hash % N) | ~100% | O(1) | O(1) | N/A | Simple apps |
| Consistent Hashing | K/N | O(N×V) | O(log(N×V)) | ✅ Any node | DynamoDB, Cassandra |
| Jump Hash | K/N | O(1) | O(log N) | ❌ Last only | Google (internal) |
| Rendezvous Hash | K/N | O(N) | O(N) | ✅ Any node | Azure, Twitter |
| Hash Slots (Redis) | Slot-level | O(16384) | O(1) | ✅ Slot migration | Redis Cluster |
