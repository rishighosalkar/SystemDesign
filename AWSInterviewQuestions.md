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

## 31 How does Route 53 routing work? Compare latency, weighted, failover, geolocation, and geoproximity policies.

Answer:
Route 53 is AWS's DNS service that routes end-user requests to endpoints using various routing policies:

- **Latency-based routing**: Routes traffic to the AWS region that provides the lowest latency for the user. Route 53 measures latency between the user's DNS resolver and AWS regions, then returns the record with the lowest latency.

- **Weighted routing**: Distributes traffic across multiple resources based on assigned weights (0–255). E.g., 70% to one server, 30% to another. Useful for blue-green deployments or A/B testing.

- **Failover routing**: Uses active-passive configuration. Routes to the primary resource; if health checks fail, automatically routes to the secondary (standby) resource. Used for disaster recovery.

- **Geolocation routing**: Routes based on the geographic location of the user (continent, country, or state). If no match is found, a default record is used. Useful for content localization or compliance restrictions.

- **Geoproximity routing** (Traffic Flow only): Routes based on geographic location of resources AND users, with an adjustable bias to expand or shrink the geographic region from which traffic is routed to a resource.

Key differences:
| Policy | Basis | Use Case |
|--------|-------|----------|
| Latency | Network latency measurement | Best performance |
| Weighted | Assigned proportions | Load distribution, canary |
| Failover | Health check status | DR / HA |
| Geolocation | User's location | Compliance, localization |
| Geoproximity | User + resource location + bias | Fine-grained geographic control |

---

## 32 What is AWS PrivateLink? How is it different from VPC peering?

Answer:
**AWS PrivateLink** provides private connectivity between VPCs, AWS services, and on-premises networks without exposing traffic to the public internet. It uses interface VPC endpoints (ENIs with private IPs) in your VPC.

**VPC Peering** creates a networking connection between two VPCs that enables routing using private IP addresses (as if they are in the same network).

| Aspect | PrivateLink | VPC Peering |
|--------|-------------|-------------|
| Connectivity model | Consumer → Provider (unidirectional) | Bidirectional full network access |
| Scope | Exposes specific services/applications | Entire VPC CIDR is routable |
| Overlapping CIDRs | Supported | Not supported |
| Transitive routing | Not applicable | Not supported |
| Cross-account/region | Yes | Yes |
| Security | Least-privilege — only the exposed service is reachable | Broad — all resources in peered VPC are reachable (unless restricted by SGs/NACLs) |
| Use case | SaaS consumption, microservices, marketplace | Full network integration between VPCs |

Choose PrivateLink when you want to expose a single service securely. Choose VPC Peering when two VPCs need full bidirectional communication.

---

## 33 Explain security groups vs NACLs — stateful vs stateless, evaluation order.

Answer:
| Feature | Security Groups | NACLs |
|---------|----------------|-------|
| Level | Instance (ENI) level | Subnet level |
| Statefulness | **Stateful** — return traffic is automatically allowed regardless of outbound rules | **Stateless** — return traffic must be explicitly allowed by rules |
| Rule type | Allow rules only (implicit deny) | Allow AND Deny rules |
| Evaluation | All rules evaluated together; if any rule allows, traffic passes | Rules evaluated in **number order** (lowest first); first match wins |
| Default | Denies all inbound, allows all outbound | Default NACL allows all; custom NACL denies all |
| Association | Applied to specific instances | Applied to all instances in the subnet |

**Evaluation order in practice:**
1. Traffic enters subnet → NACL inbound rules evaluated (numbered order, first match).
2. Traffic reaches instance → Security Group inbound rules evaluated (all rules, any allow = pass).
3. Response leaves instance → Security Group allows automatically (stateful).
4. Response leaves subnet → NACL outbound rules evaluated (stateless, must explicitly allow).

Best practice: Use Security Groups as primary defense (instance-level), NACLs as secondary subnet-level guardrail.

---

## 34 How would you design a multi-region active-active architecture on AWS?

Answer:
Key components:

1. **Global traffic routing**: Route 53 with latency-based or geoproximity routing to direct users to the nearest region.

2. **Data replication**:
   - DynamoDB Global Tables (multi-master, automatic replication)
   - Aurora Global Database (1 writer region, <1s replication to read replicas in other regions; can promote on failover)
   - S3 Cross-Region Replication (CRR)

3. **Compute**: Deploy identical application stacks in each region using CloudFormation StackSets or Terraform. Use ECS/EKS/Lambda in each region independently.

