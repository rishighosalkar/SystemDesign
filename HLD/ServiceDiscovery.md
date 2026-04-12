# Service Discovery

## Core Concept

In a distributed/microservices architecture, services need to find and communicate with each other. Unlike monoliths where everything is in-process, microservices run on dynamic IPs and ports — **service discovery** is the mechanism that lets services locate each other at runtime.

### The Problem

```
Without Service Discovery:
  OrderService → http://192.168.1.10:8080/payments   ← hardcoded IP

  What happens when:
    - PaymentService scales to 5 instances?
    - PaymentService moves to a new host?
    - PaymentService instance crashes?

  Answer: Everything breaks.
```

---

## Two Patterns of Service Discovery

### 1. Client-Side Discovery

The client is responsible for determining the network locations of available service instances and load balancing requests across them.

```
┌──────────────┐       ┌──────────────────┐
│ Order Service│──1───→│ Service Registry  │
│  (Client)    │←─2────│ (Eureka/Consul)  │
│              │       └──────────────────┘
│              │         Returns:
│              │         PaymentService:
│              │           - 10.0.1.5:8080
│              │           - 10.0.1.6:8080
│              │           - 10.0.1.7:8080
│              │
│              │──3───→  10.0.1.6:8080  (client picks one)
└──────────────┘         ▲
                         │ client-side load balancing
```

**Flow:**
1. Client queries the service registry
2. Registry returns all healthy instances
3. Client applies load balancing (round-robin, random, weighted) and calls directly

**Examples:** Netflix Eureka + Ribbon, Consul + custom client

**Pros:**
- ✅ No extra network hop (direct call)
- ✅ Client can apply smart routing (latency-based, zone-aware)

**Cons:**
- ❌ Coupling — every client needs discovery logic
- ❌ Must implement per language/framework

---

### 2. Server-Side Discovery

The client makes a request to a load balancer/router, which queries the registry and forwards the request.

```
┌──────────────┐      ┌───────────────┐      ┌──────────────────┐
│ Order Service│──1──→│ Load Balancer  │──2──→│ Service Registry  │
│  (Client)    │      │ / API Gateway  │←─3───│ (Consul/etcd)    │
│              │      │                │       └──────────────────┘
│              │      │                │──4──→  10.0.1.6:8080
│              │←─5───│                │←──────  (response)
└──────────────┘      └───────────────┘
```

**Flow:**
1. Client sends request to a known endpoint (load balancer / API gateway)
2. LB queries registry for healthy instances
3. Registry returns instance list
4. LB forwards request to a chosen instance
5. Response flows back through LB

**Examples:** AWS ALB + ECS, Kubernetes Service + kube-proxy, NGINX + Consul

**Pros:**
- ✅ Client is simple — just knows one endpoint
- ✅ Language-agnostic — discovery logic centralized

**Cons:**
- ❌ Extra network hop (higher latency)
- ❌ Load balancer is a potential bottleneck / SPOF

---

## Service Registry — The Heart of Discovery

The registry is a database of available service instances and their network locations.

### How It Works

```
┌─────────────────────────────────────────────────┐
│              SERVICE REGISTRY                    │
├─────────────────┬───────────┬───────────────────┤
│ Service Name    │ Instance  │ Status             │
├─────────────────┼───────────┼───────────────────┤
│ payment-service │ 10.0.1.5  │ HEALTHY            │
│ payment-service │ 10.0.1.6  │ HEALTHY            │
│ payment-service │ 10.0.1.7  │ UNHEALTHY (remove) │
│ order-service   │ 10.0.2.1  │ HEALTHY            │
│ user-service    │ 10.0.3.1  │ HEALTHY            │
│ user-service    │ 10.0.3.2  │ HEALTHY            │
└─────────────────┴───────────┴───────────────────┘
```

### Registration Patterns

#### A. Self-Registration

The service instance registers itself with the registry on startup and deregisters on shutdown.

```
PaymentService (on startup):
  POST /registry/register
  {
    "name": "payment-service",
    "host": "10.0.1.5",
    "port": 8080,
    "healthCheck": "/actuator/health"
  }

PaymentService (every 30s):
  PUT /registry/heartbeat/payment-service/10.0.1.5

PaymentService (on shutdown):
  DELETE /registry/deregister/payment-service/10.0.1.5
```

- ✅ Simple, no extra component
- ❌ Couples service to registry, must implement per language

#### B. Third-Party Registration

