# Database Normalization — Comprehensive Architect Notes

## 1. What is Normalization?

Normalization is the process of organizing data in a relational database to:
- **Eliminate redundancy** (duplicate data)
- **Eliminate anomalies** (insert, update, delete anomalies)
- **Ensure data integrity** through logical data dependencies

It was first proposed by **Edgar F. Codd** (1970) as part of the relational model.

---

## 2. Why Normalize?

### Problems Without Normalization (Anomalies)

Consider this **unnormalized** table:

| OrderID | Customer | CustomerCity | Product  | Price | Qty |
|---------|----------|--------------|----------|-------|-----|
| 1       | Alice    | New York     | Laptop   | 1000  | 1   |
| 1       | Alice    | New York     | Mouse    | 25    | 2   |
| 2       | Bob      | Chicago      | Laptop   | 1000  | 1   |
| 3       | Alice    | New York     | Keyboard | 75    | 1   |

**Insert Anomaly:** Can't add a new customer without an order.
**Update Anomaly:** If Alice moves cities, we must update *every* row — miss one and data is inconsistent.
**Delete Anomaly:** If we delete Bob's only order, we lose Bob's information entirely.

---

## 3. Normal Forms

### 3.1 First Normal Form (1NF)

**Rule:** Every column must contain **atomic (indivisible) values**, and each row must be unique.

❌ **Violates 1NF:**

| StudentID | Name  | Phones              |
|-----------|-------|---------------------|
| 1         | Alice | 111-1111, 222-2222  |
| 2         | Bob   | 333-3333            |

*Problem:* `Phones` column has multiple values in a single cell.

✅ **1NF Compliant:**

| StudentID | Name  | Phone    |
|-----------|-------|----------|
| 1         | Alice | 111-1111 |
| 1         | Alice | 222-2222 |
| 2         | Bob   | 333-3333 |

**Key takeaway:** No repeating groups, no multi-valued columns.

---

### 3.2 Second Normal Form (2NF)

**Rule:** Must be in 1NF + **no partial dependency** (every non-key column must depend on the *entire* primary key, not just part of it).

*Only relevant when the primary key is composite (multi-column).*

❌ **Violates 2NF:**

Table: `OrderItems` — PK = (OrderID, ProductID)

| OrderID | ProductID | ProductName | Quantity |
|---------|-----------|-------------|----------|
| 1       | 101       | Laptop      | 2        |
| 1       | 102       | Mouse       | 5        |
| 2       | 101       | Laptop      | 1        |

*Problem:* `ProductName` depends only on `ProductID`, not on the full key (OrderID, ProductID). This is a **partial dependency**.

✅ **2NF Compliant — Split into two tables:**

**OrderItems** — PK = (OrderID, ProductID)

| OrderID | ProductID | Quantity |
|---------|-----------|----------|
| 1       | 101       | 2        |
| 1       | 102       | 5        |
| 2       | 101       | 1        |

**Products** — PK = ProductID

| ProductID | ProductName |
|-----------|-------------|
| 101       | Laptop      |
| 102       | Mouse       |

**Key takeaway:** Every non-key attribute must depend on the *whole* key.

---

### 3.3 Third Normal Form (3NF)

**Rule:** Must be in 2NF + **no transitive dependency** (non-key columns must not depend on other non-key columns).

❌ **Violates 3NF:**

Table: `Employees` — PK = EmpID

| EmpID | EmpName | DeptID | DeptName    |
|-------|---------|--------|-------------|
| 1     | Alice   | D1     | Engineering |
| 2     | Bob     | D2     | Marketing   |
| 3     | Carol   | D1     | Engineering |

*Problem:* `DeptName` depends on `DeptID`, not directly on `EmpID`.
Chain: `EmpID → DeptID → DeptName` (transitive dependency).

✅ **3NF Compliant:**

**Employees**

| EmpID | EmpName | DeptID |
|-------|---------|--------|
| 1     | Alice   | D1     |
| 2     | Bob     | D2     |
| 3     | Carol   | D1     |

**Departments**

| DeptID | DeptName    |
|--------|-------------|
| D1     | Engineering |
| D2     | Marketing   |

**Key takeaway:** Non-key → Non-key dependencies must be eliminated.

> **Codd's memorable rule for 3NF:**
> *"Every non-key attribute must provide a fact about the key, the whole key, and nothing but the key — so help me Codd."*

---

### 3.4 Boyce-Codd Normal Form (BCNF / 3.5NF)

**Rule:** Must be in 3NF + **every determinant must be a candidate key**.

BCNF is a stricter version of 3NF. It handles edge cases where 3NF still allows anomalies.

❌ **Violates BCNF:**

Table: `CourseInstructors` — Candidate keys: (Student, Course) and (Student, Instructor)

| Student | Course  | Instructor |
|---------|---------|------------|
| Alice   | Math    | Dr. Smith  |
| Bob     | Math    | Dr. Jones  |
| Alice   | Physics | Dr. Jones  |

*Constraint:* Each instructor teaches only one course, but a course can have multiple instructors.

So `Instructor → Course` is a functional dependency, but `Instructor` is **not** a candidate key. Violates BCNF.

✅ **BCNF Compliant:**

**Instructors**

| Instructor | Course  |
|------------|---------|
| Dr. Smith  | Math    |
| Dr. Jones  | Math    |

**StudentInstructors**

| Student | Instructor |
|---------|------------|
| Alice   | Dr. Smith  |
| Bob     | Dr. Jones  |
| Alice   | Dr. Jones  |

---

### 3.5 Fourth Normal Form (4NF)