4. **Conflict resolution**: For true active-active writes, use last-writer-wins (DynamoDB Global Tables) or application-level conflict resolution (CRDTs).

5. **Session management**: Use DynamoDB or ElastiCache Global Datastore for session state so users can be routed to any region.

6. **Caching**: ElastiCache Global Datastore or CloudFront for edge caching.

7. **Event replication**: EventBridge cross-region event buses or SNS cross-region fanout.

8. **Health checks & failover**: Route 53 health checks on each region's endpoints; automatic DNS failover if a region becomes unhealthy.

9. **CI/CD**: Deploy to all regions simultaneously; use CodePipeline with cross-region actions.

10. **Observability**: CloudWatch cross-account/cross-region dashboards, X-Ray for distributed tracing.

Challenges: Data consistency (eventual vs strong), conflict resolution, cost (2x+ infrastructure), testing failover scenarios.

---

## 35 What is AWS Direct Connect? When would you use it over a VPN connection?

Answer:
**AWS Direct Connect** is a dedicated private network connection from your on-premises data center to AWS. It bypasses the public internet entirely.

| Aspect | Direct Connect | Site-to-Site VPN |
|--------|---------------|-----------------|
| Connection | Dedicated fiber (1 Gbps, 10 Gbps, 100 Gbps) or hosted (50 Mbps–10 Gbps) | Encrypted tunnel over public internet |
| Latency | Consistent, low latency | Variable (internet-dependent) |
| Bandwidth | Up to 100 Gbps | Up to 1.25 Gbps per tunnel |
| Setup time | Weeks to months (physical cross-connect) | Minutes |
| Cost | Higher upfront (port hours + data transfer) | Lower (per-hour + data transfer) |
| Encryption | Not encrypted by default (add VPN over DX for encryption) | IPSec encrypted |
| Redundancy | Need 2 connections at different locations for HA | Multiple tunnels easy to set up |

**Use Direct Connect when:**
- You need consistent, predictable low-latency connectivity
- High-throughput workloads (large data transfers, backups, migrations)
- Regulatory requirements for private connectivity
- Hybrid architectures with heavy bidirectional traffic
- Cost optimization on high-volume data transfer (cheaper egress rates)

**Use VPN when:**
- Quick setup needed
- Lower bandwidth requirements
- Backup/failover for Direct Connect
- Encryption is mandatory without additional overlay

Common pattern: Direct Connect as primary + VPN as backup failover path.

---

## 36 How does AWS Lambda deployment work — packages, container images, layers?

Answer:

**1. Deployment Packages (ZIP):**
- Bundle your code + dependencies into a .zip file
- Upload directly (< 50 MB) or via S3 (up to 250 MB unzipped)
- Fastest cold start; simplest approach for small functions

**2. Container Images:**
- Package function as an OCI-compliant container image (up to 10 GB)
- Must implement the Lambda Runtime Interface Client (RIC)
- Use AWS base images (have RIC built-in) or custom images
- Stored in ECR; Lambda pulls and caches the image
- Use case: large dependencies (ML models, heavy libraries), existing CI/CD container pipelines

**3. Lambda Layers:**
- Reusable ZIP archives containing libraries, custom runtimes, or data
- Up to 5 layers per function; total unzipped size ≤ 250 MB (with function code)
- Shared across multiple functions — avoids duplication
- Versioned and immutable once published
- Use case: common dependencies (numpy, SDK), custom runtimes, shared utilities

**Deployment methods:**
- AWS Console (inline editor for small functions)
- AWS CLI / SDK (`aws lambda update-function-code`)
- SAM (`sam build && sam deploy`)
- CloudFormation / CDK
- Terraform

**Versioning & Aliases:**
- Publish immutable versions ($LATEST is mutable)
- Aliases (e.g., "prod", "staging") point to specific versions
- Aliases support weighted traffic shifting for canary deployments

---

## 37 Why is Lambda suited only for lightweight functions? (cold start, 15-min timeout, memory limits)

Answer:

Lambda is designed for **short-lived, event-driven, stateless** workloads. Its constraints make it unsuitable for heavy/long-running processes:

**1. Cold Starts:**
- First invocation (or after idle period) requires: downloading code, starting runtime, initializing handler
- Adds 100ms–10s latency depending on runtime, package size, VPC config
- Problematic for latency-sensitive synchronous APIs
- Mitigated by provisioned concurrency (but adds cost)

**2. 15-Minute Timeout:**
- Maximum execution duration is 900 seconds
- Not suitable for: ETL on large datasets, video transcoding, long-running batch jobs, ML training
- Alternative: Step Functions (orchestrate multiple short Lambdas), ECS/Fargate for long tasks