A separate **registrar** process watches for new instances and registers them.

```
┌──────────────┐     detects new container
│  Registrar   │←────────────────────────── Docker / K8s
│  (sidecar /  │
│   platform)  │────→ Registry: "new payment-service at 10.0.1.8"
└──────────────┘
```

- ✅ Services are decoupled from registry
- ✅ Works with any language/framework
- ❌ Extra component to manage

**Examples:** Kubernetes (kubelet + etcd), AWS ECS (built-in), Netflix Prana (sidecar)

---

## Health Checking

A registry is only useful if it reflects reality. Health checks ensure dead instances are removed.

```
Health Check Strategies:
─────────────────────────────────────────────────────────
1. Heartbeat (TTL-based)
   Service sends periodic heartbeat → miss 3 in a row → marked unhealthy

2. Active Polling
   Registry pings service's health endpoint periodically
   GET /health → 200 OK = healthy, timeout/5xx = unhealthy

3. Passive / Traffic-based
   Load balancer monitors real traffic error rates
   >50% errors in 30s window → circuit break → mark unhealthy
─────────────────────────────────────────────────────────
```

---

## Popular Implementations

### 1. Netflix Eureka (Client-Side Discovery)

```
Architecture:
┌────────────┐  register  ┌──────────────┐  replicate  ┌──────────────┐
│ Service A  │───────────→│  Eureka       │←───────────→│  Eureka       │
│ (Eureka    │  heartbeat │  Server 1     │             │  Server 2     │
│  Client)   │←──fetch────│  (us-east-1a) │             │  (us-east-1b) │
└────────────┘  registry  └──────────────┘             └──────────────┘

Key Features:
  - AP system (availability over consistency — CAP theorem)
  - Peer-to-peer replication between Eureka servers
  - Client caches registry locally (works even if Eureka is down)
  - Self-preservation mode: stops evicting instances during network partitions
```

### 2. HashiCorp Consul (Client or Server-Side)

```
Architecture:
┌────────────┐              ┌──────────────────────┐
│ Service A  │──register───→│  Consul Agent        │
│            │              │  (local sidecar)     │
│            │              │       │               │
└────────────┘              │       │ gossip        │
                            │       ▼               │
                            │  Consul Server       │
                            │  Cluster (Raft)      │
                            └──────────────────────┘

Key Features:
  - CP system (consistency over availability — uses Raft consensus)
  - Built-in health checking (HTTP, TCP, script, gRPC)
  - Multi-datacenter support out of the box
  - Key-Value store for configuration
  - DNS interface: payment-service.service.consul → 10.0.1.5
  - Service mesh with mTLS (Consul Connect)
```

### 3. Kubernetes (Server-Side Discovery — Platform-Native)

```
Architecture:
┌──────────────────────────────────────────────────────┐
│                  Kubernetes Cluster                    │
│                                                       │
│  ┌──────────┐    ┌─────────────────┐                 │
│  │  Pod A   │───→│  Service        │  ClusterIP:     │
│  │ (client) │    │  "payment-svc"  │  10.96.0.15     │
│  └──────────┘    │                 │                  │
│                  │  Selector:      │                  │
│                  │  app=payment    │                  │
│                  └────────┬────────┘                  │
│                      ┌────┴────┐                      │
│                      │kube-proxy│ (iptables/IPVS)     │
│                      └────┬────┘                      │
│                  ┌────────┼────────┐                  │
│                  ▼        ▼        ▼                  │
│              ┌──────┐ ┌──────┐ ┌──────┐              │
│              │Pod 1 │ │Pod 2 │ │Pod 3 │              │
│              │:8080 │ │:8080 │ │:8080 │              │
│              └──────┘ └──────┘ └──────┘              │
│                                                       │
│  DNS: payment-svc.namespace.svc.cluster.local         │
│       → 10.96.0.15 → load balanced to Pod 1/2/3      │
│                                                       │
│  Registry: etcd (stores all service/endpoint state)   │
└──────────────────────────────────────────────────────┘

Flow:
  1. Pods with label app=payment are auto-registered as endpoints
  2. K8s Service object creates a stable virtual IP (ClusterIP)
  3. CoreDNS resolves service name → ClusterIP
  4. kube-proxy routes traffic to healthy pods via iptables/IPVS
  5. No application-level discovery code needed
```

### 4. AWS Cloud Map (Managed Service Discovery)

