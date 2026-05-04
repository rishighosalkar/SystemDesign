# AWS Solutions Architect Associate — Interview Questions & Answers

---

## 1. Explain EC2 instances in-depth — instance families, use cases, and pricing models (on-demand, reserved, spot).

**Instance Families:**

| Family | Optimized For | Examples | Use Cases |
|--------|--------------|----------|-----------|
| General Purpose (M, T) | Balanced compute, memory, networking | M7i, T3, T3a | Web servers, code repos, small/mid DBs |
| Compute Optimized (C) | High-performance processors | C7g, C6i | Batch processing, ML inference, gaming servers, HPC |
| Memory Optimized (R, X, z) | Large in-memory datasets | R7g, X2idn, z1d | In-memory caches (Redis/Memcached), real-time big data analytics, SAP HANA |
| Storage Optimized (I, D, H) | High sequential read/write to local storage | I4i, D3, H1 | Data warehousing, distributed file systems (HDFS), high-frequency OLTP |
| Accelerated Computing (P, G, Inf, Trn) | Hardware accelerators (GPU/custom chips) | P5, G5, Inf2, Trn1 | ML training, graphics rendering, video transcoding |
| HPC Optimized (Hpc) | High performance computing | Hpc7g | Tightly coupled HPC workloads |

**Naming Convention:** e.g., `m5.xlarge` → m = family, 5 = generation, xlarge = size.

**Pricing Models:**