**3. Memory & CPU Limits:**
- Memory: 128 MB to 10,240 MB (10 GB)
- CPU scales linearly with memory (1 vCPU at 1,769 MB)
- No GPU access
- /tmp storage: 512 MB (configurable up to 10 GB)

**4. Payload Limits:**
- Synchronous invocation: 6 MB request/response
- Asynchronous: 256 KB event payload

**5. Concurrency Limits:**
- Default 1,000 concurrent executions per region (soft limit, can be increased)

**6. Statelessness:**
- No persistent local state between invocations
- Must use external stores (DynamoDB, S3, ElastiCache)

**Best suited for:** API backends, event processing, file processing, scheduled tasks, stream processing — all short-duration, bursty workloads.

---

## 38 What is Lambda concurrency? Explain reserved vs provisioned concurrency.

Answer:

**Lambda Concurrency** = number of function instances serving requests simultaneously. One instance handles one request at a time.

**Account-level limit:** 1,000 concurrent executions per region (default, can be increased).

**Unreserved concurrency:** Shared pool available to all functions. If one function spikes, it can starve others.

**Reserved Concurrency:**
- Guarantees a set number of concurrent instances for a specific function
- Acts as both a **guarantee** (always available) and a **cap** (cannot exceed)
- Subtracts from the account pool — other functions cannot use this reserved capacity
- No additional cost
- Use case: Ensure critical functions always have capacity; throttle non-critical functions

**Provisioned Concurrency:**
- Pre-initializes a specified number of execution environments (warm instances)
- **Eliminates cold starts** — instances are ready to respond immediately
- You pay for provisioned concurrency even when idle (per-GB-hour)
- Can use Application Auto Scaling to adjust provisioned concurrency based on schedule or utilization
- Use case: Latency-sensitive APIs, predictable traffic patterns

| Aspect | Reserved | Provisioned |
|--------|----------|-------------|
| Purpose | Guarantee capacity + cap | Eliminate cold starts |
| Cold starts | Still possible | Eliminated (up to provisioned count) |
| Cost | Free | Additional charge |
| Scaling | Scales on-demand within reserved limit | Pre-warmed; scales beyond with cold starts |
| Throttling | Requests beyond reserved limit get throttled (429) | Requests beyond provisioned count get cold starts (not throttled unless reserved is also set) |

---

## 39 How does API Gateway handle throttling? Explain burst limit, rate limit, and usage plans.

Answer:

**API Gateway Throttling** protects backend services from traffic spikes using the token bucket algorithm.

**Account-level defaults (per region):**
- **Rate limit (steady-state):** 10,000 requests/second
- **Burst limit:** 5,000 requests (token bucket capacity)

**How token bucket works:**
- Bucket fills at the steady-state rate (10,000 tokens/sec)
- Burst allows short spikes up to 5,000 concurrent requests
- Once burst tokens are exhausted, requests are throttled to the steady-state rate
- Throttled requests receive HTTP 429 (Too Many Requests)

**Throttling levels (evaluated in order):**
1. **Account-level** — hard ceiling across all APIs in the region
2. **Stage-level** — per-stage default limits (configurable)
3. **Method-level** — per-resource/method override (e.g., POST /orders = 100 rps)
4. **Usage plan level** — per API key throttling

**Usage Plans:**
- Associate API keys with throttling limits and quota
- **Throttle settings:** rate + burst per API key
- **Quota:** maximum number of requests in a given time period (day/week/month)
- Use case: Tiered access (free tier: 100 rps, premium: 5,000 rps), partner APIs, monetization

**Best practices:**
- Set method-level throttling for expensive operations
- Use caching to reduce backend calls
- Implement retry with exponential backoff on client side
- Monitor with CloudWatch metrics: Count, 4XXError, 5XXError, Latency

---

## 40 What are the differences between REST API, HTTP API, and WebSocket API in API Gateway?

Answer:

