# Networking Protocols - SSE/SDE-2 Interview Guide

## Overview
Understanding networking protocols is fundamental for system design interviews. The OSI model has 7 layers, but we focus on:
- **Layer 3 (Network)**: IP
- **Layer 4 (Transport)**: TCP, UDP
- **Layer 7 (Application)**: HTTP, HTTPS, DNS

---

## 1. IP (Internet Protocol)

### Purpose
- Identifies machines on a network using unique IP addresses
- Routes packets across different networks
- Provides the foundation for packet delivery

### Key Characteristics
- **Connectionless**: No setup required before sending data
- **Unreliable**: No guarantee of delivery or ordering
- **Packet Structure**:
  - Source IP: Sender's address
  - Destination IP: Recipient's address
  - Payload: Data being transmitted
  - TTL (Time-to-Live): Prevents infinite loops
  - Version: IPv4 (32-bit) or IPv6 (128-bit)

### Interview Insight
- IP is a Layer 3 protocol responsible for logical addressing and routing
- It works independent of the transport layer protocol (TCP/UDP)
- CIDR notation (10.0.0.0/24) is used for IP address ranges

---

## 2. TCP (Transmission Control Protocol)

### Purpose
- Transport layer protocol for reliable, ordered data delivery
- Ensures data integrity and delivery guarantees

### Key Characteristics

#### Reliability
- **Acknowledgment mechanism**: Receiver confirms receipt of packets
- **Retransmission**: Unacknowledged packets are resent
- **Sequence numbers**: Packets are ordered correctly at the destination
- **Error checking**: Checksums detect corrupted data

#### Connection-Oriented
- **3-Way Handshake** (Establishment):
  1. **SYN**: Client sends synchronize packet with sequence number (seq=x)
  2. **SYN-ACK**: Server responds with its sequence number (seq=y) and acknowledges client (ack=x+1)
  3. **ACK**: Client sends acknowledgment (ack=y+1), connection established

- **4-Way Termination** (Graceful Close):
  1. **FIN**: One side sends finish packet
  2. **ACK**: Other side acknowledges
  3. **FIN**: Other side sends finish
  4. **ACK**: First side acknowledges

#### Flow Control & Congestion Control
- **Sliding window**: Regulates data transmission rate
- **Congestion avoidance**: Reduces transmission speed when network is congested
- **Slow start**: Gradually increases transmission rate

### Use Cases
- Web services (HTTP)
- Email (SMTP, POP3, IMAP)
- File transfer (FTP, SFTP)
- Remote access (SSH, Telnet)
- Database connections

### Interview Insight
- TCP guarantees delivery but has higher overhead due to acknowledgments
- Connection setup adds latency (3-way handshake)
- Best for applications requiring reliability over speed

---

## 3. UDP (User Datagram Protocol)

### Purpose
- Transport layer protocol for fast, connectionless data delivery
- Minimal overhead with no delivery guarantees

### Key Characteristics

#### Connectionless
- No handshake required
- Immediate packet transmission
- Lower latency than TCP

#### Unreliable
- No acknowledgments
- No retransmission mechanism
- Packets may be lost, duplicated, or out of order
- No error recovery