- **On-Demand:** Pay per second (Linux) or per hour (Windows). No commitment. Best for unpredictable, short-term workloads. Most expensive per unit.
- **Reserved Instances (RI):** 1 or 3-year commitment. Up to 72% discount vs on-demand. Options: Standard RI (can't change instance family) vs Convertible RI (can change family/OS/tenancy, up to 66% discount). Payment: All Upfront > Partial Upfront > No Upfront.
- **Savings Plans:** Commit to a $/hr spend for 1 or 3 years. Compute Savings Plans (flexible across family, region, OS, tenancy) or EC2 Instance Savings Plans (locked to family + region). Up to 72% discount.
- **Spot Instances:** Up to 90% discount. AWS can reclaim with 2-minute notice. Best for fault-tolerant, stateless workloads (batch jobs, CI/CD, data analysis). Use Spot Fleet to diversify across instance types/AZs.
- **Dedicated Hosts:** Physical server dedicated to you. Needed for server-bound software licenses (BYOL). Most expensive option.
- **Dedicated Instances:** Run on hardware dedicated to your account but you don't control placement. Less expensive than Dedicated Hosts.
- **Capacity Reservations:** Reserve capacity in a specific AZ. No billing discount — combine with RIs or Savings Plans for discount + guaranteed capacity.

---

## 2. What are the EC2 storage options? (EBS, instance store, EFS, FSx)

- **EBS (Elastic Block Store):** Network-attached block storage. Persists independently of instance lifecycle. Bound to a single AZ. Supports snapshots (stored in S3). Types:
  - gp3/gp2 — General purpose SSD. gp3: 3,000 baseline IOPS, up to 16,000. gp2: burstable, 3 IOPS/GB.
  - io2/io1 — Provisioned IOPS SSD. Up to 64,000 IOPS (io2 Block Express: 256,000). For databases needing sustained IOPS.
  - st1 — Throughput-optimized HDD. Up to 500 MB/s. For big data, data warehouses, log processing.
  - sc1 — Cold HDD. Lowest cost. For infrequently accessed data.

- **Instance Store:** Physically attached NVMe/SSD storage on the host. Highest I/O performance (millions of IOPS). Ephemeral — data lost on stop/terminate/hardware failure. Best for caches, buffers, scratch data, temp content.

- **EFS (Elastic File System):** Managed NFS (NFSv4.1). Multi-AZ, shared across hundreds of EC2 instances concurrently. Auto-scales. Linux only. Storage classes: Standard, Infrequent Access (IA), One Zone, One Zone-IA. Use for content management, web serving, shared home directories.

- **FSx:** Managed third-party high-performance file systems.
  - FSx for Lustre — HPC, ML training, video processing. Integrates with S3.
  - FSx for Windows File Server — SMB protocol, Active Directory integration, Windows workloads.
  - FSx for NetApp ONTAP — Multi-protocol (NFS, SMB, iSCSI), data deduplication, snapshots.
  - FSx for OpenZFS — NFS, snapshots, compression. Linux workloads migrating from ZFS.

---

## 3. EBS encryption and backup options.

**Encryption:**
- EBS encryption uses AWS KMS (AES-256). Encrypts data at rest, in transit (between instance and volume), all snapshots, and volumes created from snapshots.
- Encryption is handled transparently — no impact on latency. Minimal performance overhead.
- You can use the default AWS-managed key (`aws/ebs`) or a customer-managed CMK.
- Encryption can be enabled by default at the account level per region (Account Settings → EBS encryption).
- Encrypted volumes can only be attached to supported instance types (all current-gen instances support it).

**Backup Options:**
- **EBS Snapshots:** Point-in-time, incremental backups stored in S3 (managed by AWS). First snapshot is full; subsequent ones are incremental. Can copy snapshots cross-region and cross-account. Can create volumes from snapshots in any AZ within the region.
  - Snapshot Archive: Move rarely-accessed snapshots to archive tier (75% cheaper). 24–72 hour retrieval time.
  - Recycle Bin: Protect against accidental deletion. Set retention rules (1 day to 1 year).
  - Fast Snapshot Restore (FSR): Eliminates latency on first access. Costs extra — enable per AZ.
- **AWS Backup:** Centralized, policy-driven backup service. Supports EBS, RDS, DynamoDB, EFS, FSx, etc. Cross-region and cross-account backup. Backup plans with schedules, retention, and lifecycle rules.
- **Data Lifecycle Manager (DLM):** Automate EBS snapshot creation, retention, and deletion with lifecycle policies. Tag-based targeting.

---

## 4. How do you create a new encrypted EBS from an unencrypted EBS and from an encrypted EBS?

**From an unencrypted EBS:**
1. Create a snapshot of the unencrypted volume.
2. Copy the snapshot and enable encryption during the copy (select a KMS key).
3. Create a new volume from the encrypted snapshot.
4. Attach the new encrypted volume to the instance.

> You cannot directly encrypt an existing unencrypted volume. You must go through the snapshot-copy-encrypt flow.

**From an encrypted EBS (re-encrypting with a different key):**
1. Create a snapshot of the encrypted volume (snapshot inherits encryption + key).
2. Copy the snapshot and select a different KMS key during the copy.
3. Create a new volume from the re-encrypted snapshot.

**From an encrypted EBS (same key):**
1. Create a snapshot (automatically encrypted with the same key).
2. Create a new volume from that snapshot — it will be encrypted with the same key.

> Shortcut: If account-level default encryption is enabled, any new volume or snapshot copy is automatically encrypted.

---

## 5. What is the difference between EBS, EFS, and instance store? When would you choose each?

| Feature | EBS | EFS | Instance Store |
|---------|-----|-----|----------------|
| Type | Block storage | File storage (NFS) | Block storage |
| Persistence | Persists after instance stop/terminate | Persists independently | Ephemeral — lost on stop/terminate |
| Scope | Single AZ, single instance (except io1/io2 multi-attach) | Multi-AZ, multi-instance | Tied to host hardware |
| Performance | Up to 256K IOPS (io2 Block Express) | Scales with # of instances | Millions of IOPS (NVMe) |
| Scaling | Manual resize | Auto-scales | Fixed at launch |
| OS Support | Linux + Windows | Linux only | Linux + Windows |
| Cost | Pay for provisioned size | Pay for what you use | Included in instance price |

**When to choose:**
- **EBS:** Boot volumes, databases, any workload needing persistent block storage for a single instance.
- **EFS:** Shared file storage across multiple instances — CMS, web serving, shared configs, container storage.
- **Instance Store:** Temporary high-performance storage — caches, buffers, scratch space, distributed replicated data (e.g., Hadoop HDFS, Cassandra).

---

## 6. What is EC2 placement groups? Explain cluster, spread, and partition strategies and their trade-offs.

Placement groups control how instances are placed on underlying hardware.

**Cluster Placement Group:**
- All instances in a single AZ, on the same rack (or close racks).
- Pros: Lowest latency, highest throughput (10 Gbps between instances). Ideal for HPC, tightly coupled workloads.
- Cons: Single point of failure (rack failure = all instances affected). Limited to one AZ.

**Spread Placement Group:**
- Each instance on a different physical rack. Max 7 instances per AZ per group.
- Pros: Maximum isolation. A rack failure affects only one instance. Best for critical applications needing high availability.
- Cons: Limited to 7 instances per AZ. Not suitable for large-scale deployments.

**Partition Placement Group:**
- Instances divided into partitions (logical groups), each on separate racks. Up to 7 partitions per AZ. Hundreds of instances per group.
- Pros: Large-scale distributed workloads with rack-level fault isolation. Partition metadata available to applications (e.g., Hadoop, Cassandra, Kafka can be topology-aware).
- Cons: Not as isolated as spread (instances within a partition share racks). More complex to manage.

**Trade-offs Summary:**
- Need low latency → Cluster
- Need high availability for small critical set → Spread
- Need fault isolation at scale for distributed systems → Partition

---

## 7. How does EC2 Auto Scaling work? Explain target tracking vs step scaling vs scheduled scaling.

**How Auto Scaling Works:**
- Auto Scaling Group (ASG) manages a fleet of EC2 instances. You define min, max, and desired capacity.
- Uses a Launch Template (or Launch Configuration — legacy) to define instance config (AMI, instance type, security groups, user data).
- ASG spans multiple AZs for high availability. It automatically rebalances instances across AZs.
- Health checks: EC2 status checks (default) and/or ELB health checks. Unhealthy instances are terminated and replaced.
- Cooldown period: Default 300s. Prevents rapid scale in/out oscillation.

**Scaling Policies:**

- **Target Tracking Scaling:**
  - Set a target metric value (e.g., average CPU = 50%). ASG automatically adjusts capacity to maintain the target.
  - Simplest to configure. AWS handles the math. Best for most use cases.
  - Example: "Keep average CPU at 50%" — ASG adds instances when CPU > 50%, removes when < 50%.
  - Predefined metrics: ASGAverageCPUUtilization, ASGAverageNetworkIn/Out, ALBRequestCountPerTarget.

- **Step Scaling:**
  - Define step adjustments based on CloudWatch alarm thresholds.
  - Example: CPU 50–70% → add 1 instance, CPU 70–90% → add 2, CPU > 90% → add 3.
  - More granular control than target tracking. Reacts proportionally to the alarm breach size.
  - No cooldown — uses "warm-up time" instead.

- **Scheduled Scaling:**
  - Scale based on a known schedule (cron-like).
  - Example: Scale to 10 instances every weekday at 8 AM, scale down to 2 at 8 PM.
  - Best for predictable traffic patterns.

- **Predictive Scaling (bonus):** Uses ML to forecast traffic and pre-provisions capacity ahead of demand. Combines with dynamic scaling.

---

## 8. What is an AMI? How would you share an AMI across accounts and regions?

**What is an AMI?**
- Amazon Machine Image — a template containing the OS, application server, applications, and launch permissions.
- Includes: root volume snapshot (EBS-backed) or template (instance-store-backed), launch permissions, block device mapping.
- Types: Public, Private (default — owner only), Shared (specific accounts).
- Region-scoped — an AMI exists in one region.

**Sharing across accounts:**
1. Modify the AMI's launch permissions: add the target account ID.
   - `aws ec2 modify-image-attribute --image-id ami-xxx --launch-permission "Add=[{UserId=123456789012}]"`
2. The target account can now launch instances from the shared AMI.
3. For encrypted AMIs: you must also share the KMS key used to encrypt the snapshot with the target account (via KMS key policy).
4. Best practice: The target account should copy the AMI to own it (so it's not dependent on the source account).

**Sharing across regions:**
1. Copy the AMI to the target region:
   - `aws ec2 copy-image --source-image-id ami-xxx --source-region us-east-1 --region eu-west-1 --name "My AMI Copy"`
2. This copies the underlying EBS snapshots to the target region.
3. The copied AMI gets a new AMI ID in the target region.
4. For encrypted AMIs: you can re-encrypt with a different KMS key in the target region during the copy.

---

## 9. How do you troubleshoot a high CPU or memory issue on an EC2 instance in production?

**Step 1 — Identify the problem:**
- CloudWatch metrics: CPUUtilization (built-in), MemoryUtilization (requires CloudWatch Agent).
- Set up CloudWatch Alarms for thresholds (e.g., CPU > 80% for 5 minutes).

**Step 2 — SSH into the instance and investigate:**
- `top` / `htop` — identify the process consuming CPU/memory.
- `ps aux --sort=-%cpu | head` — top CPU consumers.
- `ps aux --sort=-%mem | head` — top memory consumers.
- `vmstat`, `iostat`, `sar` — check for I/O wait, swap usage, system-level bottlenecks.
- `dmesg` — check for OOM (Out of Memory) killer events.
- `free -h` — check available memory and swap.

**Step 3 — Analyze application-level:**
- Check application logs for errors, memory leaks, runaway threads.
- Use profiling tools (e.g., Java: jstack, jmap; Python: py-spy; Node: clinic.js).
- Check for cron jobs or background processes that may have spiked.

**Step 4 — Remediate:**
- Short-term: Kill/restart the offending process. Vertically scale (resize instance).
- Long-term:
  - Fix application bugs (memory leaks, inefficient queries).
  - Implement Auto Scaling to handle load spikes.
  - Offload work: use caching (ElastiCache), queues (SQS), or move compute to Lambda.
  - Right-size the instance using AWS Compute Optimizer recommendations.
  - Enable detailed monitoring (1-minute intervals) for faster detection.

**Step 5 — Prevent recurrence:**
- CloudWatch Alarms + SNS notifications.
- Systems Manager Run Command for remote diagnostics without SSH.
- AWS X-Ray for distributed tracing if microservices are involved.

---

## 10. S3 archival modes — S3 Glacier Instant, Flexible, and Deep Archive. When to use each?

| Feature | Glacier Instant Retrieval | Glacier Flexible Retrieval | Glacier Deep Archive |
|---------|--------------------------|---------------------------|---------------------|
| Min storage duration | 90 days | 90 days | 180 days |
| Retrieval time | Milliseconds (same as S3 Standard) | Expedited: 1–5 min, Standard: 3–5 hrs, Bulk: 5–12 hrs | Standard: 12 hrs, Bulk: 48 hrs |
| Cost (storage) | Higher than Flexible | Lower than Instant | Lowest of all S3 classes |
| Cost (retrieval) | Per-GB retrieval fee (higher) | Per-GB + per-request fee | Per-GB + per-request fee |
| First byte latency | Milliseconds | Minutes to hours | Hours |

**When to use:**
- **Glacier Instant Retrieval:** Data accessed once per quarter but needs immediate access when requested. Examples: medical images, news media archives, user-generated content archives.
- **Glacier Flexible Retrieval:** Data accessed 1–2 times per year, can tolerate minutes-to-hours retrieval. Examples: backup data, disaster recovery, long-term analytics data.
- **Glacier Deep Archive:** Data rarely accessed (compliance/regulatory retention), can tolerate 12–48 hour retrieval. Cheapest storage in AWS. Examples: financial records (7-year retention), regulatory archives, tape replacement.

> Tip: Use S3 Lifecycle policies to automatically transition objects between storage classes based on age.

---

## 11. S3 policies and connectivity options — bucket policies, ACLs, VPC endpoints, Access Points.

**Bucket Policies:**
- JSON-based resource policies attached to the bucket. Control access at the bucket level.
- Can grant cross-account access, enforce encryption (deny PutObject without SSE), restrict by IP/VPC, require MFA delete.
- Evaluated with IAM policies — explicit deny always wins.

**ACLs (Access Control Lists):**
- Legacy mechanism. Grants basic read/write permissions to AWS accounts or predefined groups (e.g., public-read).
- AWS recommends disabling ACLs (S3 Object Ownership = Bucket owner enforced) and using bucket policies + IAM instead.
- Still needed for: S3 access logging (log delivery group needs ACL write permission).

**VPC Endpoints:**
- **Gateway Endpoint (S3 and DynamoDB only):** Free. Route table entry that directs S3 traffic through AWS private network instead of the internet. No NAT Gateway needed. Specified in route tables. Controlled via endpoint policies.
- **Interface Endpoint (PrivateLink):** ENI with private IP in your subnet. Costs per hour + per GB. Needed for on-premises access via VPN/Direct Connect, or cross-region access. DNS resolution required.

**S3 Access Points:**
- Named network endpoints with dedicated access policies. Simplify managing access for shared datasets.
- Each access point has its own DNS name and policy. Can restrict to a specific VPC.
- Example: One access point for "analytics-team" (read-only to `/analytics/*`), another for "data-engineering" (read-write to `/raw/*`).
- Multi-Region Access Points: Single global endpoint that routes to the nearest S3 bucket (uses S3 Replication). Accelerates multi-region architectures.

---

## 12. What is S3 Transfer Acceleration, and how does it differ from multi-part upload?

**S3 Transfer Acceleration:**
- Uses CloudFront edge locations to accelerate uploads to S3 over long distances.
- Client uploads to the nearest edge location → AWS backbone network → S3 bucket.
- Enabled per bucket. Uses a distinct endpoint: `bucketname.s3-accelerate.amazonaws.com`.
- Best for: Geographically distant uploads (e.g., users in Asia uploading to us-east-1), large files over long distances.
- Additional cost per GB transferred. Only charged if acceleration actually improves transfer speed.
- Use the [Speed Comparison Tool](http://s3-accelerate-speedtest.s3-accelerate.amazonaws.com/en/accelerate-speed-comparsion.html) to test benefit.

**Multi-Part Upload:**
- Splits a large file into parts (5 MB to 5 GB each) and uploads them in parallel.
- Required for files > 5 GB. Recommended for files > 100 MB.
- Benefits: Parallel uploads improve throughput, resume failed parts without re-uploading the entire file, begin upload before you know the total size.
- `CreateMultipartUpload` → `UploadPart` (parallel) → `CompleteMultipartUpload`.
- Abort incomplete uploads with lifecycle rules to avoid storage charges for orphaned parts.

**Key Difference:**
- Transfer Acceleration optimizes the network path (edge locations + AWS backbone).
- Multi-Part Upload optimizes the upload mechanism (parallelism + resilience).
- They are complementary — use both together for maximum performance on large, long-distance uploads.

---

## 13. How does S3 versioning work? What happens when you delete a versioned object?

**How Versioning Works:**
- Enabled at the bucket level. Once enabled, it can be suspended but never fully disabled.
- Every PUT/POST/COPY creates a new version with a unique version ID.
- GET without a version ID returns the latest (current) version.
- GET with a specific version ID returns that exact version.
- All versions consume storage and are billed.

**What happens when you delete a versioned object:**

- **Simple DELETE (no version ID specified):**
  - S3 does NOT actually delete the object. It inserts a "delete marker" as the current version.
  - The object appears deleted (GET returns 404), but all previous versions still exist.
  - To restore: delete the delete marker (DELETE with the delete marker's version ID).

- **DELETE with a specific version ID:**
  - Permanently deletes that specific version. This is irreversible.
  - Other versions are unaffected.

- **Deleting the delete marker:**
  - Effectively "undeletes" the object — the previous version becomes current again.

**MFA Delete:**
- Optional extra protection. Requires MFA to: permanently delete a version or change versioning state.
- Can only be enabled by the root account via the CLI (not the console).

> Important: Versioning + Lifecycle rules can manage costs — e.g., "delete non-current versions after 30 days" or "transition non-current versions to Glacier after 60 days."

---

## 14. Explain S3 replication — CRR vs SRR. What are the replication time control (RTC) guarantees?

**Cross-Region Replication (CRR):**
- Replicates objects from a source bucket in one region to a destination bucket in a different region.
- Use cases: Compliance (data in multiple regions), lower latency access for geographically distributed users, disaster recovery.

**Same-Region Replication (SRR):**
- Replicates objects between buckets in the same region.
- Use cases: Log aggregation from multiple buckets, replicate between production and test accounts, data sovereignty (keep data in-region but replicate for redundancy).

**Common Requirements:**
- Versioning must be enabled on both source and destination buckets.
- Source bucket needs an IAM role with permissions to replicate to the destination.
- Can replicate cross-account (destination bucket policy must allow the source role).
- Replication is asynchronous.
- Only new objects are replicated after enabling (use S3 Batch Replication for existing objects).
- Delete markers are NOT replicated by default (can be enabled). Permanent deletes (by version ID) are never replicated (to prevent malicious deletes).
- No chaining: if Bucket A → Bucket B → Bucket C, objects in A do NOT auto-replicate to C.

**Replication Time Control (RTC):**
- SLA: 99.99% of objects replicated within 15 minutes.
- Provides S3 Replication Metrics (replication latency, pending operations, failed operations) via CloudWatch.
- Includes S3 Replication Notifications (via EventBridge) for tracking replication status.
- Additional cost on top of standard replication.
- Best for compliance or business-critical workloads that need predictable replication times.

---

## 15. How would you design an S3 lifecycle policy to optimize costs for data with different access patterns?

**Example Scenario:** An application stores user uploads that are frequently accessed for 30 days, occasionally accessed for 90 days, rarely accessed for a year, and must be retained for 7 years for compliance.

**Lifecycle Policy Design:**

```json
{
  "Rules": [
    {
      "ID": "OptimizeCosts",
      "Status": "Enabled",
      "Filter": { "Prefix": "uploads/" },
      "Transitions": [
        {
          "Days": 30,
          "StorageClass": "STANDARD_IA"
        },
        {
          "Days": 90,
          "StorageClass": "INTELLIGENT_TIERING"
        },
        {
          "Days": 180,
          "StorageClass": "GLACIER_IR"
        },
        {
          "Days": 365,
          "StorageClass": "DEEP_ARCHIVE"
        }
      ],
      "NoncurrentVersionTransitions": [
        {
          "NoncurrentDays": 30,
          "StorageClass": "STANDARD_IA"
        },
        {
          "NoncurrentDays": 90,
          "StorageClass": "DEEP_ARCHIVE"
        }
      ],
      "NoncurrentVersionExpiration": {
        "NoncurrentDays": 365
      },
      "AbortIncompleteMultipartUpload": {
        "DaysAfterInitiation": 7
      }
    },
    {
      "ID": "ExpireAfter7Years",
      "Status": "Enabled",
      "Filter": { "Prefix": "uploads/" },
      "Expiration": {
        "Days": 2555
      }
    }
  ]
}
```

**Design Principles:**
- **Days 0–30 (S3 Standard):** Frequent access. Highest availability and performance.
- **Days 30–90 (S3 Standard-IA):** Lower cost, 128 KB minimum object size charge, per-GB retrieval fee. Min 30-day storage charge.
- **Days 90–180 (S3 Intelligent-Tiering):** Auto-moves objects between frequent/infrequent/archive tiers based on access patterns. Small monthly monitoring fee per object. Best when access patterns are unpredictable.
- **Days 180–365 (Glacier Instant Retrieval):** Millisecond retrieval, 90-day minimum. For quarterly access patterns.
- **Days 365+ (Deep Archive):** Cheapest. 180-day minimum. 12–48 hour retrieval. For compliance retention.
- **Non-current versions:** Aggressively transition and expire to avoid paying for old versions.
- **Abort incomplete multipart uploads:** Prevent orphaned parts from accumulating charges.

> Key constraints: Minimum storage duration charges apply per class. Objects must be ≥128 KB for IA classes (smaller objects are charged as 128 KB). Transition order must follow the storage class waterfall (can't go from Glacier back to Standard via lifecycle).


## 16. What is S3 Object Lock? Explain WORM compliance and governance modes.

**S3 Object Lock:**
- Prevents objects from being deleted or overwritten for a fixed retention period or indefinitely.
- Implements WORM (Write Once, Read Many) model.
- Must be enabled at bucket creation time (cannot be enabled on an existing bucket).
- Works only with versioned buckets (versioning is auto-enabled).
- Used for regulatory compliance (SEC 17a-4, CFTC, FINRA), ransomware protection, and audit trails.

**Retention Modes:**

- **Compliance Mode:**
  - No user — including the root account — can delete or overwrite the object or shorten the retention period until it expires.
  - Retention period cannot be reduced once set.
  - Strictest mode. Use for regulatory requirements where data immutability must be guaranteed.

- **Governance Mode:**
  - Most users cannot delete or overwrite the object.
  - Users with the `s3:BypassGovernanceRetention` IAM permission CAN override the lock (with the `x-amz-bypass-governance-retention: true` header).
  - Useful for testing retention policies or when you need an escape hatch for admins.

**Retention Period vs Legal Hold:**
- **Retention Period:** Fixed date or duration. Object is locked until the date passes.
- **Legal Hold:** No expiry date. Object is locked until the legal hold is explicitly removed. Independent of retention period. Any user with `s3:PutObjectLegalHold` permission can add/remove it.

**Default Retention:** You can set a default retention mode and period at the bucket level — applies to all new objects unless overridden per object.

---

## 17. How does S3 event notification work? Compare SNS, SQS, and Lambda as targets.

**How S3 Event Notifications Work:**
- S3 publishes events when certain operations occur on objects (e.g., PUT, POST, COPY, DELETE, restore from Glacier, replication failure).
- Configured at the bucket level. Filter by prefix and/or suffix (e.g., `images/` prefix, `.jpg` suffix).
- Delivery is asynchronous and at-least-once (rare duplicates possible).
- Can also use Amazon EventBridge as a target — enables more advanced filtering, multiple destinations, and archiving/replay.

**Targets:**

| Feature | SNS | SQS | Lambda |
|---------|-----|-----|--------|
| Pattern | Fan-out (1 event → multiple subscribers) | Decoupled queue (consumer pulls) | Direct processing |
| Use case | Notify multiple systems simultaneously | Buffer events, decouple producer/consumer | Real-time processing (resize image, update DB) |
| Consumers | Multiple (email, HTTP, SQS, Lambda) | Single consumer group | Single function invocation per event |
| Ordering | No guarantee | FIFO queue available | N/A |
| Retry | SNS retries delivery | Message stays in queue until processed | Lambda retries on failure (async invocation) |
| Throughput | High | High | Scales automatically |

**Common Patterns:**
- S3 → Lambda: Trigger image thumbnail generation on upload.
- S3 → SQS → Lambda: Buffer large bursts of uploads, process at controlled rate.
- S3 → SNS → (SQS + Lambda + Email): Fan-out to multiple consumers simultaneously.
- S3 → EventBridge: When you need content-based filtering, multiple rules, or cross-account targets.

**Note:** For S3 → SQS/SNS, the queue/topic resource policy must grant S3 permission to publish.

---

## 18. Secrets Manager vs Systems Manager Parameter Store — when to use which?

| Feature | Secrets Manager | SSM Parameter Store |
|---------|----------------|---------------------|
| Primary use | Secrets (DB passwords, API keys, OAuth tokens) | Configuration values + secrets |
| Automatic rotation | Yes — built-in rotation via Lambda for RDS, Redshift, DocumentDB, and custom | No native rotation (can build with Lambda + EventBridge) |
| Cost | ~$0.40/secret/month + $0.05 per 10K API calls | Standard tier: Free. Advanced tier: $0.05/parameter/month |
| Encryption | Always encrypted with KMS | Standard: SSM-managed key or KMS. SecureString: KMS required |
| Versioning | Yes — multiple versions, staging labels (AWSCURRENT, AWSPENDING, AWSPREVIOUS) | Yes — limited version history |
| Cross-account access | Yes — via resource policy | No native cross-account (use Secrets Manager instead) |
| Max value size | 65,536 bytes | Standard: 4 KB. Advanced: 8 KB |
| Integration | RDS, ECS, EKS, Lambda, CloudFormation | EC2, ECS, Lambda, CloudFormation, Systems Manager |

**When to use Secrets Manager:**
- Storing database credentials, API keys, OAuth tokens that need automatic rotation.
- Cross-account secret sharing.
- Compliance requirements for secret lifecycle management.

**When to use Parameter Store:**
- Application configuration (feature flags, environment variables, non-secret config).
- Hierarchical config organization (e.g., `/myapp/prod/db_host`).
- Cost-sensitive workloads with many parameters (free standard tier).
- Storing secrets that don't need automatic rotation.

**Tip:** Parameter Store can reference Secrets Manager secrets — use Parameter Store for config and Secrets Manager for secrets, then reference both uniformly via SSM paths.

---

## 19. AWS Key Management Service (KMS) — CMKs, key policies, envelope encryption.

**KMS Key Types:**

- **AWS Managed Keys:** Created and managed by AWS on your behalf for a specific service (e.g., `aws/s3`, `aws/ebs`). Free. You cannot manage rotation or key policy directly. Rotated automatically every year.
- **Customer Managed Keys (CMK):** You create and manage. $1/month/key + $0.03 per 10K API calls. Full control over key policy, rotation, and deletion. Can be rotated automatically (every year) or manually. Can be used cross-account.
- **AWS Owned Keys:** Owned and managed entirely by AWS. Not visible in your account. No cost. Used by some services internally.

**Key Policies:**
- Every KMS key has a key policy (resource-based policy). Unlike IAM, KMS key policies are the primary access control mechanism.
- The default key policy grants the root account full access, allowing IAM policies to also control access.
- Key policy must explicitly allow the principal — IAM policy alone is not sufficient unless the key policy grants the account root access.
- Key policies control: who can use the key (kms:Encrypt, kms:Decrypt), who can manage the key (kms:CreateKey, kms:ScheduleKeyDeletion), and cross-account grants.
- **Grants:** Temporary, programmatic delegation of key usage permissions. Used by AWS services (e.g., EBS, RDS) to use your CMK on your behalf.

**Envelope Encryption:**
- KMS has a 4 KB limit on data it can encrypt directly.
- For larger data, envelope encryption is used:
  1. KMS generates a **Data Encryption Key (DEK)** — a plaintext key + an encrypted copy.
  2. Your application uses the plaintext DEK to encrypt the data locally (AES-256).
  3. The plaintext DEK is discarded. Only the encrypted DEK is stored alongside the encrypted data.
  4. To decrypt: call KMS to decrypt the encrypted DEK → use the plaintext DEK to decrypt the data locally.
- Benefits: Only the small DEK travels to KMS. Large data is encrypted locally. Reduces KMS API calls and latency.
- Used by: S3, EBS, RDS, Secrets Manager, and the AWS Encryption SDK.

**Key Deletion:** 7–30 day waiting period before deletion. Cannot be cancelled once the waiting period ends. Disable the key instead if unsure.

---

## 20. What is GuardDuty, WAF, AWS Shield (Standard vs Advanced), and AWS Network Firewall?

**Amazon GuardDuty:**
- Intelligent threat detection service. Analyzes VPC Flow Logs, CloudTrail events, DNS logs, and EKS audit logs using ML and threat intelligence.
- Detects: compromised EC2 instances (crypto mining, C&C communication), credential exfiltration, unusual API calls, port scanning.
- No agents to install. Enabled per region. Multi-account support via AWS Organizations.
- Findings sent to EventBridge for automated remediation (e.g., isolate instance via Lambda).

**AWS WAF (Web Application Firewall):**
- Layer 7 firewall. Protects against common web exploits: SQL injection, XSS, HTTP floods.
- Deployed on: ALB, CloudFront, API Gateway, AppSync, Cognito User Pool.
- Rules: AWS Managed Rules (pre-built rule groups), custom rules (IP sets, regex, rate-based), Bot Control, Fraud Control.
- Web ACL: collection of rules applied to a resource. Rules evaluated in priority order.
- Rate-based rules: block IPs exceeding a request threshold (DDoS mitigation).

**AWS Shield:**

| Feature | Shield Standard | Shield Advanced |
|---------|----------------|-----------------|
| Cost | Free (automatic) | $3,000/month/org |
| Protection | Layer 3/4 DDoS (SYN floods, UDP reflection) | Layer 3/4/7 DDoS + application layer |
| Coverage | All AWS customers automatically | EC2, ELB, CloudFront, Route 53, Global Accelerator |
| DDoS Response Team | No | 24/7 AWS DRT access |
| Cost protection | No | Credits for scaling costs during DDoS |
| Advanced metrics | No | Real-time attack visibility in CloudWatch |

**AWS Network Firewall:**
- Managed stateful network firewall for VPCs. Deployed in a dedicated firewall subnet.
- Supports: stateful inspection, intrusion prevention (IPS), domain-based filtering (block specific FQDNs), protocol detection.
- Uses Suricata-compatible rules. Can import existing Suricata rule sets.
- Centrally managed across accounts via AWS Firewall Manager.
- Use for: filtering traffic between VPCs, filtering egress to internet, compliance requirements for deep packet inspection.

**Layered Security Summary:**
- GuardDuty → threat detection (reactive)
- WAF → Layer 7 application protection (proactive)
- Shield → DDoS protection (proactive)
- Network Firewall → network-level traffic filtering within VPC (proactive)

---

## 21. What is the difference between an IAM role, IAM user, and IAM group? When would you use each?

**IAM User:**
- A permanent identity representing a person or application. Has long-term credentials (password + access keys).
- Use for: human users who need AWS Console/CLI access (though AWS recommends using IAM Identity Center instead), service accounts for legacy applications that don't support roles.
- Best practice: Enable MFA, rotate access keys regularly, apply least privilege.

**IAM Group:**
- A collection of IAM users. Policies attached to a group apply to all members.
- Groups cannot be nested (no groups within groups).
- Use for: managing permissions for teams (e.g., "Developers" group with dev permissions, "Admins" group with admin permissions).
- A user can belong to multiple groups. Permissions are the union of all group policies + user policies.

**IAM Role:**
- An identity with permissions but no long-term credentials. Assumed temporarily via STS (Security Token Service) — issues short-term credentials (15 min to 12 hours).
- Has a trust policy (who can assume it) and a permissions policy (what they can do).
- Use for:
  - EC2/Lambda/ECS tasks needing AWS API access (instance profiles / execution roles).
  - Cross-account access (Role in Account B trusted by Account A).
  - Federated access (SAML, OIDC — corporate SSO, web identity).
  - AWS services acting on your behalf (e.g., CodePipeline deploying to ECS).

**Summary:**
- Human users → IAM Identity Center (SSO) or IAM Users (legacy)
- Teams/groups of users → IAM Groups
- Applications, services, cross-account, federation → IAM Roles (always prefer roles over long-term keys)

---

## 22. Explain IAM permission boundaries. How do they differ from SCPs in AWS Organizations?

**IAM Permission Boundaries:**
- An advanced feature that sets the maximum permissions an IAM entity (user or role) can have.
- A permission boundary is an IAM managed policy attached to a user/role. The effective permissions = intersection of the identity policy AND the permission boundary.
- Even if the identity policy grants `s3:*`, if the boundary only allows `s3:GetObject`, only `s3:GetObject` is allowed.
- Use case: Delegate IAM administration safely. Allow developers to create roles for their applications, but restrict those roles to only the permissions the developer themselves has (prevents privilege escalation).
- Does NOT apply to resource-based policies or service-linked roles.

**SCPs (Service Control Policies) in AWS Organizations:**
- Policies applied at the Organization, OU (Organizational Unit), or account level.
- Set the maximum permissions for all IAM entities (users, roles) within the account(s).
- SCPs do NOT grant permissions — they only restrict. An SCP allowing `s3:*` doesn't mean all users have S3 access; IAM policies still need to grant it.
- Apply to all principals in the account including the root user (except the management account).
- Use case: Guardrails across the organization — prevent any account from disabling CloudTrail, restrict regions, prevent leaving the organization.

**Key Differences:**

| Feature | Permission Boundary | SCP |
|---------|-------------------|-----|
| Scope | Single IAM user or role | Entire AWS account / OU / Org |
| Applied by | IAM admin in the account | Organization management account |
- Both are "guardrails" — they restrict but don't grant.
- Effective permissions = intersection of SCP + Permission Boundary + Identity Policy + Resource Policy.

---

## 23. What is AWS Cognito? Explain User Pools vs Identity Pools.

**AWS Cognito:**
- Fully managed authentication, authorization, and user management service for web and mobile applications.
- Two main components: User Pools and Identity Pools (often used together).

**User Pools:**
- A user directory that provides sign-up and sign-in functionality.
- Handles: username/password auth, MFA, email/phone verification, password policies, account recovery.
- Supports federation: users can sign in with Google, Facebook, Apple, SAML, or OIDC providers.
- Returns JWT tokens (ID token, access token, refresh token) after successful authentication.
- Integrates with ALB and API Gateway for token-based authorization.
- Use for: authenticating users into your application (who are you?).

**Identity Pools (Federated Identities):**
- Provides temporary AWS credentials (via STS) to grant users access to AWS services directly.
- Users can be authenticated (from a User Pool, social provider, SAML) or unauthenticated (guest access).
- Maps authenticated/unauthenticated users to IAM roles.
- Use for: authorizing users to access AWS resources directly (e.g., upload to S3, query DynamoDB) — (what can you do in AWS?).

**Typical Flow (User Pool + Identity Pool together):**
1. User signs in via Cognito User Pool → receives JWT tokens.
2. JWT token is exchanged with Identity Pool → STS issues temporary AWS credentials.
3. User uses credentials to access S3, DynamoDB, etc. directly from the client.

**Use Cases:**
- User Pool only: Web/mobile app login, API Gateway authorization.
- Identity Pool only: Grant guest users limited S3 access.
- Both: Full auth flow where authenticated users get scoped AWS resource access.

---

## 24. How does cross-account access work using IAM roles? Walk through the trust policy mechanism.

**How It Works:**
- Account A (trusting account) has a role with a trust policy that allows Account B (trusted account) to assume it.
- A principal in Account B calls `sts:AssumeRole` with the role ARN from Account A.
- STS returns temporary credentials (access key, secret key, session token) scoped to the role's permissions.
- The principal uses those credentials to make API calls in Account A.

**Step-by-Step:**

1. **In Account A — Create the role with a trust policy:**
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": { "AWS": "arn:aws:iam::ACCOUNT_B_ID:root" },
    "Action": "sts:AssumeRole",
    "Condition": {
      "StringEquals": { "sts:ExternalId": "unique-external-id" }
    }
  }]
}
```

2. **In Account A — Attach a permissions policy to the role** (e.g., S3 read access).

3. **In Account B — Grant the user/role permission to assume the role in Account A:**
```json
{
  "Effect": "Allow",
  "Action": "sts:AssumeRole",
  "Resource": "arn:aws:iam::ACCOUNT_A_ID:role/CrossAccountRole"
}
```

4. **Principal in Account B assumes the role:**
```bash
aws sts assume-role \
  --role-arn "arn:aws:iam::ACCOUNT_A_ID:role/CrossAccountRole" \
  --role-session-name "MySession" \
  --external-id "unique-external-id"
```

**External ID:**
- Used to prevent the "confused deputy" problem — a third-party service assuming your role on behalf of a malicious actor.
- The external ID is a secret shared between you and the third party. Only they know it, so only they can assume the role.

**Session Duration:** Default 1 hour. Max 12 hours (if role's max session duration is set accordingly).

---

## 25. What is AWS Macie? How does it complement GuardDuty?

**AWS Macie:**
- Fully managed data security service that uses ML to automatically discover, classify, and protect sensitive data in S3.
- Scans S3 buckets for: PII (names, addresses, credit card numbers, SSNs, passport numbers), credentials (API keys, private keys), financial data, healthcare data (PHI).
- Provides: S3 bucket inventory with security posture (public access, encryption status, replication), sensitive data findings with object-level detail.
- Findings sent to EventBridge for automated remediation or alerting.
- Multi-account support via AWS Organizations.
- Pricing: per GB of data scanned + per S3 bucket evaluated.

**How Macie Complements GuardDuty:**

| Dimension | Macie | GuardDuty |
|-----------|-------|-----------|
| Focus | Data security — what sensitive data exists and where | Threat detection — malicious activity and behavior |
| Data source | S3 object content | VPC Flow Logs, CloudTrail, DNS logs, EKS audit logs |
| Detects | PII exposure, unencrypted sensitive data, public buckets with sensitive data | Compromised credentials, crypto mining, port scanning, C&C traffic |
| Question answered | "Do I have sensitive data exposed?" | "Is my infrastructure under attack?" |

**Together they provide:**
- Macie: "You have credit card numbers in a publicly accessible S3 bucket."
- GuardDuty: "Someone is exfiltrating data from that S3 bucket using compromised credentials."
- Combined: Full visibility into both data risk posture and active threats.

---

## 26. Explain the principle of least privilege. How do you audit and enforce it at scale using AWS tools?

**Principle of Least Privilege:**
- Grant only the minimum permissions required to perform a task — nothing more.
- Reduces blast radius of compromised credentials, insider threats, and misconfiguration.

**Enforcing at Scale:**

- **IAM Access Analyzer:**
  - Identifies resources shared with external principals (S3 buckets, IAM roles, KMS keys, Lambda functions, SQS queues, Secrets Manager secrets).
  - Generates least-privilege policies by analyzing CloudTrail activity — shows which permissions were actually used.
  - Policy validation: checks policies for errors, security warnings, and suggestions before deployment.

- **AWS Organizations SCPs:**
  - Apply guardrails at the OU/account level. Prevent any principal from performing high-risk actions (e.g., disable CloudTrail, create IAM users, leave the org).

- **IAM Permission Boundaries:**
  - Cap the maximum permissions delegated IAM admins can grant to new roles/users.

- **AWS Config Rules:**
  - `iam-no-inline-policy` — flag inline policies (prefer managed policies for auditability).
  - `iam-policy-no-statements-with-admin-access` — detect `*:*` policies.
  - `iam-root-access-key-check` — ensure root has no access keys.
  - `access-keys-rotated` — enforce key rotation.

- **CloudTrail + Athena/CloudWatch Insights:**
  - Query CloudTrail logs to find unused permissions, identify over-privileged roles, detect anomalous API calls.

- **IAM Last Accessed Information:**
  - Shows when a service was last accessed by a user/role. Remove permissions for services not accessed in 90+ days.

- **AWS Security Hub:**
  - Aggregates findings from GuardDuty, Macie, IAM Access Analyzer, Config, Inspector into a single dashboard. Scores against CIS AWS Foundations Benchmark and AWS Foundational Security Best Practices.

---

## 27. AWS CloudFront vs Global Accelerator — key differences, use cases, and caching behavior.

**CloudFront:**
- CDN (Content Delivery Network). Caches content at 400+ edge locations globally.
- Layer 7 (HTTP/HTTPS). Supports caching of static and dynamic content.
- Cache behavior: TTL-based. Cache-Control headers from origin control caching. Supports cache invalidation.
- Origins: S3, ALB, EC2, API Gateway, any HTTP endpoint.
- Features: WAF integration, Lambda@Edge / CloudFront Functions (run code at edge), signed URLs/cookies (private content), Origin Shield (additional caching layer to reduce origin load), field-level encryption.
- Best for: Static assets (images, JS, CSS), video streaming, API acceleration with caching, websites with global users.
- Protocols: HTTP, HTTPS, WebSocket.

**Global Accelerator:**
- Network-level (Layer 3/4) acceleration. No caching.
- Routes traffic through AWS global network (anycast) to the nearest AWS edge location, then over AWS backbone to the origin.
- Provides 2 static anycast IP addresses (consistent IPs — useful for whitelisting in firewalls).
- Supports: TCP, UDP (not just HTTP). Works with ALB, NLB, EC2, Elastic IPs.
- Health checks: Automatically routes around unhealthy endpoints. Instant failover (<30 seconds).
- Best for: Non-HTTP workloads (gaming, IoT, VoIP), applications requiring static IPs, multi-region active-active or active-passive failover, latency-sensitive TCP/UDP applications.

**Key Differences:**

| Feature | CloudFront | Global Accelerator |
|---------|-----------|-------------------|
| Layer | 7 (HTTP/HTTPS) | 3/4 (TCP/UDP) |
| Caching | Yes | No |
| Static IPs | No (uses DNS) | Yes (2 anycast IPs) |
| Protocols | HTTP, HTTPS, WS | TCP, UDP |
| Use case | Content delivery, caching | Network performance, non-HTTP, static IPs |
| Pricing | Per GB transferred + requests | Per accelerator/hour + per GB |

---

## 28. Different types of Gateways in AWS — Internet Gateway, NAT Gateway, Transit Gateway, VPN Gateway.

**Internet Gateway (IGW):**
- Horizontally scaled, redundant, HA VPC component. Allows communication between VPC and the internet.
- Performs NAT for instances with public IPs (maps public IP ↔ private IP).
- One IGW per VPC. Attach to VPC, then add route `0.0.0.0/0 → IGW` in public subnet route table.
- Stateful — return traffic is automatically allowed.

**NAT Gateway:**
- Allows instances in private subnets to initiate outbound internet traffic (e.g., software updates) without being reachable from the internet.
- Deployed in a public subnet with an Elastic IP. Private subnet route: `0.0.0.0/0 → NAT Gateway`.
- Managed by AWS — highly available within an AZ. Deploy one per AZ for HA.
- Supports TCP, UDP, ICMP. Bandwidth: 5 Gbps, scales to 100 Gbps.
- NAT Instance (legacy): EC2-based, self-managed, cheaper but single point of failure.

**Transit Gateway (TGW):**
- Regional hub-and-spoke network transit hub. Connects VPCs, VPNs, and Direct Connect gateways through a single gateway.
- Replaces complex VPC peering meshes (N*(N-1)/2 peering connections → N attachments to TGW).
- Supports: VPC attachments, VPN attachments, Direct Connect Gateway attachments, peering with other TGWs (cross-region).
- Route tables on TGW control which attachments can communicate.
- Supports multicast. Can be shared across accounts via AWS Resource Access Manager (RAM).

**Virtual Private Gateway (VGW) / VPN Gateway:**
- The AWS side of a Site-to-Site VPN connection or Direct Connect connection.
- Attached to a VPC. On-premises Customer Gateway (CGW) connects to the VGW over IPsec VPN tunnels.
- Each VPN connection has 2 tunnels for redundancy (different AZs).
- For Direct Connect: VGW is the target for private VIF (Virtual Interface) to connect to a VPC.
- Limitation: One VGW per VPC. For connecting many VPCs to on-premises, use Transit Gateway instead.

---

## 29. VPC connectivity options — VPC peering, Transit Gateway, VPN, Direct Connect, PrivateLink.

**VPC Peering:**
- Direct, private connection between two VPCs (same or different accounts/regions).
- Traffic stays on AWS network. No bandwidth bottleneck, no single point of failure.
- Non-transitive: if A↔B and B↔C, A cannot reach C through B. Must create A↔C peering separately.
- CIDR ranges must not overlap.
- Best for: Simple, point-to-point VPC connectivity. Small number of VPCs.

**Transit Gateway:**
- Hub-and-spoke model. Connects many VPCs and on-premises networks through one gateway.
- Transitive routing: all attachments can communicate (controlled by TGW route tables).
- Supports inter-region peering (TGW ↔ TGW).
- Best for: Large-scale, multi-VPC architectures. Centralized network management.

**Site-to-Site VPN:**
- Encrypted IPsec tunnel over the public internet between on-premises and AWS (VGW or TGW).
- Quick to set up (minutes). Low cost. 2 tunnels per connection for redundancy.
- Bandwidth: up to 1.25 Gbps per tunnel. Latency varies (internet-dependent).
- Best for: Quick on-premises connectivity, backup for Direct Connect, low-bandwidth use cases.

**AWS Direct Connect (DX):**
- Dedicated private network connection from on-premises to AWS (bypasses internet).
- Consistent latency, higher bandwidth (1 Gbps, 10 Gbps, 100 Gbps), lower data transfer costs.
- Lead time: weeks to months to provision.
- Connection types: Dedicated (physical port at DX location) or Hosted (via DX partner, sub-1Gbps available).
- Virtual Interfaces: Private VIF (to VPC via VGW), Public VIF (to AWS public services), Transit VIF (to TGW).
- Best for: High-bandwidth, latency-sensitive workloads, large data transfers, hybrid cloud.
- For HA: use two DX connections from different locations, or DX + VPN as backup.

**AWS PrivateLink:**
- Expose a service in your VPC to other VPCs privately without VPC peering, IGW, or NAT.
- Provider creates a Network Load Balancer in front of their service. Consumer creates an Interface VPC Endpoint (ENI in their subnet).
- Traffic never leaves AWS network. No CIDR overlap issues.
- Best for: SaaS services, sharing services across accounts/VPCs at scale, accessing AWS services privately.

---

## 30. What is a VPC endpoint? Explain interface endpoints vs gateway endpoints.

**VPC Endpoint:**
- Allows private connectivity from your VPC to AWS services or PrivateLink-powered services without requiring an IGW, NAT Gateway, VPN, or Direct Connect.
- Traffic stays within the AWS network.

**Gateway Endpoints:**
- Supported services: S3 and DynamoDB only.
- Free of charge.
- A route table entry is added pointing the service's prefix list to the gateway endpoint.
- Does not use an ENI — no IP address in your subnet.
- Scoped to the VPC. Cannot be extended to on-premises (via VPN/DX) or to other VPCs via peering.
- Access controlled via endpoint policies (in addition to bucket/table policies).
- Best for: Private EC2 instances accessing S3 or DynamoDB without NAT Gateway.

**Interface Endpoints (PrivateLink):**
- Supported services: Most AWS services (SSM, Secrets Manager, KMS, SNS, SQS, CloudWatch, API Gateway, etc.) + third-party PrivateLink services.
- Creates an ENI with a private IP address in your subnet.
- Cost: ~$0.01/hour per AZ + $0.01 per GB processed.
- DNS: AWS creates private DNS names that resolve to the ENI's private IP (e.g., `ssm.us-east-1.amazonaws.com` resolves to the ENI IP within the VPC). Requires `enableDnsHostnames` and `enableDnsSupport` on the VPC.
- Can be accessed from on-premises via VPN or Direct Connect (unlike gateway endpoints).
- Deploy one per AZ for high availability.
- Access controlled via endpoint policies.

**Comparison:**

| Feature | Gateway Endpoint | Interface Endpoint |
|---------|-----------------|-------------------|
| Services | S3, DynamoDB only | Most AWS services |
| Cost | Free | Per hour + per GB |
| Implementation | Route table entry | ENI in subnet |
| On-premises access | No | Yes (via VPN/DX) |
| Cross-VPC access | No | Yes (via peering/TGW) |
| DNS | No private DNS | Private DNS available |