| Feature | REST API | HTTP API | WebSocket API |
|---------|----------|----------|---------------|
| Protocol | RESTful HTTP | RESTful HTTP | WebSocket (persistent connection) |
| Cost | Higher (~$3.50/million) | Lower (~$1.00/million) — 71% cheaper | Per-message + connection-minutes |
| Latency | Higher | Lower (designed for low latency) | Real-time bidirectional |
| Auth | IAM, Cognito, Lambda authorizer, API keys | IAM, Cognito, JWT (native), Lambda authorizer | IAM, Lambda authorizer |
| Features | Full-featured: caching, request validation, WAF, resource policies, usage plans, API keys, request/response transformation | Minimal: no caching, no request validation, no WAF, no usage plans | Routes, connection management |
| Integration | Lambda, HTTP, AWS services, Mock, VPC Link | Lambda, HTTP, VPC Link, private ALB/NLB | Lambda, HTTP, AWS services |
| Deployment | Stage-based with canary | Automatic deployments | Stage-based |
| Use case | Full API management, enterprise APIs | Simple proxies, microservices, low-cost APIs | Chat, notifications, live dashboards, gaming |

**Choose REST API when:** You need caching, request validation, WAF integration, API keys/usage plans, or request/response transformations.

**Choose HTTP API when:** You want lower cost, lower latency, simpler proxy to Lambda/HTTP backends, and JWT authorization is sufficient.

**Choose WebSocket API when:** You need persistent bidirectional communication (real-time updates, chat, streaming).

---

## 41 How would you handle Lambda cold starts in a latency-sensitive production system?

Answer:

**1. Provisioned Concurrency (primary solution):**
- Pre-warms execution environments — eliminates cold starts entirely
- Use Application Auto Scaling (target tracking or scheduled) to adjust based on traffic patterns
- Cost: pay for idle provisioned instances

**2. Reduce package size:**
- Smaller deployment packages = faster cold starts
- Remove unused dependencies, use tree-shaking
- Use layers for shared dependencies (cached separately)

**3. Choose optimal runtime:**
- Python, Node.js, Go → faster cold starts (100–300ms)
- Java, .NET → slower cold starts (1–5s) unless using SnapStart (Java) or Native AOT (.NET)

**4. Lambda SnapStart (Java):**
- Takes a snapshot of initialized execution environment after init phase
- Resumes from snapshot on cold start — reduces Java cold starts from ~5s to ~200ms
- No additional cost

**5. Avoid VPC (if possible):**
- VPC-attached Lambdas previously had 10s+ cold starts (now improved with Hyperplane ENI caching)
- Still slightly slower than non-VPC; avoid unless necessary

**6. Keep functions warm (workaround):**
- Scheduled CloudWatch Events/EventBridge to invoke function every 5 minutes
- Hacky; doesn't scale well with concurrency; provisioned concurrency is better

**7. Optimize initialization code:**
- Move heavy initialization outside the handler (runs once per cold start)
- Lazy-load modules not needed on every invocation
- Use connection pooling for DB connections

**8. Architecture alternatives:**
- Use ALB + Fargate for ultra-latency-sensitive paths
- Use CloudFront Functions / Lambda@Edge for edge logic
- Hybrid: API Gateway → Fargate (hot path) + Lambda (background tasks)

---

## 42 Explain Lambda destinations and dead letter queues. When would you use each?

Answer:

**Dead Letter Queues (DLQ):**
- Configured on the Lambda function itself
- Captures **failed** asynchronous invocations (after all retries exhausted — default 2 retries)
- Targets: SQS queue or SNS topic only
- Contains only the original event payload
- No context about the error (limited metadata in message attributes)
- Legacy feature (pre-2019)

**Lambda Destinations:**
- Configured per function for **asynchronous invocations**
- Separate destinations for **success** and **failure**
- Targets: SQS, SNS, Lambda, EventBridge
- Includes rich context: request payload, response payload, error details, stack trace, request ID, timestamps
- More flexible routing (e.g., success → EventBridge for further processing)

| Aspect | DLQ | Destinations |
|--------|-----|-------------|
| Triggers on | Failure only | Success AND/OR Failure |
| Targets | SQS, SNS | SQS, SNS, Lambda, EventBridge |
| Payload | Original event only | Full context (request + response + error) |
| Configuration | On function | On function (per invocation type) |
| Use with | Async invocations, SQS trigger (source DLQ) | Async invocations only |

**When to use DLQ:**
- SQS-triggered Lambda: configure DLQ on the SQS queue (not the function) for messages that exceed maxReceiveCount
- Simple poison-pill capture with minimal setup

**When to use Destinations:**
- Async invocations where you need success/failure routing
- When you need error context for debugging
- Event-driven workflows (chain Lambdas via EventBridge)
- Preferred over DLQ for async Lambda invocations (AWS recommendation)

**Both can coexist**, but destinations take precedence for async invocations.

---

## 43 What is Lambda@Edge vs CloudFront Functions? What are their constraints?

Answer:

Both run code at the edge in response to CloudFront events, but differ significantly:

| Feature | CloudFront Functions | Lambda@Edge |
|---------|---------------------|-------------|
| Runtime | JavaScript only (ECMAScript 5.1) | Node.js, Python |
| Execution location | 400+ CloudFront edge locations | ~13 Regional Edge Caches |
| Max execution time | < 1 ms | 5s (viewer) / 30s (origin) |
| Max memory | 2 MB | 128–3,008 MB |
| Max package size | 10 KB | 1 MB (viewer) / 50 MB (origin) |
| Network access | No | Yes |
| File system access | No | No |
| Request body access | No | Yes (origin events) |
| Triggers | Viewer Request, Viewer Response only | Viewer Request, Viewer Response, Origin Request, Origin Response |
| Pricing | ~1/6th the cost of Lambda@Edge | Higher |
| Scale | Millions of requests/sec | Thousands of requests/sec |

**CloudFront Functions — use for:**
- URL rewrites/redirects
- Header manipulation (add/modify/delete)
- Cache key normalization
- Simple A/B testing (cookie-based routing)
- JWT/token validation (lightweight)
- Request/response header manipulation

**Lambda@Edge — use for:**
- Dynamic content generation at edge
- Complex authentication/authorization
- Image resizing/transformation
- SEO optimization (server-side rendering)
- Origin selection/failover logic
- Accessing external services (network calls)
- Modifying request body

**Constraints shared:**
- No VPC access
- No environment variables (Lambda@Edge)
- Must be deployed in us-east-1 (Lambda@Edge)
- No DLQ or destinations
- Immutable versions only (no $LATEST for Lambda@Edge)

---

## 44 How do you manage secrets and environment variables securely in Lambda?

Answer:

**1. Environment Variables (basic):**
- Key-value pairs set in function configuration
- Encrypted at rest with AWS KMS (default service key or custom CMK)
- Visible in console/API — not suitable for highly sensitive secrets
- Use for: non-sensitive config (feature flags, stage name, table names)

**2. AWS Secrets Manager (recommended for secrets):**
- Stores secrets (DB credentials, API keys, tokens) with automatic rotation
- Lambda fetches at runtime via SDK: `secretsmanager.get_secret_value()`
- Cache secrets in memory (use AWS Secrets Manager caching library) to avoid API calls on every invocation
- Supports cross-account access via resource policies
- Cost: $0.40/secret/month + $0.05/10,000 API calls

**3. AWS Systems Manager Parameter Store:**
- Free tier: Standard parameters (up to 10,000, 4 KB each)
- SecureString parameters encrypted with KMS
- Hierarchical naming: `/app/prod/db-password`
- Use AWS Parameters and Secrets Lambda Extension (caches values, reduces API calls)
- Use for: configuration + secrets when rotation isn't needed

**4. Lambda Extensions for caching:**
- AWS Parameters and Secrets Lambda Extension — sidecar that caches SSM/Secrets Manager values
- Reduces latency and API costs
- TTL-based cache refresh

**5. IAM best practices:**
- Grant least-privilege IAM role to Lambda (only `secretsmanager:GetSecretValue` for specific ARNs)
- Use resource-based policies on secrets
- Enable KMS key policies to restrict decryption

**6. What NOT to do:**
- ❌ Hardcode secrets in code
- ❌ Store secrets in environment variables in plaintext without KMS
- ❌ Commit secrets to source control
- ❌ Log secrets (mask in CloudWatch)

---

## 45 What is the difference between SQS Standard and FIFO queues? When do you choose FIFO despite the throughput limit?

Answer:

| Feature | SQS Standard | SQS FIFO |
|---------|-------------|----------|
| Ordering | Best-effort ordering (no guarantee) | Strict first-in-first-out ordering |
| Delivery | At-least-once (possible duplicates) | Exactly-once processing (deduplication) |
| Throughput | Virtually unlimited | 300 msg/sec (without batching) or 3,000 msg/sec (with batching); high-throughput mode: 30,000 msg/sec per API action |
| Deduplication | Not built-in | Content-based or MessageDeduplicationId (5-min window) |
| Message groups | Not applicable | MessageGroupId enables parallel processing while maintaining order within each group |
| Queue name | Any name | Must end with `.fifo` suffix |
| Cost | Lower | ~25% more expensive |

**Choose FIFO when:**
1. **Order matters**: Financial transactions, event sourcing, command processing where sequence is critical
2. **Exactly-once processing required**: Payment processing, inventory updates — duplicates cause real harm
3. **Regulatory/compliance**: Audit trails that must reflect exact order of operations
4. **State machines**: Events that build on previous state (e.g., order status: placed → paid → shipped)
5. **Message grouping**: Need parallel processing across groups but strict order within each group (e.g., per-customer order processing)