#### Minimal Overhead
- Smaller header size (8 bytes vs TCP's 20 bytes)
- Reduced processing at each hop

### Use Cases
- **Real-time applications**: Live video streaming, VoIP
- **Gaming**: Multiplayer online games (low latency prioritized)
- **DNS queries**: Quick lookups without connection overhead
- **IoT**: Sensor data where occasional packet loss is acceptable
- **Online multiplayer**: Discord, Twitch, Zoom use UDP

### TCP vs UDP Comparison

| Aspect | TCP | UDP |
|--------|-----|-----|
| **Reliability** | Guaranteed delivery | Best effort |
| **Ordering** | In-order delivery | No ordering guarantee |
| **Speed** | Slower (overhead) | Faster (minimal overhead) |
| **Connection** | Connection-oriented | Connectionless |
| **Handshake** | 3-way required | None |
| **Use Case** | Critical data | Real-time, loss-tolerant |

### Interview Insight
- UDP is preferred when latency matters more than reliability
- Trade-off: speed vs. guaranteed delivery
- Application layer can implement its own reliability on top of UDP (e.g., QUIC protocol)

---

## 4. DNS (Domain Name System)

### Purpose
- Translates human-readable domain names to IP addresses
- Enables users to access services without memorizing IP addresses

### Architecture

#### Query Process (Recursive Resolution)
1. **Local resolver**: Your device queries the local DNS resolver
2. **Root nameserver**: Resolver queries root nameserver (knows TLD servers). The root server directs the query to the correct TLD server based on the domain's extension (like .com).
3. **TLD nameserver**: They manage the information for all domains that share a common extension (e.g., all .com or .net websites). TLD server points the query to the specific Authoritative Name Server for that exact domain
4. **Authoritative nameserver**: Returns the IP address for the domain
5. **Response**: IP address returned to your device

#### Caching Layers
- **Browser cache**: Stores recent lookups
- **OS cache**: System-level DNS cache
- **ISP resolver cache**: ISP maintains cache for common domains
- **TTL (Time-to-Live)**: Determines cache validity period

### DNS Records
- **A Record**: Domain → IPv4 address
- **AAAA Record**: Domain → IPv6 address
- **CNAME**: Alias pointing to another domain
- **MX Record**: Mail exchange server
- **NS Record**: Nameserver for the domain
- **TXT Record**: Text records (SPF, DKIM verification)

### Optimization Techniques
- **DNS Caching**: Reduce repeated lookups
- **Geolocation-based routing**: Route users to nearest server
- **Health checks**: Failover to healthy servers
- **Load balancing**: Distribute traffic across servers

### Interview Insight
- DNS can be a bottleneck; latency adds up (typically 50-300ms per lookup)
- Connection pooling and caching are crucial for performance
- DNS is typically UDP-based (port 53) for speed; TCP for large responses

---

## 5. HTTP (HyperText Transfer Protocol)

### Purpose
- Application layer protocol defining request/response communication between clients and servers
- Foundation for web services

### Key Characteristics

#### Stateless
- Server doesn't maintain session state between requests
- Each request is independent
- **Implication**: Clients must send all necessary information with each request
- **Solution**: Cookies, sessions, tokens maintain state at application level

#### Plain Text
- Data transmitted in human-readable format
- **Security Risk**: Anyone intercepting network traffic can read the data
- **Solution**: Use HTTPS to encrypt the data

#### Request-Response Model
- **Request**: Client sends HTTP request with method, headers, body
- **Response**: Server sends status code, headers, body

### HTTP Methods
- **GET**: Retrieve data (idempotent, cacheable)
- **POST**: Submit data (non-idempotent)
- **PUT**: Replace resource (idempotent)
- **PATCH**: Partial update (non-idempotent)
- **DELETE**: Remove resource (idempotent)
- **HEAD**: Like GET but no response body
- **OPTIONS**: Describe communication options

### HTTP Status Codes
- **1xx**: Informational
- **2xx**: Success (200 OK, 201 Created, 204 No Content)
- **3xx**: Redirection (301 Moved Permanently, 302 Found, 304 Not Modified)
- **4xx**: Client error (400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found)
- **5xx**: Server error (500 Internal Server Error, 502 Bad Gateway, 503 Service Unavailable)

### HTTP Headers
- **Request**: Host, User-Agent, Accept, Authorization, Content-Type
- **Response**: Content-Type, Content-Length, Cache-Control, Set-Cookie, ETag
- **Cache Control**: max-age, public, private, no-cache, no-store

### Versions
- **HTTP/1.0**: One request-response per connection
- **HTTP/1.1**: Persistent connections (keep-alive), pipelining, chunked encoding
- **HTTP/2**: Multiplexing, server push, header compression (binary protocol)
- **HTTP/3**: Uses QUIC protocol (UDP-based), faster connection establishment

### Interview Insight
- HTTP is stateless, so design sessions carefully
- Connection pooling reduces latency from repeated TCP handshakes
- Understand idempotency for designing safe retry mechanisms

---

## 6. HTTPS (HTTP Secure)

### Purpose
- Secure version of HTTP using encryption
- Protects data from interception and tampering

### TLS/SSL Encryption

#### Evolution
- **SSL (Secure Sockets Layer)**: Deprecated, no longer used
- **TLS 1.0, 1.1**: Outdated, vulnerable to attacks
- **TLS 1.2**: Industry standard (still widely used)
- **TLS 1.3**: Latest, faster, more secure

#### Symmetric vs Asymmetric Encryption
- **Symmetric**: Same key for encryption and decryption (fast, secure if key is protected)
- **Asymmetric**: Public-private key pair (slower but enables key exchange)
- **HTTPS approach**: Asymmetric for key exchange, then symmetric for data encryption

#### TLS Handshake (TLS 1.2)
1. **ClientHello**: Client sends supported cipher suites, TLS version, random number
2. **ServerHello**: Server selects cipher suite, sends certificate, random number
3. **Certificate Exchange**: Server sends X.509 certificate with public key
4. **Key Exchange**: Client generates pre-master secret, encrypts with public key, sends to server
5. **Both compute Master Secret**: Using pre-master secret and random numbers
6. **Cipher Suite Established**: Symmetric encryption keys derived from master secret
7. **Finished**: Both sides send encrypted verification message
8. **Encrypted communication**: All subsequent data encrypted with symmetric key

#### TLS 1.3 Improvements
- **Faster handshake**: 1 round-trip (vs 2 for TLS 1.2)
- **Forward secrecy**: Ephemeral keys ensure past sessions can't be decrypted
- **Removed weak ciphers**: Enforces strong algorithms

### Certificate Authority (CA)
- Issues digital certificates proving server identity
- Browser trusts CAs and validates certificates
- **Certificate chain**: Server cert → Intermediate CA → Root CA

### Interview Insight
- HTTPS adds latency (TLS handshake) but is mandatory for sensitive data
- Certificate pinning provides additional security
- HTTP/2 and HTTP/3 require HTTPS
- Perfect forward secrecy: compromise of long-term key doesn't expose past sessions

---

## 7. Complete End-to-End Flow: Accessing https://amazon.com

### Step 1: DNS Resolution
```
User types: https://amazon.com
Browser queries local DNS resolver
amazon.com → 176.32.98.166 (example IP)
```
**Time**: ~50-300ms depending on caching

### Step 2: TCP 3-Way Handshake (Port 443 for HTTPS)
```
Client → Server: SYN (seq=x)
Server → Client: SYN-ACK (seq=y, ack=x+1)
Client → Server: ACK (ack=y+1)
```
**Time**: ~50-150ms depending on network latency
**Result**: TCP connection established

### Step 3: TLS Handshake (HTTPS Encryption Setup)
```
1. Client → Server: ClientHello (supported ciphers, TLS version)
2. Server → Client: ServerHello + Certificate
3. Client generates pre-master secret, encrypts with public key
4. Client → Server: Encrypted pre-master secret
5. Both sides compute master secret
6. Client → Server: Encrypted "Finished" message
7. Server → Client: Encrypted "Finished" message
8. Secure channel established
```
**Time**: ~100-200ms
**Result**: Symmetric encryption keys established

### Step 4: HTTP Request
```
GET /products HTTP/1.1
Host: amazon.com
Accept: text/html
User-Agent: Mozilla/5.0
[Other headers...]
```
**Encrypted**: All data is encrypted by TLS
**Sent over**: Established TCP connection

### Step 5: Server Processing
- Request routed through load balancers
- Application server processes request
- Database queries executed
- Response generated

### Step 6: HTTP Response
```
HTTP/1.1 200 OK
Content-Type: text/html; charset=utf-8
Content-Length: 45678
Cache-Control: max-age=3600
Set-Cookie: session_id=abc123
[Response body: HTML content]
```
**Encrypted**: Response encrypted by TLS before sending

### Timeline Summary
- DNS lookup: 50-300ms
- TCP handshake: 50-150ms
- TLS handshake: 100-200ms
- HTTP request transmission: 1-10ms
- Server processing: 50-500ms
- **Total**: 250ms-1200ms (excluding server processing)

### Optimization Opportunities
- **DNS prefetch**: Reduce DNS lookup latency
- **Keep-alive connections**: Reuse TCP connections for multiple requests
- **HTTP/2 multiplexing**: Multiple requests over single connection
- **TLS session resumption**: Skip full handshake on reconnection
- **CDN**: Serve content from geographically closer servers
- **Caching**: Reduce server hits for static content

---

## Interview Key Takeaways

### Design Principles
1. **Choose protocol based on requirements**: TCP for reliability, UDP for speed
2. **Understand trade-offs**: Latency vs. reliability, security vs. performance
3. **Optimize for common scenarios**: DNS caching, connection pooling, HTTP keep-alive
4. **Monitor performance**: Track DNS resolution, connection establishment, request latency

### Common Interview Questions
- **TCP vs UDP**: When to use each, trade-offs
- **3-way handshake**: Why needed, sequence of events
- **Stateless HTTP**: How to maintain state, session management
- **HTTPS security**: Certificate validation, encryption mechanism
- **DNS resolution**: Recursive vs iterative, caching strategy
- **Connection pooling**: Why beneficial, connection reuse
- **Load balancing**: DNS-based, connection-based approaches

### Performance Considerations
- Every extra hop adds latency (DNS, TLS handshake)
- Connection reuse is critical for performance
- Caching at multiple levels reduces redundant work
- Protocol choice impacts overall system latency
- Security (HTTPS/TLS) has performance cost, worth it for sensitive data