```
┌────────────┐   API call    ┌──────────────┐
│ ECS Task / │──────────────→│  AWS Cloud   │
│ Lambda     │  discover     │  Map         │
│            │←──────────────│              │
└────────────┘  instances    │  Namespace:  │
                             │  myapp.local │
                             │              │
                             │  Services:   │
                             │  - payment   │
                             │  - order     │
                             │  - user      │
                             └──────┬───────┘
                                    │
                              Route 53 DNS
                              (auto-managed)

  DNS: payment.myapp.local → healthy ECS task IPs
  API: DiscoverInstances("payment") → [{ip, port, attributes}]
```

---

## DNS-Based vs Registry-Based Discovery

```
┌──────────────────────┬──────────────────────────────────┐
│   DNS-Based          │   Registry-Based                 │
├──────────────────────┼──────────────────────────────────┤
│ Consul DNS           │ Eureka                           │
│ Kubernetes CoreDNS   │ Zookeeper                        │
│ AWS Cloud Map (DNS)  │ etcd (direct API)                │
│ Route 53             │ Consul (HTTP API)                │
├──────────────────────┼──────────────────────────────────┤
│ ✅ Universal (every  │ ✅ Rich metadata (version, zone, │
│    language has DNS)  │    weight, custom attributes)    │
│ ✅ No client library │ ✅ Real-time updates (watch/push)│
│ ❌ TTL caching stale │ ✅ Fine-grained health checks    │
│ ❌ No metadata       │ ❌ Requires client library       │
│ ❌ Limited LB control│ ❌ More complex setup            │
└──────────────────────┴──────────────────────────────────┘
```

---

## Service Mesh — The Evolution of Discovery

Modern systems push discovery into the **infrastructure layer** via sidecar proxies.

```
┌─────────────────────────────────────────────────────┐
│                    Service Mesh                      │
│                                                      │
│  ┌──────────┬──────────┐   ┌──────────┬──────────┐  │
│  │ Service A│  Envoy   │──→│  Envoy   │ Service B│  │
│  │          │  Proxy   │   │  Proxy   │          │  │
│  └──────────┴──────────┘   └──────────┴──────────┘  │
│                    ▲               ▲                  │
│                    └───────┬───────┘                  │
│                     ┌──────▼──────┐                   │
│                     │ Control Plane│                  │
│                     │ (Istio/Linkerd)                 │
│                     │ - Service registry              │
│                     │ - Load balancing rules           │
│                     │ - mTLS certificates              │
│                     │ - Traffic policies               │
│                     └─────────────┘                   │
│                                                      │
│  App code: just call http://service-b/api            │
│  Sidecar handles: discovery, LB, retries, TLS, etc. │
└─────────────────────────────────────────────────────┘

Examples: Istio (Envoy), Linkerd, AWS App Mesh
```

- ✅ Zero discovery code in application
- ✅ Language-agnostic
- ✅ Observability, security, traffic control built-in
- ❌ Operational complexity
- ❌ Latency overhead from sidecar proxy

---

## Comparison Matrix

| Feature | Eureka | Consul | Kubernetes | Zookeeper | AWS Cloud Map |
|---|---|---|---|---|---|
| CAP | AP | CP | CP | CP | Managed |
| Health Check | Heartbeat | HTTP/TCP/gRPC/Script | Liveness/Readiness probes | Session + ephemeral nodes | HTTP/TCP |
| DNS Support | No | Yes | Yes (CoreDNS) | No | Yes (Route 53) |
| Multi-DC | Limited | Native | Federation | Manual | Multi-region |
| KV Store | No | Yes | etcd (internal) | Yes | Attributes |
| Service Mesh | No | Consul Connect | Istio/Linkerd | No | AWS App Mesh |
| Best For | Spring Cloud | Multi-platform | Container workloads | Legacy/Hadoop | AWS-native |

---

## When to Use What

- **Kubernetes native** → Use K8s Services + CoreDNS (simplest, no extra infra)
- **AWS ECS/Lambda** → Use AWS Cloud Map
- **Spring Boot microservices** → Eureka (tight Spring integration)
- **Multi-platform / multi-DC** → Consul (most versatile)
- **Need service mesh** → Istio (K8s) / Consul Connect / AWS App Mesh
- **Legacy systems** → Zookeeper (if already in stack, otherwise migrate)

> **Industry trend:** Platform-native discovery (K8s, Cloud Map) is replacing standalone registries. Service meshes are absorbing discovery into the infrastructure layer, making it invisible to application code.