**Choose Standard when:**
- Throughput is the priority (millions of messages/sec)
- Application is idempotent (handles duplicates gracefully)
- Order doesn't matter (e.g., image thumbnailing, email sending, log processing)
- Fan-out workloads with independent messages

**FIFO throughput workaround:**
- Use multiple MessageGroupIds to parallelize (each group is an independent ordered stream)
- Enable high-throughput mode (up to 30,000 TPS with batching)
- Partition workload across multiple FIFO queues if needed

---

**Q46. Explain SQS visibility timeout, message retention, and dead letter queues.**

**Visibility Timeout:**
When a consumer picks up a message, SQS hides it from other consumers for a configurable duration (default 30s, max 12 hours). If the consumer processes and deletes it within that window, it's gone. If not (e.g., consumer crashes), the message becomes visible again and can be reprocessed. You should set the visibility timeout slightly longer than your expected processing time.

**Message Retention:**
SQS retains messages for 1 minute to 14 days (default 4 days). After that, messages are automatically deleted whether consumed or not.

**Dead Letter Queue (DLQ):**
A separate SQS queue where messages are sent after exceeding the `maxReceiveCount` (i.e., failed processing N times). DLQs help isolate poison-pill messages for debugging without blocking the main queue. You configure a redrive policy on the source queue pointing to the DLQ.

---

**Q47. What is SNS fan-out pattern? Walk through an architecture using SNS + multiple SQS subscribers.**

**Fan-out** means publishing one message to SNS and having it delivered simultaneously to multiple subscribers.

**Architecture:**
1. Producer publishes a single message to an SNS topic (e.g., `order-placed`).
2. SNS topic has 3 SQS queue subscriptions:
   - `inventory-queue` → Inventory service decrements stock
   - `email-queue` → Notification service sends confirmation email
   - `analytics-queue` → Analytics service logs the event
3. Each SQS queue decouples the downstream service, allowing independent scaling and retry logic.

**Benefits:** Decoupling, parallel processing, each subscriber can fail independently without affecting others.

---

**Q48. How does SQS long polling differ from short polling? Why is long polling preferred?**

| | Short Polling | Long Polling |
|---|---|---|
| Behavior | Returns immediately, even if queue is empty | Waits up to 20s for a message to arrive |
| Empty responses | Frequent | Rare |
| Cost | Higher (more API calls) | Lower |
| Latency | Slightly lower on busy queues | Negligible difference |

**Why long polling is preferred:**
- Reduces the number of empty `ReceiveMessage` API calls → lower cost
- Reduces CPU/network overhead on the consumer side
- Messages are returned as soon as they arrive within the wait window

Enable via `WaitTimeSeconds=20` on the `ReceiveMessage` call or at the queue level.

---

**Q49. When would you choose SQS vs SNS vs EventBridge?**

| Service | Best For |
|---|---|
| **SQS** | Decoupled async processing, task queues, load leveling, exactly-once or at-least-once delivery to a single consumer group |
| **SNS** | Fan-out to multiple subscribers (SQS, Lambda, HTTP), push-based pub/sub, simple topic-based routing |
| **EventBridge** | Event-driven architectures, routing events based on content/rules, SaaS integrations, scheduled events (cron), cross-account/cross-region event buses |

**Rule of thumb:**
- Point-to-point queue → SQS
- Broadcast to many → SNS
- Complex routing / event-driven microservices / SaaS → EventBridge

---

**Q50. What is Amazon MQ? When would you use it over SQS/SNS?**

Amazon MQ is a managed message broker service supporting **Apache ActiveMQ** and **RabbitMQ** protocols (AMQP, MQTT, STOMP, OpenWire, WebSocket).

**Use Amazon MQ when:**
- Migrating an on-premises application that already uses JMS, AMQP, MQTT, or STOMP — you want a lift-and-shift without rewriting messaging code.
- Your application requires protocol-level compatibility with standard broker APIs.

**Use SQS/SNS when:**
- Building cloud-native applications from scratch.
- You need massive scale, serverless operation, and tight AWS integration.
- No legacy protocol requirements.

**Key difference:** SQS/SNS are AWS-proprietary, infinitely scalable, and serverless. Amazon MQ is protocol-compatible but requires broker instance sizing and management.

---

**Q51. How does DynamoDB partition key design affect performance? What is a hot partition?**