**Rule:** Must be in BCNF + **no multi-valued dependencies**.

A multi-valued dependency exists when one attribute determines a *set* of values of another attribute, independently.

❌ **Violates 4NF:**

| EmpID | Skill   | Language |
|-------|---------|----------|
| 1     | Java    | English  |
| 1     | Java    | French   |
| 1     | Python  | English  |
| 1     | Python  | French   |

*Problem:* Skills and Languages are independent of each other but both depend on EmpID. This creates a cartesian product explosion.

✅ **4NF Compliant:**

**EmpSkills**

| EmpID | Skill  |
|-------|--------|
| 1     | Java   |
| 1     | Python |

**EmpLanguages**

| EmpID | Language |
|-------|----------|
| 1     | English  |
| 1     | French   |

---

### 3.6 Fifth Normal Form (5NF / Project-Join Normal Form)

**Rule:** Must be in 4NF + **no join dependency** that isn't implied by candidate keys.

A table is in 5NF if it cannot be decomposed into smaller tables without losing data (lossless join).

This is rare in practice and applies when a table has complex multi-way relationships that can only be reconstructed by joining three or more tables.

---

## 4. Quick Reference Summary

| Normal Form | Requirement                                      | Eliminates                |
|-------------|--------------------------------------------------|---------------------------|
| **1NF**     | Atomic values, unique rows                       | Repeating groups          |
| **2NF**     | 1NF + No partial dependencies                   | Partial dependencies      |
| **3NF**     | 2NF + No transitive dependencies                | Transitive dependencies   |
| **BCNF**    | 3NF + Every determinant is a candidate key       | Non-key determinants      |
| **4NF**     | BCNF + No multi-valued dependencies             | Multi-valued dependencies |
| **5NF**     | 4NF + No join dependencies beyond candidate keys | Join dependencies         |

---

## 5. Functional Dependency Notation

```
EmpID → EmpName, DeptID       (EmpID determines EmpName and DeptID)
DeptID → DeptName              (DeptID determines DeptName)
EmpID → DeptName               (Transitive: via DeptID)
```

**Types:**
- **Full FD:** A → B where B depends on ALL of A
- **Partial FD:** (A, B) → C but actually A → C alone (violates 2NF)
- **Transitive FD:** A → B → C (violates 3NF)

---

## 6. Denormalization — The Trade-off

In practice, **3NF or BCNF** is the target for most OLTP systems.

However, architects intentionally **denormalize** for:
- **Read-heavy workloads** (fewer JOINs = faster queries)
- **Data warehouses / OLAP** (star schema, snowflake schema)
- **Caching layers** (materialized views, precomputed aggregates)
- **Microservices** (each service owns its data, may duplicate across boundaries)

### When to Denormalize

| Scenario                  | Normalize | Denormalize |
|---------------------------|-----------|-------------|
| OLTP / Transactional      | ✅        |             |
| OLAP / Analytics          |           | ✅          |
| Write-heavy               | ✅        |             |
| Read-heavy dashboards     |           | ✅          |
| Data integrity is critical| ✅        |             |
| Low-latency reads needed  |           | ✅          |

---

## 7. Architect's Decision Framework

```
Step 1: Start with an unnormalized dataset
Step 2: Apply 1NF → 2NF → 3NF (minimum standard)
Step 3: Evaluate if BCNF is needed (complex key structures)
Step 4: Profile query patterns
Step 5: Selectively denormalize hot paths with:
         - Materialized views
         - Caching (Redis, ElastiCache)
         - Read replicas
         - CQRS pattern (separate read/write models)
Step 6: Document every denormalization decision with justification
```

---

## 8. Real-World Example: E-Commerce

### Unnormalized

| OrderID | Date       | CustName | CustEmail       | Product | Category    | Price | Qty |
|---------|------------|----------|-----------------|---------|-------------|-------|-----|
| 1       | 2024-01-01 | Alice    | alice@mail.com  | Laptop  | Electronics | 1000  | 1   |
| 1       | 2024-01-01 | Alice    | alice@mail.com  | Mouse   | Accessories | 25    | 2   |
| 2       | 2024-01-02 | Bob      | bob@mail.com    | Laptop  | Electronics | 1000  | 1   |

### After 3NF

**Customers**
| CustomerID | Name  | Email          |
|------------|-------|----------------|
| C1         | Alice | alice@mail.com |
| C2         | Bob   | bob@mail.com   |

**Categories**
| CategoryID | CategoryName |
|------------|--------------|
| CAT1       | Electronics  |
| CAT2       | Accessories  |

**Products**
| ProductID | ProductName | CategoryID | Price |
|-----------|-------------|------------|-------|
| P1        | Laptop      | CAT1       | 1000  |
| P2        | Mouse       | CAT2       | 25    |

**Orders**
| OrderID | OrderDate  | CustomerID |
|---------|------------|------------|
| 1       | 2024-01-01 | C1         |
| 2       | 2024-01-02 | C2         |

**OrderItems**
| OrderID | ProductID | Quantity |
|---------|-----------|----------|
| 1       | P1        | 1        |
| 1       | P2        | 2        |
| 2       | P1        | 1        |

✅ No redundancy. No anomalies. Each fact stored once.

---

## 9. Key Takeaways for Architects

1. **Normalize for correctness first**, denormalize for performance later.
2. **3NF is the practical sweet spot** for most transactional systems.
3. **Every denormalization is technical debt** — document it.
4. **Normalization is not just theory** — it directly prevents real bugs (stale data, inconsistent updates).
5. **CQRS pattern** lets you have both: normalized writes + denormalized reads.