DynamoDB distributes data across partitions based on the partition key hash. Each partition supports up to **3,000 RCUs** and **1,000 WCUs**.

**Good partition key design:**
- High cardinality (many distinct values) — e.g., `userId`, `orderId`
- Evenly distributes reads and writes across partitions

**Hot Partition:**
Occurs when too many requests target the same partition key value (e.g., using `date` as a partition key means all today's writes go to one partition). This causes throttling even if overall table capacity is sufficient.

**Mitigations:**
- Use high-cardinality keys
- Add a random suffix/prefix to spread writes (write sharding)
- Use composite keys to distribute load

---

**Q52. Explain DynamoDB read consistency — eventually consistent vs strongly consistent reads.**

**Eventually Consistent Reads (default):**
- DynamoDB returns data from any of the replica nodes.
- Data may be slightly stale (replication lag of typically under a second).
- Costs **0.5 RCU per 4KB**.
- Best for most read-heavy workloads where slight staleness is acceptable.

**Strongly Consistent Reads:**
- DynamoDB reads from the leader node, guaranteeing the most up-to-date data.
- Costs **1 RCU per 4KB** (double the cost).
- Use when your application requires read-after-write consistency (e.g., financial balances, inventory counts).
- Not available on Global Secondary Indexes (GSIs).

---

**Q53. What is DynamoDB DAX and when would you use it?**

**DAX (DynamoDB Accelerator)** is a fully managed, in-memory cache for DynamoDB that delivers microsecond read latency (vs single-digit milliseconds for DynamoDB).

**How it works:**
- DAX sits in front of DynamoDB as a write-through cache.
- Reads are served from cache; on a cache miss, DAX fetches from DynamoDB and caches the result.
- API-compatible — minimal code changes needed.

**Use DAX when:**
- Read-heavy workloads with repeated reads of the same items (e.g., product catalog, leaderboards)
- Microsecond latency is required
- You want to reduce RCU consumption and cost

**Don't use DAX when:**
- Write-heavy workloads (DAX doesn't cache writes in a way that reduces WCUs)
- Strongly consistent reads are required (DAX only supports eventually consistent)
- Data changes frequently and cache hit rate would be low

---

**Q54. Explain DynamoDB Streams. What use cases does it enable?**

**DynamoDB Streams** captures a time-ordered sequence of item-level changes (INSERT, MODIFY, REMOVE) in a table. Records are retained for 24 hours.

**Stream view types:**
- `KEYS_ONLY` — only key attributes
- `NEW_IMAGE` — entire item after change
- `OLD_IMAGE` — entire item before change
- `NEW_AND_OLD_IMAGES` — both before and after

**Use cases:**
- **Change Data Capture (CDC):** Trigger downstream processing on data changes
- **Lambda triggers:** Invoke Lambda on every write for real-time processing
- **Cross-region replication:** Foundation for DynamoDB Global Tables
- **Audit logging:** Record all changes to items
- **Search indexing:** Sync changes to Elasticsearch/OpenSearch
- **Event sourcing:** Rebuild state from the stream of changes

---

**Q55. What is the difference between DynamoDB on-demand and provisioned capacity modes?**

| | On-Demand | Provisioned |
|---|---|---|
| Capacity | Auto-scales instantly | You set RCU/WCU manually (or with Auto Scaling) |
| Pricing | Pay per request | Pay for provisioned capacity regardless of usage |
| Best for | Unpredictable/spiky traffic, new tables | Predictable, steady workloads |
| Cost at scale | More expensive at high, consistent throughput | More cost-efficient at steady load |
| Throttling | No throttling (within account limits) | Throttled if you exceed provisioned capacity |

**Rule of thumb:** Start with on-demand for new applications. Switch to provisioned once traffic patterns are understood to optimize cost.

---

**Q56. How do you model a one-to-many relationship in DynamoDB? (single-table design)**

In single-table design, you store multiple entity types in one table using a generic `PK` and `SK` (sort key) pattern.

**Example: Customer → Orders**

| PK | SK | Attributes |
|---|---|---|
| `CUSTOMER#123` | `METADATA` | name, email |
| `CUSTOMER#123` | `ORDER#2024-001` | total, status |
| `CUSTOMER#123` | `ORDER#2024-002` | total, status |

**Access patterns:**
- Get customer: `PK = CUSTOMER#123, SK = METADATA`
- Get all orders for customer: `PK = CUSTOMER#123, SK begins_with ORDER#`
- Get specific order: `PK = CUSTOMER#123, SK = ORDER#2024-001`

**Key principle:** Design your keys around your access patterns, not your entity relationships. Use `begins_with`, `between`, and `SK` range queries to fetch related items efficiently in a single query.

---

**Q57. What are Global Secondary Indexes vs Local Secondary Indexes? Trade-offs?**

**Local Secondary Index (LSI):**
- Same partition key as the base table, different sort key
- Must be created at table creation time (cannot add later)
- Shares the partition's read/write capacity with the base table
- Supports strongly consistent reads
- Max 5 per table
- Max 10GB per partition key value

**Global Secondary Index (GSI):**
- Different partition key and/or sort key from the base table
- Can be added or deleted at any time
- Has its own separate provisioned/on-demand capacity
- Only eventually consistent reads
- Max 20 per table (default)

**Trade-offs:**

| | LSI | GSI |
|---|---|---|
| Flexibility | Low (fixed at creation) | High (add anytime) |
| Consistency | Strong or eventual | Eventual only |
| Capacity | Shared with table | Independent |
| Use case | Alternate sort on same partition | Entirely new access pattern |

**Prefer GSIs** in most cases due to flexibility. Use LSIs only when you need strongly consistent alternate sort queries within a partition.

---

**Q58. What is the difference between RDS Multi-AZ and Read Replicas?**

**Multi-AZ:**
- Primary purpose: **High availability and failover**
- Synchronous replication to a standby instance in another AZ
- Standby is not readable — it's purely for failover
- Automatic failover in ~1–2 minutes if primary fails
- No performance benefit for reads

**Read Replicas:**
- Primary purpose: **Read scaling**
- Asynchronous replication to one or more replica instances
- Replicas are readable — offload read traffic from primary
- Can be in same AZ, different AZ, or different region (cross-region replicas)
- Can be promoted to standalone DB (useful for DR)
- Up to 5 replicas for MySQL/PostgreSQL/MariaDB; 15 for Aurora

**Summary:** Multi-AZ = HA/DR. Read Replicas = read scalability. You can and should use both together for production workloads.

---

**Q59. How does Aurora differ from standard RDS? Explain Aurora's shared storage architecture.**

**Aurora** is AWS's cloud-native relational database, compatible with MySQL and PostgreSQL but rebuilt from the ground up for the cloud.

**Key differences from standard RDS:**

| | Standard RDS | Aurora |
|---|---|---|
| Storage | EBS volume per instance | Shared distributed storage |
| Replication | Block-level replication | Storage-level replication |
| Failover time | ~1–2 min | ~30 seconds |
| Read replicas | Up to 5 | Up to 15 |
| Storage scaling | Manual | Auto-grows in 10GB increments up to 128TB |
| Performance | Baseline | Up to 5x MySQL, 3x PostgreSQL |

**Aurora Shared Storage Architecture:**
- Storage is decoupled from compute. All DB instances (writer + readers) share the same distributed storage layer.
- Data is replicated **6 ways across 3 AZs** (2 copies per AZ) automatically.
- Only **redo logs** are written to storage (not full data pages), reducing write amplification.
- A read replica can be promoted to writer instantly since it already has access to the same storage — no data copy needed.
- Quorum-based reads/writes: tolerates loss of 2 copies for writes, 3 copies for reads.

---

**Q60. What is Aurora Serverless v2? How does it differ from provisioned Aurora?**

**Aurora Serverless v2** automatically scales Aurora capacity up and down based on actual workload demand, measured in **Aurora Capacity Units (ACUs)**.

**How it works:**
- You set a min and max ACU range (e.g., 0.5 to 128 ACUs).
- Aurora scales in fine-grained increments (as small as 0.5 ACU) within seconds.
- You pay per ACU-second consumed, not for idle capacity.

**Differences from Provisioned Aurora:**

| | Provisioned Aurora | Aurora Serverless v2 |
|---|---|---|
| Capacity | Fixed instance size (e.g., r6g.large) | Dynamic ACU range |
| Scaling | Manual or slow Auto Scaling | Near-instant, fine-grained |
| Cost model | Per instance-hour | Per ACU-second |
| Best for | Steady, predictable workloads | Variable, spiky, or dev/test workloads |
| Multi-AZ | Yes | Yes |
| Read replicas | Yes | Yes (can mix with provisioned) |
| Scale to zero | No | Yes (min 0 ACU — pauses when idle) |

**When to use Serverless v2:**
- Unpredictable or bursty traffic (e.g., SaaS multi-tenant apps)
- Dev/test environments (scale to zero when not in use)
- Applications with infrequent but sudden spikes
- When you want to avoid over-provisioning costs
