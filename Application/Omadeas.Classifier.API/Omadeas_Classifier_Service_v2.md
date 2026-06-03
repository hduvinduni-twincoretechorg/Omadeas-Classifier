# Omadeas Classifier Service — First Principles
### Single source of truth · v2.0 · April 2026

> **v2.0 change**: Classifiers are now a **central platform service**, not a per-module framework.
> Modules consume the central service for definitions; assignment and history tables remain
> module-local. See §0 below for the architectural shift and §11 for adoption guidance.

---

## 0. THE ARCHITECTURAL SHIFT (v1 → v2)

### What changed

In v1, every module implemented the classifier framework with its own prefix:
`projectops_classifier`, `freeflow_classifier`, `peopleops_classifier`. The framework
guaranteed they were structurally identical, but each module ran its own copy of the data.

**v2 splits this in two:**

- **Definition layer + Profile layer** → centralised in a new **classifier service**.
  Owned by one team, one set of tables, one admin UI. The platform has *one* PRIORITY
  classifier, not N copies.

- **Assignment layer + Profile attachment** → remain **per-module**. They join to
  each module's local `node` table, so they cannot centralise. Each module keeps
  its own `<module>_node_classifier_assignment`, `<module>_node_classifier_assignment_history`,
  and `<module>_node_classifier_profile_attachment`.

### Why this is the right boundary

**Definition is platform-wide, assignment is module-local.** A PRIORITY classifier
shouldn't exist N times across modules — but a ProjectOps task's priority assignment
is meaningful only to ProjectOps. The split is along this natural seam.

The change unlocks three things the v1 framework couldn't deliver:

1. **Cross-module knowledge management.** Records across ProjectOps, Freeflow, and
   future modules can be queried by classifier value. "All work platform-wide tagged
   Strategic Theme = Digital Transformation" becomes possible.

2. **Cross-module profiles.** A tenant's "Strategic Planning Profile" can bundle
   classifiers used by ProjectOps projects AND Freeflow canvases. With per-module
   classifier tables this wasn't expressible.

3. **One admin UX.** Tenants manage classifiers in one place, not across N module
   admin screens.

### What stays the same from v1

- The 10 entity shapes (now: 7 central + 3 per-module)
- The three selection modes (SINGLE / MULTIPLE / HIERARCHICAL)
- The four constraint types (REQUIRES / PROHIBITS / WARNS, with IMPLIES rejected)
- Policy D for history writes (modal-close, session-grouped)
- The 17 relationships and their cardinalities
- Soft-retire behaviour (no hard deletes for definition entities)

### What is new in v2

- **Soft references** across the service boundary — assignment tables reference
  classifier and value IDs that live in a different service. No cross-DB FK is
  possible; integrity is application-validated.
- **Read replicas / cache** in each consuming module so the hot read path stays fast.
- **Change propagation** via the classifier service publishing definition changes
  for consumers to react to.
- **Consumer manifest** — each module declares which classifiers it uses, so the
  classifier service knows who to notify when definitions change.

### A second service boundary — the Calculation Engine

Computed and suggested classifier values are **not produced by the classifier
service itself**. They are produced by a separate **Calculation Engine** (Calc
Engine) — another central platform service that owns calculation definitions and
their rules.

The classifier service stores the *fact* that a classifier value was computed,
plus soft references to the calculation that produced it. It does not store
calculation definitions.

This second split exists because calculations are useful well beyond classifiers
— they can drive custom fields, notifications, reports, anything that needs a
derived value. Coupling them to the classifier service would cap their reach.
See §16 for the Calc Engine interface contract.

---

## WHY THIS DOCUMENT EXISTS

The Omadeas platform has multiple modules — ProjectOps, Freeflow, and others to follow — and each needs to label its data with structured, controlled vocabularies. Without a shared model, each module builds its own; the platform fragments; users see inconsistent UI; cross-module reporting becomes impossible.

This document defines the classifier service **once**. The classifier service owns
definitions; modules consume them and own their own assignment data.

If you are building a new Omadeas module and need to tag your data with structured
labels, you read this document, you connect to the classifier service, and you
build the three per-module entities (assignment, assignment_history, profile_attachment).

---

## 1. FIRST PRINCIPLES

These are the rules the framework is built on. Everything downstream follows from them.

### 1.1 A classifier is a controlled vocabulary

A classifier (Priority, RAG Status, Effort Size, Strategic Theme) is a **named list of allowed values** that can be attached to something. It is *not* a free-text field. The value list is curated.

### 1.2 Three layers, kept distinct

| Layer | What it answers | Who edits it |
|---|---|---|
| Definition | "What classifiers exist? What values does each have? What rules apply?" | Admins (rarely) |
| Profile | "Which classifiers are in scope for which corner of the data?" | Project owners (occasionally) |
| Assignment | "Which value is on which record?" | All users (constantly) |

Mixing these layers up is the most common source of confusion. The definition is the dictionary; the profile is the curriculum; the assignment is the actual writing.

### 1.3 Reading is cheap, writing is governed

Anyone with read access to a record can see its classifier assignments. Writing an assignment must pass policy checks (single vs multi, required, constraints). The framework's job is to make reads simple and writes safe.

### 1.4 One service, many consumers

Classifier definitions live in **one central service** — owned by one team, one
set of tables, one admin UI. Modules (ProjectOps, Freeflow, future modules) are
consumers. Cross-module reasoning becomes trivial because there is one PRIORITY,
not N copies.

### 1.5 Platform-seeded and tenant-defined coexist

Some classifiers (PRIORITY, RAG_STATUS) are universal and shipped by the platform. Some are tenant-specific (a tenant's custom Strategic Theme list). Both live in the same tables, distinguished by a `source` flag. Platform classifiers are read-only to tenants; tenant classifiers are owned by the tenant.

### 1.6 Profiles solve the scale problem

Without profiles, every classifier defined in a tenant would appear on every record. At 30+ classifiers per tenant (realistic), this is unusable. Profiles let a project owner curate "the relevant classifiers for this programme" once, and that scope applies to every descendant record automatically.

### 1.7 Assignments are simple rows, not history logs

An assignment is a (record, classifier_value) pair. Standard audit columns (created/updated by/at) provide the audit trail. **No temporal fields** (effective_from/to) — assignment history is YAGNI for v1. If it matters later, the migration is small.

### 1.8 Schema supports more than UI does

The framework reserves capability (HIERARCHICAL selection mode, constraint cross-classifier semantics) in the schema even when the UI doesn't surface it yet. This is so v2 features don't require schema migrations. Modules implementing the framework should hide-not-break unsupported features.

### 1.9 Cross-service boundaries use soft references

Assignment tables in consuming modules reference classifier and value IDs that
live in the classifier service. **These are soft references — application-validated,
not enforced by a database foreign key.** This is because cross-microservice FKs
aren't workable in practice. Integrity is enforced by the application layer plus
event-driven cache invalidation. See §11 for the consumer pattern.

### 1.10 The framework is for controlled-vocabulary metadata classifiers only

The classifier service is purpose-built for *labelling records with structured,
controlled vocabularies*. It is not a general-purpose data store. A concept
belongs in this framework if and only if **all three** are true:

1. **Controlled vocabulary** — values come from a fixed list, not free text
2. **Reusable across records** — more than one node could carry the same classifier
3. **Shared meaning** — every user understands a given value the same way (with the per-scope variability caveat in §1.11 below)

**Things that pass the test (in the framework):**
- PRIORITY, RAG_STATUS, COMPLEXITY, EFFORT_SIZE
- STRATEGIC_THEME, DOMAIN, FUNCTION, GEOGRAPHY
- RISK_PROBABILITY, RISK_IMPACT
- COMPLIANCE_CATEGORY, DELIVERY_PHASE

**Things that fail the test (and belong elsewhere):**
- **Workflow status** (DRAFT/IN_REVIEW/APPROVED) — engine-controlled lifecycle, not user labelling. Use `node_status` or equivalent state-machine entity.
- **Free-text tags** — no controlled vocabulary. Use a tagging entity if needed.
- **Numeric metrics** (effort hours, cost, headcount) — quantitative values, not categorical labels. Use custom-field tables.
- **Relationship labels** (assignee, owner, watcher) — these reference principals, not classifier values. Use dedicated junction tables.
- **Rich domain entities** (skills, capabilities, contracts) — entities in their own right with their own schemas, not classifier values.

This boundary is non-negotiable. If a concept feels like it might belong in the
framework but doesn't pass the three-part test, model it as a different entity.
Stuffing non-classifier concepts into the framework makes the schema work harder
and the UI inconsistent.

### 1.11 Two authoring tiers — platform and tenant

The framework supports two tiers of authoring authority:

| Tier | Owner | Visibility | Editability |
|---|---|---|---|
| **Platform** | Anthropic / platform team | All tenants | Read-only to tenants |
| **Tenant** | Tenant admin | Tenant-wide | Tenant admins edit |

**Tenant classifiers are managed centrally** by the tenant admin. A tenant's
business structure (business units, legal entities, divisions) typically
provides the natural lineage along which classifier proliferation is
controlled.

**For PM-level ad-hoc labelling** that doesn't warrant a full classifier
definition, the platform provides **tags** (a separate, lightweight mechanism).
Tags let project managers label individual records without polluting the
tenant classifier vocabulary. When a tagging pattern proves valuable enough,
the tenant admin can promote it to a first-class classifier.

**Profile-based scoping** (see §10) controls which classifiers actually apply
in a given project subtree. PMs don't author new classifiers per project;
they pick which existing tenant classifiers apply via profile attachment.

**Permissions for write at each tier:**
- Platform: only Anthropic / platform team
- Tenant: tenant admins

The application layer enforces write permissions. Read visibility is
controlled via profile attachment (§10), not via the classifier definition itself.

---

## 2. THE 11 ENTITIES AT A GLANCE — split across two ownership boundaries

```
╔══════════════════════════════════════════════════════════════════╗
║  CLASSIFIER SERVICE (centrally owned)                            ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  DEFINITION LAYER (what classifiers exist)                       ║
║  ┌─────────────────────────────────────┐                         ║
║  │  1. classifier                      │  ── platform/tenant     ║
║  │  2. classifier_value                │  ── allowed values      ║
║  │  3. classifier_value_policy         │  ── selection rules     ║
║  │  4. classifier_value_constraint     │  ── inter-value rules   ║
║  └─────────────────────────────────────┘                         ║
║                                                                  ║
║  PROFILE LAYER (bundles, cross-module capable)                   ║
║  ┌─────────────────────────────────────┐                         ║
║  │  5. classifier_profile              │  ── named bundles       ║
║  │  6. classifier_profile_member       │  ── junction            ║
║  └─────────────────────────────────────┘                         ║
║                                                                  ║
║  DEFINITION HISTORY LAYER (audit log for definition edits)       ║
║  ┌─────────────────────────────────────┐                         ║
║  │  7. classifier_history              │  ── definition audit    ║
║  │  8. classifier_profile_history      │  ── profile audit       ║
║  └─────────────────────────────────────┘                         ║
║                                                                  ║
║  LOOKUP                                                          ║
║  ┌─────────────────────────────────────┐                         ║
║  │  +  classifier_selection_mode       │  ── SINGLE/MULTI/HIER   ║
║  └─────────────────────────────────────┘                         ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
                              │
                              │ soft references (IDs only)
                              ▼
╔══════════════════════════════════════════════════════════════════╗
║  CONSUMING MODULE (e.g. ProjectOps, Freeflow)                    ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  ATTACHMENT (where profiles apply, with calculation overrides)   ║
║  ┌──────────────────────────────────────────┐                    ║
║  │  9. <module>_node_classifier_profile_    │  ── on module node ║
║  │         attachment                       │                    ║
║  └──────────────────────────────────────────┘                    ║
║                                                                  ║
║  ASSIGNMENT LAYER (actual labelling)                             ║
║  ┌──────────────────────────────────────────┐                    ║
║  │ 10. <module>_node_classifier_assignment  │  ── current state  ║
║  │ 11. <module>_node_classifier_assignment_ │  ── history log    ║
║  │         history                          │                    ║
║  └──────────────────────────────────────────┘                    ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝

                              ┊
                              ┊ soft references (where applicable)
                              ▼

╔══════════════════════════════════════════════════════════════════╗
║  CALCULATION ENGINE (separate central service — see §16)         ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Owns: calculation definitions, calculation rules,               ║
║         calculation evaluation, calculation change events.       ║
║                                                                  ║
║  Referenced by (soft FK):                                        ║
║    - classifier_value_policy.default_calculation_id              ║
║    - <module>_node_classifier_profile_attachment.                ║
║                       calculation_overrides (JSONB)              ║
║    - <module>_node_classifier_assignment.calculation_id          ║
║    - <module>_node_classifier_assignment.calculation_rule_id     ║
║    - <module>_node_classifier_assignment_history.                ║
║                       calculation_id / calculation_rule_id       ║
║                                                                  ║
║  Schema NOT owned by the classifier service. The classifier      ║
║  service stores REFERENCES to calculations and the inputs        ║
║  observed at evaluation time, but the calculation definitions    ║
║  themselves live in the Calc Engine.                             ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
```

**7 central entities + 1 lookup live in the classifier service.**
**3 entities per module live in each consuming module.**

The split follows the natural seam between *definitions* (platform-wide) and
*assignments* (module-local — they join to the module's `node` table).

The Calculation Engine is a separate service (see §16) referenced by soft FKs
where classifier values are produced by calculations rather than user selection.
The classifier service does not define or evaluate calculations.

---

## 3. ENTITY RELATIONSHIP DIAGRAM

```
                          ┌─────────────────────┐
                          │   classifier        │  ◄──── platform OR tenant
                          │  (e.g. PRIORITY)    │
                          └──────────┬──────────┘
                                     │ 1
                       ┌─────────────┼─────────────┬──────────────┐
                       │             │             │              │
                       │ 1:N         │ 1:1         │ 1:N          │ N:M
                       ▼             ▼             ▼              ▼
            ┌──────────────┐  ┌────────────┐  ┌─────────────┐  ┌──────────────┐
            │ classifier   │  │   value_   │  │   value_    │  │   profile_   │
            │   value      │  │   policy   │  │ constraint  │  │   member     │
            │  (HIGH, LOW) │  │ (single...)│  │  (REQUIRES) │  └──────┬───────┘
            └──────┬───────┘  └────────────┘  └─────────────┘         │
                   │                                                  │ N
                   │ 1                                                ▼
                   │                                       ┌────────────────────┐
                   │                                       │     profile        │
                   │                                       │  (Software Delv.)  │
                   │                                       └─────────┬──────────┘
                   │                                                 │ 1
                   │                                                 │
                   │                                                 │ N
                   │                                                 ▼
                   │                                       ┌─────────────────────┐
                   │                                       │  node_classifier_   │
                   │                                       │  profile_attachment │
                   │                                       │   (subtree scope)   │
                   │                                       └──────────┬──────────┘
                   │                                                  │ 1
                   │                                                  │
                   │                                                  ▼
                   │                                       ┌─────────────────────┐
                   │ N                                     │       node          │
                   ▼                                  ───► │                     │
            ┌──────────────────────────┐                   └──────────┬──────────┘
            │ node_classifier_         │ N : 1                        │
            │     assignment           │ ─────────────────────────────┘
            │  (the hot table)         │
            └──────────────────────────┘
```

The data flow read top-to-bottom: a classifier has values (and one policy and zero-or-more constraints). Profiles bundle classifiers. Profiles attach to nodes with subtree inheritance. Nodes carry value assignments.

**Ownership note**: the upper half of the diagram (`classifier`, `classifier_value`,
`value_policy`, `value_constraint`, `profile`, `profile_member`) lives in the
**classifier service**. The lower half (`node_classifier_profile_attachment`,
`node_classifier_assignment`, and the `node` itself) is **module-local**. The
dashed boundary between the two is where soft references replace hard FKs.

---

## 4. RELATIONSHIPS (the full set, with rationale)

The diagram in §3 shows the structure; this section explains *why* each relationship exists, how it behaves, and what rules govern it. The framework has **19 relationships** across the 11 entities + 1 lookup. They're grouped by layer below.

For each relationship: cardinality, direction (which entity holds the FK), lifecycle behaviour (what happens on delete or deactivation), and the design rationale.

---

### 4.1 Definition layer relationships

#### R1 — `classifier` ⟶ `classifier_value` (1:N)

- **FK direction**: `classifier_value.classifier_id → classifier.id`
- **Cardinality**: One classifier has many values. Every value belongs to exactly one classifier.
- **Lifecycle**: When a classifier is soft-retired (`is_active=false`), its values remain readable for historical assignments but cannot be assigned to new nodes. Classifiers are not hard-deleted in v1 — soft-retire only.
- **Why it exists**: Values are *scoped* to their classifier. "Red" only means something inside RAG_STATUS. A value cannot exist without a parent classifier.
- **Special case**: Values can also have a `parent_value_id` (self-FK) for HIERARCHICAL classifiers — see R3 below.

#### R2 — `classifier` ⟶ `classifier_value_policy` (1:1)

- **FK direction**: `classifier_value_policy.classifier_id → classifier.id` (UNIQUE constraint)
- **Cardinality**: Exactly one policy per classifier. The UNIQUE constraint enforces this.
- **Lifecycle**: Policy is created at the same time as the classifier. If the classifier is soft-retired, the policy is also soft-retired (`is_active=false`).
- **Why 1:1 separate, not columns on classifier**: Policy is a separate entity (rather than columns on classifier) because:
  - Policies have their own audit trail — you want to know when min/max/required changed independent of when the classifier name changed
  - Policy may grow new fields (we already added `default_value_id`, `allow_custom_values`, `requires_reason_on_change` after v1.0) without bloating the classifier row
  - Application code can pass around a `Policy` object distinct from a `Classifier` object — cleaner domain model

#### R3 — `classifier_value` ⟶ `classifier_value` (self-FK, 1:N)

- **FK direction**: `classifier_value.parent_value_id → classifier_value.id`
- **Cardinality**: A value can have one parent and many children. Flat classifiers have all `parent_value_id=NULL`.
- **Lifecycle**: When a parent value is soft-retired, its children are also visually retired in the UI but the rows remain for historical integrity. v2 may introduce reparenting rules.
- **Why it exists**: HIERARCHICAL classifiers (the third selection mode) need a tree structure for their values. Rather than introducing a separate entity for hierarchies, the self-FK keeps the model simple. Flat classifiers ignore the field.
- **Constraint to enforce**: A child's `classifier_id` must match its parent's `classifier_id`. You cannot cross classifier boundaries with parenting.

#### R4 — `classifier_value_policy` ⟶ `classifier_selection_mode` (N:1)

- **FK direction**: `classifier_value_policy.selection_mode_id → classifier_selection_mode.id`
- **Cardinality**: Many policies reference one selection mode. Three modes exist (SINGLE, MULTIPLE, HIERARCHICAL) and most policies use SINGLE.
- **Lifecycle**: Selection modes are platform-seeded and read-only. Policies cannot reference an inactive mode.
- **Why a FK not an enum**: The mode rows carry useful denormalised metadata (`allows_multiple` flag) and human-readable descriptions. Adding a new mode in v2 (e.g. PARTIAL_SELECT) is a row insert, not a CHECK constraint migration.

#### R5 — `classifier_value_policy` ⟶ `classifier_value` (N:1, optional)

- **FK direction**: `classifier_value_policy.default_value_id → classifier_value.id` (NULLable)
- **Cardinality**: A policy may reference zero or one default value. The default is shown as auto-selected when the classifier is first assigned.
- **Lifecycle**: If the referenced value is soft-retired, the policy's `default_value_id` should be cleared (set to NULL) by application logic.
- **Why it exists**: Enables sensible auto-defaults — PRIORITY defaults to MEDIUM, RAG defaults to GREEN — so new nodes start with reasonable assignments rather than nothing.
- **Constraint to enforce**: The referenced value's `classifier_id` must match the policy's `classifier_id`. The default of PRIORITY cannot be a RAG value.

#### R6 — `classifier_value_constraint` ⟶ `classifier` (N:1, source)

- **FK direction**: `classifier_value_constraint.source_classifier_id → classifier.id`
- **Cardinality**: A classifier can be the source of many constraints.
- **Why dual `source` and `target` FKs**: Constraints commonly *cross* classifiers (PRIORITY=CRITICAL REQUIRES RAG=RED). Naming both endpoints explicitly avoids ambiguity. A single `classifier_id` would force the constraint to live on just one side of the relationship, which doesn't work for cross-classifier rules.

#### R7 — `classifier_value_constraint` ⟶ `classifier` (N:1, target)

- **FK direction**: `classifier_value_constraint.target_classifier_id → classifier.id`
- **Cardinality**: A classifier can be the target of many constraints.
- **Symmetry note**: A single classifier can be both source of some constraints and target of others. PRIORITY might be the source for some rules and the target for others.

#### R8 — `classifier_value_constraint` ⟶ `classifier_value` (N:1, source value)

- **FK direction**: `classifier_value_constraint.source_value_id → classifier_value.id`
- **Cardinality**: A value can trigger many constraints when it's set.
- **Constraint to enforce**: `source_value_id` must belong to the value whose `classifier_id` matches the constraint's `source_classifier_id`. The constraint cannot reference a value from a different classifier than the one named in `source_classifier_id`.

#### R9 — `classifier_value_constraint` ⟶ `classifier_value` (N:1, target value)

- **FK direction**: `classifier_value_constraint.target_value_id → classifier_value.id`
- **Symmetric** with R8 on the target side.

---

### 4.2 Profile layer relationships

#### R10 — `classifier_profile` ⟶ `classifier_profile_member` (1:N)

- **FK direction**: `classifier_profile_member.profile_id → classifier_profile.id`
- **Cardinality**: One profile contains many members. Each member represents one classifier-in-profile.
- **Lifecycle**: When a profile is soft-retired, its member rows remain for audit but are excluded from scope resolution queries.
- **Why a junction entity, not a JSONB array on profile**: Members carry their own audit data (`is_required`, `order`, audit columns). A junction entity keeps the schema queryable — "show me all profiles that include the PRIORITY classifier" is a join, not a JSONB scan.

#### R11 — `classifier_profile_member` ⟶ `classifier` (N:1)

- **FK direction**: `classifier_profile_member.classifier_id → classifier.id`
- **Cardinality**: A classifier can be a member of many profiles. PRIORITY appears in nearly every profile.
- **Unique constraint**: `UNIQUE (profile_id, classifier_id)` — a classifier appears at most once in any given profile.
- **Why the same classifier in multiple profiles**: The same classifier can be optional in one profile (Standard Delivery — PRIORITY optional) and required in another (Risk Governance — PRIORITY required). `is_required` is on the member row, not the classifier row, precisely to support this.

#### R12 — `classifier_profile` ⟶ `node_classifier_profile_attachment` (1:N)

- **FK direction**: `node_classifier_profile_attachment.profile_id → classifier_profile.id`
- **Cardinality**: A profile can be attached to many nodes (typically programmes or projects).
- **Lifecycle**: When a profile is soft-retired, its attachments remain but the profile is excluded from scope resolution. Attachments can also be soft-retired independently (`is_active=false`).
- **Why this is a separate entity from `classifier_profile_member`**: Members define *what's in* a profile (definition-layer concern). Attachments define *where the profile applies* (operational concern). Different lifecycles, different access patterns, different audit needs.

#### R13 — `node_classifier_profile_attachment` ⟶ `node` (N:1)

- **FK direction**: `node_classifier_profile_attachment.assignee_node_id → node.id`
- **Cardinality**: A node can have many profile attachments. The Acme Programme might have Software Delivery and Portfolio Reporting both attached.
- **Lifecycle**: When a node is deleted, all its profile attachments cascade. (Profile definitions remain — they just don't attach anywhere.)
- **Why attachments live here, not as a JSON column on node**: Same reasoning as R10 — queryable, auditable, supports soft-retirement of individual attachments without affecting others.

---

### 4.3 Assignment layer relationships

#### R14 — `classifier` ⟶ `node_classifier_assignment` (1:N, denormalised)

- **FK direction**: `node_classifier_assignment.classifier_id → classifier.id`
- **Cardinality**: A classifier can have many assignments across many nodes.
- **Why denormalised**: The `classifier_id` on the assignment row is denormalised from `classifier_value.classifier_id`. This avoids a JOIN on the most common UI query ("show me all classifier chips on this node").
- **Coherence check**: A CHECK constraint ensures `node_classifier_assignment.classifier_id` always matches `classifier_value.classifier_id` for the row's `classifier_value_id`. Applications should set both fields atomically.

#### R15 — `classifier_value` ⟶ `node_classifier_assignment` (1:N)

- **FK direction**: `node_classifier_assignment.classifier_value_id → classifier_value.id`
- **Cardinality**: A value can be assigned to many nodes.
- **Lifecycle**: When a value is soft-retired, existing assignments remain for historical integrity. UI hides retired values from new selection.

#### R16 — `node` ⟶ `node_classifier_assignment` (1:N)

- **FK direction**: `node_classifier_assignment.assignee_node_id → node.id`
- **Cardinality**: A node can have many assignments — one per assigned classifier value.
- **Indexing note**: This is the hot query path — *"all classifiers on this node"*. Index `(assignee_node_id, classifier_id)` for fast lookup.
- **Lifecycle**: When a node is deleted, all its assignments cascade. History (R17) retains the events.

---

### 4.4 History layer relationships

#### R17 — `node_classifier_assignment` ⟶ `node_classifier_assignment_history` (1:N, optional)

- **FK direction**: `node_classifier_assignment_history.original_assignment_id → node_classifier_assignment.id` (NULLable)
- **Cardinality**: An assignment can have many history events. The relationship is loose because UNASSIGN events remove the assignment row but the history persists.
- **NULL semantics**: `original_assignment_id` is NULL when:
  - The original assignment was unassigned and the row no longer exists (the history retains the event but cannot reference back to a removed row)
  - The history was written for an ASSIGN event where there was no prior assignment to reference (the new assignment ID may not yet exist at history-write time)
- **Why this is the only nullable FK in the framework**: History must survive its source. If the assignment is deleted, the history events remain. Hard FK constraints would require either cascading deletes (losing history) or preventing deletes (locking the assignment table).

#### Plus three denormalised references on the history table:

- `node_classifier_assignment_history.assignee_node_id → node.id`
- `node_classifier_assignment_history.classifier_id → classifier.id`
- `node_classifier_assignment_history.from_value_id → classifier_value.id` (nullable, NULL for ASSIGN)
- `node_classifier_assignment_history.to_value_id → classifier_value.id` (nullable, NULL for UNASSIGN)

These are all standard N:1 references for query convenience — they let history be queried without joining back through the (potentially deleted) assignment row.

---

### 4.5 External references

The framework references one entity it doesn't own:

- **`node`** (owned by the module's core schema, not the classifier framework) — assignment and profile-attachment rows reference node IDs. The framework doesn't define the node table; each module has its own.

---

### 4.6 Cascade and soft-delete summary

Quick reference for what happens on deletion:

| Source entity | Soft-retire behaviour | Hard-delete behaviour |
|---|---|---|
| `classifier` | Excluded from new scope resolution; existing assignments remain readable | **Not allowed in v1** — would orphan values, assignments, history |
| `classifier_value` | Hidden from new selection; existing assignments remain | **Not allowed in v1** — would orphan assignments and history |
| `classifier_value_policy` | Inherits its classifier's lifecycle | Cascade on classifier hard-delete (n/a in v1) |
| `classifier_value_constraint` | Excluded from constraint checks | Direct delete allowed (no cascade impact) |
| `classifier_profile` | Excluded from scope resolution; attachments remain inactive | **Not allowed in v1** |
| `classifier_profile_member` | Excluded from scope resolution | Direct delete allowed (removes classifier from the bundle) |
| `node_classifier_profile_attachment` | Excluded from scope resolution; closure walk skips it | Direct delete allowed |
| `node_classifier_assignment` | n/a — assignments are deleted on unassign, recorded in history | Delete on UNASSIGN; history retains the event with `original_assignment_id=NULL` |
| `node_classifier_assignment_history` | **Append-only** — never updated, never deleted in v1 | Forbidden |

The single principle: **definition entities are soft-retired, junction entities can be hard-deleted, history is append-only.**

---

### 4.7 The cross-classifier coherence rules

Three coherence rules apply across multiple relationships. These cannot be enforced by simple FK constraints — they require either CHECK constraints with subqueries or application-level enforcement:

1. **Assignment coherence (R14)**: `node_classifier_assignment.classifier_id` must equal `classifier_value.classifier_id` of the assignment's `classifier_value_id`. Enforced as a CHECK constraint via trigger or computed column.

2. **Default value coherence (R5)**: `classifier_value_policy.default_value_id` must reference a value whose `classifier_id` matches the policy's `classifier_id`. Enforced at application layer.

3. **Constraint coherence (R8, R9)**: `classifier_value_constraint.source_value_id` must belong to `source_classifier_id`. Same for target. Enforced at application layer.

These are noted explicitly because they're the most common source of data integrity bugs in classifier systems — values getting assigned to the wrong classifier through bypass paths.

---

## 5. NAMING CONVENTIONS

These conventions apply across the platform.

| Concern | Choice | Rationale |
|---|---|---|
| Central service entities | No module prefix — `classifier`, `classifier_value`, etc. | Owned by classifier service. Single global vocabulary. |
| Module-local entities | `<module>_*` prefix — `projectops_node_classifier_assignment` etc. | Owned by the consuming module. Join to module's `node` table. |
| Display string field | `name` | One word, clear, present-tense noun. Avoids the `label` vs `name` divergence. |
| Sort order field | `order` | Quoted because reserved word in SQL, but conventional. |
| Active flag | `is_active` (BOOLEAN) | Prefix `is_` for booleans. |
| Source flag | `source` VARCHAR CHECK ('platform','tenant') | Explicit, future-extensible (could add 'industry' later). |
| Audit columns | `created_at`, `created_by`, `updated_at`, `updated_by` | Standard everywhere. |
| Junction entities | `<parent>_<child>` (e.g. `classifier_profile_member`) | Reads as a relationship. |
| Self-FK for trees | `parent_<entity>_id` (e.g. `parent_value_id`) | Reserved on `classifier_value` for HIERARCHICAL. |
| Cross-service references | Suffix `_id` only; type is UUID; documented as soft reference | E.g. `classifier_id UUID` on the module's assignment table. No FK keyword in DDL. |

---

## 6. FIELD INVENTORY — ALL 11 ENTITIES + LOOKUP

> **Ownership boundary**: Entities **6.1–6.6** and **6.9** (lookup) live in the
> classifier service — no module prefix. Entities **6.7, 6.8, 6.10** live
> per module — prefixed `<module>_`. Cross-boundary FKs are soft references
> (UUID columns with no DB-level FK constraint).

Format below uses `field_name TYPE constraints` notation. All entities also have the four standard audit columns at the bottom (omitted for brevity unless noteworthy).

### 6.1 `classifier` — the classifier definition

```
id                          UUIDv4    PK
company_id                  UUIDv4    NOT NULL  FK → company.id
                                                Platform classifiers use the reserved
                                                PLATFORM_TENANT_ID. Tenant classifiers
                                                use the tenant's actual company_id.
source                      VARCHAR   NOT NULL  CHECK ('platform','tenant')
code                        VARCHAR   NOT NULL  Machine code (PRIORITY, RAG_STATUS, ...).
                                                Unique per (company, code).
name                        VARCHAR   NOT NULL  Display label.
                                                Unique per (company, name).
description                 TEXT      NULL      Surfaced under the name in UI.
"order"                     INTEGER   NOT NULL  DEFAULT 100
                                                Sort order in the modal's Available list.
applies_to_node_types       JSONB     NULL      Array of node anchor type codes.
                                                NULL or [] = any type.
                                                ["RISK"] = restricted to RISK-anchored nodes.
is_active                   BOOLEAN   NOT NULL  DEFAULT TRUE
                                                Soft-retire. Inactive still readable for
                                                historical assignments.
+ audit columns
```

**Uniqueness rules (enforced as UNIQUE constraints):**

```
UNIQUE (company_id, code)
UNIQUE (company_id, name)
```

- Two classifiers cannot share the same `code` within the same tenant
- Two classifiers cannot share the same display `name` within the same tenant
- A platform classifier "PRIORITY" and a tenant classifier "PRIORITY" coexist (different `company_id`)

**Justification of fields:**

- `source` is non-negotiable — without it you cannot distinguish platform-shipped classifiers from tenant-created ones, and tenants must be prevented from editing platform classifiers.
- `applies_to_node_types` solves a real concern: not every classifier applies to every record type. RISK_PROBABILITY only makes sense on RISK nodes. Without this field, the UI either shows nonsensical classifiers everywhere or has to encode scoping rules in application code.
- `order` is for predictable display. Without it, classifiers appear in undefined order, which feels broken.
- **Name uniqueness** (not just code) prevents real user confusion. Without it, a tenant admin could create two classifiers both called "Priority". The system must require disambiguation at write time.

**PM-level vocabulary is handled by profiles and tags, not by node-scoped classifiers.** When a PM needs control over which classifiers apply in their project subtree, they attach a profile (§10) — they don't author project-scoped classifier definitions. For ad-hoc record-level labelling that doesn't warrant a formal classifier, the platform provides tags (separate mechanism).

---

### 6.2 `classifier_value` — the allowed values

```
id                          UUIDv4    PK
classifier_id               UUIDv4    NOT NULL  FK → classifier.id
parent_value_id             UUIDv4    NULL      FK → classifier_value.id
                                                Self-FK for HIERARCHICAL classifiers.
                                                NULL for flat classifiers.
code                        VARCHAR   NOT NULL  UNIQUE (classifier_id, code)
name                        VARCHAR   NOT NULL  UNIQUE (classifier_id, name)
                                                Display label (e.g. "High", "Green").
description                 TEXT      NULL
"order"                     INTEGER   NOT NULL  DEFAULT 100
colour                      VARCHAR   NULL      Hex string. Used by UI chips.
icon                        VARCHAR   NULL      Unicode glyph or short string.
is_active                   BOOLEAN   NOT NULL  DEFAULT TRUE
+ audit columns
```

**Uniqueness rules:**

```
UNIQUE (classifier_id, code)
UNIQUE (classifier_id, name)
```

Within a classifier:
- Value codes are unique (CRITICAL appears at most once per classifier)
- Value names are unique (you can't have two "Critical" rows even if their codes differ)

This is stricter than v1 (which only enforced code uniqueness). Name uniqueness
prevents confusing duplicates in the assignment UI.

**Justification of fields:**

- `parent_value_id` reserves capability for HIERARCHICAL classifiers without needing a future migration. For flat classifiers it's always NULL. UI implementations in v1 can ignore it; v2 can render trees.
- `colour` and `icon` are display data. They're on the value (not the classifier) because each value has its own visual treatment — Critical is red, Low is grey.
- `description` matters more than it looks. The classifier popup uses it to explain values to users who don't know what "Amber" means in this tenant's RAG conventions.

---

### 6.3 `classifier_value_policy` — how values can be selected

```
id                          UUIDv4    PK
classifier_id               UUIDv4    NOT NULL  UNIQUE (one policy per classifier)
                                                FK → classifier.id
selection_mode_id           UUIDv4    NOT NULL  FK → classifier_selection_mode.id
min_selected                INTEGER   NULL      Lower bound (typically NULL or 1).
max_selected                INTEGER   NULL      Upper bound for MULTI_SELECT.
                                                NULL for SINGLE_SELECT or unlimited.
is_required                 BOOLEAN   NOT NULL  DEFAULT FALSE
default_value_id            UUIDv4    NULL      FK → classifier_value.id
                                                Auto-assigned on first interaction.
allow_custom_values         BOOLEAN   NOT NULL  DEFAULT FALSE
                                                If true, users can create ad-hoc values.
                                                Most classifiers are false.
requires_reason_on_change   BOOLEAN   NOT NULL  DEFAULT FALSE
                                                If true, the UI requires a user-supplied
                                                reason whenever a value is changed.
                                                The reason is captured in history.
                                                Used for governance-tracked classifiers
                                                (e.g. RISK_PROBABILITY, GATE_STATUS).
computation_mode            VARCHAR   NOT NULL  DEFAULT 'manual'
                                                CHECK ('manual','computed','suggested')
                                                manual: user picks (v2 default)
                                                computed: Calc Engine sets; user cannot override
                                                suggested: Calc Engine suggests; user can override
                                                v2 implements only 'manual'.
                                                'computed' and 'suggested' reserved for v3.
default_calculation_id      UUIDv4    NULL      Soft FK → Calc Engine: calculation.id
                                                Cross-service reference. The classifier service
                                                does NOT own the calculation definition.
                                                When computation_mode IN ('computed','suggested'),
                                                this is the default calculation applied when no
                                                profile attachment overrides it.
                                                NULL when computation_mode='manual'.
                                                v3 — see §16.
is_active                   BOOLEAN   NOT NULL  DEFAULT TRUE
+ audit columns
```

**Justification of fields:**

- `selection_mode_id` is a FK (not an inline CHECK) because the mode rows carry useful metadata (`allows_multiple` flag, description) that the application needs. The list will not grow, but the metadata earns its keep. See §6.9 for the lookup entity.
- `default_value_id` enables sensible auto-assignment ("new TASK nodes start with PRIORITY=Medium"). Without it, every classifier starts unassigned, which is a workflow papercut.
- `allow_custom_values` is for things like a "Tags" classifier where users add their own — the tenant's controlled vocabulary grows organically. Most classifiers leave this false.
- `requires_reason_on_change` is the governance hook. When TRUE, the UI inserts an inline reason field on value change. The reason is captured in the history row. Useful for regulated classifiers where every change needs justification.
- `computation_mode` reserves the schema slot for **computed** and **suggested** classifiers, deferred to v3 (see §16). v2 implements only `manual`; the field exists now so v3 doesn't need a migration. This is cheap insurance — one column today, deferred behaviour, no migration later.

---

### 6.4 `classifier_value_constraint` — inter-value rules

```
id                          UUIDv4    PK
source_classifier_id        UUIDv4    NOT NULL  FK → classifier.id
source_value_id             UUIDv4    NOT NULL  FK → classifier_value.id
target_classifier_id        UUIDv4    NOT NULL  FK → classifier.id
target_value_id             UUIDv4    NOT NULL  FK → classifier_value.id
constraint_type             VARCHAR   NOT NULL  CHECK ('REQUIRES','PROHIBITS','WARNS')
is_active                   BOOLEAN   NOT NULL  DEFAULT TRUE
+ audit columns
```

**Justification of fields:**

- The constraint must explicitly name both `source_classifier_id` and `target_classifier_id` because constraints commonly cross classifiers (PRIORITY=CRITICAL REQUIRES RAG=RED). A single `classifier_id` is ambiguous.
- `constraint_type` is an enum with three values:
  - `REQUIRES` — if source value is set, target value must also be set
  - `PROHIBITS` — if source value is set, target value cannot be set
  - `WARNS` — surface a warning but don't block

Constraints are surfaced in the UI as advisory at v1 (warning triangle) — they don't block writes. v2 may add hard enforcement via the assignment service.

---

### 6.5 `classifier_profile` — a bundle of classifiers

```
id                          UUIDv4    PK
company_id                  UUIDv4    NOT NULL  FK → company.id
source                      VARCHAR   NOT NULL  CHECK ('platform','tenant')
code                        VARCHAR   NOT NULL  Unique per (company, code).
name                        VARCHAR   NOT NULL  Unique per (company, name).
description                 TEXT      NULL
"order"                     INTEGER   NOT NULL  DEFAULT 100
                                                Display order in profile lists.
scope_default               VARCHAR   NOT NULL  CHECK ('self','subtree')
                                                Default inheritance behaviour when attached.
is_active                   BOOLEAN   NOT NULL  DEFAULT TRUE
+ audit columns
```

**Uniqueness rules:**

```
UNIQUE (company_id, code)
UNIQUE (company_id, name)
```

Same pattern as classifier — codes and names are unique within the tenant.

**Justification of fields:**

- Same `source` pattern as classifier — platform-seeded profiles ("Standard Project Profile") coexist with tenant-defined ones.
- `scope_default` defaults the inheritance behaviour at attachment time. Most profiles attach with subtree scope; the field lets specific profiles default to `'self'` for one-off attachments.
- Profiles are how PMs control which classifiers apply in their project subtree. Rather than authoring project-scoped classifier definitions, PMs attach a tenant-wide profile to their project — the attachment scopes which classifiers are active, while the underlying classifier definitions remain tenant-managed.

---

### 6.6 `classifier_profile_member` — which classifiers are in which profile

```
id                          UUIDv4    PK
profile_id                  UUIDv4    NOT NULL  FK → classifier_profile.id
classifier_id               UUIDv4    NOT NULL  FK → classifier.id
                                                UNIQUE (profile_id, classifier_id)
is_required                 BOOLEAN   NOT NULL  DEFAULT FALSE
"order"                     INTEGER   NOT NULL  DEFAULT 100
+ audit columns
```

**Justification of fields:**

- `is_required` here is different from the policy's `is_required` — this one says "within this profile, this classifier must be assigned". A classifier can be optional in one profile and required in another. Useful flexibility.
- `order` controls display sequence within the profile.

---

### 6.7 `<module>_node_classifier_profile_attachment` — profile attached to a node

```
id                          UUIDv4    PK
assignee_node_id            UUIDv4    NOT NULL  FK → node.id
                                                Usually a programme or project node.
profile_id                  UUIDv4    NOT NULL  FK → classifier_profile.id
                                                UNIQUE (assignee_node_id, profile_id) WHERE is_active
scope_hint                  VARCHAR   NOT NULL  CHECK ('self','subtree')
                                                Overrides profile.scope_default.
calculation_overrides       JSONB     NULL      Map of {classifier_id: calculation_id}.
                                                Each calculation_id is a soft FK to the Calc
                                                Engine. Overrides the policy's default
                                                calculation for classifiers in this profile,
                                                scoped to this attachment's subtree.
                                                NULL = use policy defaults.
                                                Example:
                                                  {"cls-rag":"calc-financial-health"}
                                                Means: on nodes in this subtree, RAG uses the
                                                Financial Health calculation rather than the
                                                platform/tenant default. See §16 for resolution.
                                                v3 — populated only when Calc Engine ships.
applied_by                  UUIDv4    NOT NULL  FK → authentication.principals.id
applied_at                  TIMESTAMPTZ NOT NULL DEFAULT now()
is_active                   BOOLEAN   NOT NULL  DEFAULT TRUE
+ audit columns
```

**Justification of fields:**

- `scope_hint` lets a specific attachment override the profile's default. Useful for the escape hatch: "this profile usually applies to a subtree, but for this specific attachment I only want it on this single node."
- `calculation_overrides` is the PM's lever for project-specific calculations (RAG, or any other computed classifier). A PM picks which calculation applies in their project subtree; the attachment carries that decision. The calculation IDs are soft FKs to the Calc Engine — see §16. Schema slot reserved in v2; populated when v3 ships.
- `applied_by` and `applied_at` capture attribution. These are user-facing in the modal's profile context strip ("inherited from Programme X, applied by Ayodh on Apr 1").

---

### 6.8 `<module>_node_classifier_assignment` — the hot table

```
id                          UUIDv4    PK
assignee_node_id            UUIDv4    NOT NULL  FK → node.id
                                                Indexed for the "all classifiers on this node" query.
classifier_id               UUIDv4    NOT NULL  Soft FK → classifier.id (cross-service)
                                                Denormalised from classifier_value.classifier_id.
                                                CHECK: must match the value's parent classifier.
classifier_value_id         UUIDv4    NOT NULL  Soft FK → classifier_value.id (cross-service)
assigned_by                 UUIDv4    NOT NULL  FK → authentication.principals.id
assigned_at                 TIMESTAMPTZ NOT NULL DEFAULT now()
assignment_method           VARCHAR   NOT NULL  DEFAULT 'manual'
                                                CHECK ('manual','computed',
                                                       'suggested_accepted','suggested_overridden')
                                                How this assignment was determined.
                                                v2: always 'manual'. v3 supports the others.
calculation_id              UUIDv4    NULL      Soft FK → Calc Engine: calculation.id
                                                Cross-service reference. Which CALCULATION
                                                (method) produced this value.
                                                Reserved for v3.
                                                NULL when assignment_method='manual'.
calculation_rule_id         UUIDv4    NULL      Soft FK → Calc Engine: calculation_rule.id
                                                Cross-service reference. Which RULE within
                                                the calculation actually fired.
                                                Reserved for v3.
                                                NULL when assignment_method='manual'.
                                                Coherence: rule.calculation_id must equal
                                                this row's calculation_id (Calc Engine
                                                enforces; classifier service trusts).
calculation_inputs          JSONB     NULL      Snapshot of input values at calculation time.
                                                Reserved for v3 — e.g. {"cost_variance_pct": 12.5,
                                                "schedule_slip_days": 7} for a computed RAG.
                                                Captured by the classifier service from the
                                                Calc Engine's evaluation context.
+ audit columns
```

**Justification of fields:**

- `classifier_id` is denormalised on this table to avoid a JOIN on the most common UI query ("show me all classifier chips on this node"). The CHECK constraint ensures it stays coherent with the value's parent classifier. This is a deliberate trade — small write cost for big read benefit.
- **No temporal fields** (`effective_from`, `effective_to`, `is_active`). The hot table stores only current state. Historical questions are answered by the separate history table — see §6.10. This keeps the hot table small, fast, and free of "filter by date" complications on every read.
- `assigned_by` and `assigned_at` are user-facing attribution — surfaced in the panel and modal as "by Raj". The audit columns are for system traceability.
- `assignment_method` records *how* the value was set. In v2 every row is `'manual'`. In v3, computed and suggested classifiers populate this with the appropriate value. Captured here so the UI can show "this RAG was system-computed" vs "this RAG was set by Raj".
- `calculation_id` records which **calculation** (method) was used (the *governance choice*). `calculation_rule_id` records which **specific rule** fired (the *threshold/condition*). Both are soft FKs to the **Calc Engine** (see §16) — the classifier service does not own these definitions. They are kept distinct because changing the calculation method ("we now run delivery-led RAG") is a different governance decision from changing a threshold ("AMBER triggers at 15% not 10%"). Both should be auditable independently.
- `calculation_inputs` snapshots the input values seen by the Calc Engine at the time of evaluation. The classifier service captures this from the Calc Engine's response and stores it. Lets an audit answer "what data did the system see when it decided this?" without time-travel queries on the input sources.

---

### 6.9 `classifier_selection_mode` — the mode lookup

```
id                          UUIDv4    PK
company_id                  UUIDv4    NULL      Always NULL for platform-seeded modes.
                                                Reserved for tenant-custom modes (v2+).
code                        VARCHAR   NOT NULL  UNIQUE ('SINGLE_SELECT','MULTIPLE_SELECT','HIERARCHICAL')
name                        VARCHAR   NOT NULL  Display name.
description                 TEXT      NULL
allows_multiple             BOOLEAN   NOT NULL  Denormalised flag. TRUE for MULTIPLE and HIERARCHICAL.
is_active                   BOOLEAN   NOT NULL  DEFAULT TRUE
+ audit columns
```

**Justification of this entity existing at all:**

You could inline the selection mode as a CHECK constraint on the policy. We chose the lookup pattern because:

1. The metadata is useful — `allows_multiple` is denormalised so the application can check "can this classifier take multiple values?" without string-matching the code.
2. The description is helpful for admin UIs explaining the modes to users.
3. Adding a new mode (when v2 needs PARTIAL_SELECT or some other variant) is a row insert, not a CHECK constraint migration.

Seed data (platform-shipped, all tenants see these three rows):

| code | name | allows_multiple |
|---|---|---|
| SINGLE_SELECT | Single Select | false |
| MULTIPLE_SELECT | Multiple Select | true |
| HIERARCHICAL | Hierarchical | true |

---

### 6.10 `<module>_node_classifier_assignment_history` — the audit shadow

The hot assignment table holds only current state. This sibling table holds the historical log of changes. Append-only — never updated, never deleted.

```
id                          UUIDv4    PK
original_assignment_id      UUIDv4    NULL      FK → <module>_node_classifier_assignment.id
                                                NULL when:
                                                  - the assignment was removed (UNASSIGN)
                                                  - the original was deleted then re-created
                                                Set when:
                                                  - referencing a still-live assignment
assignee_node_id            UUIDv4    NOT NULL  FK → node.id
                                                Denormalised for fast query
classifier_id               UUIDv4    NOT NULL  FK → classifier.id
                                                Denormalised; the change happened to
                                                this classifier on the assignee node
operation                   VARCHAR   NOT NULL  CHECK ('ASSIGN','UNASSIGN','REASSIGN')
from_value_id               UUIDv4    NULL      FK → classifier_value.id
                                                NULL for ASSIGN (no prior value)
                                                Set for UNASSIGN and REASSIGN
to_value_id                 UUIDv4    NULL      FK → classifier_value.id
                                                Set for ASSIGN and REASSIGN
                                                NULL for UNASSIGN
reason                      TEXT      NULL      User-supplied or system-generated
                                                rationale. Required when the
                                                classifier's policy has
                                                requires_reason_on_change=TRUE.
session_id                  UUIDv4    NULL      Optional grouping. Multiple history
                                                events from the same modal editing
                                                session share a session_id. Lets you
                                                reconstruct "what was changed together".
assignment_method           VARCHAR   NOT NULL  DEFAULT 'manual'
                                                Mirrors assignment.assignment_method.
                                                Captured at history-write time so audits
                                                can answer "how was this value determined".
calculation_id              UUIDv4    NULL      Soft FK → Calc Engine: calculation.id
                                                Which calculation was applied at this moment.
                                                Reserved for v3.
calculation_rule_id         UUIDv4    NULL      Soft FK → Calc Engine: calculation_rule.id
                                                Which rule within the calculation fired.
                                                Reserved for v3.
calculation_inputs          JSONB     NULL      Snapshot of inputs at write time.
                                                Reserved for v3.
occurred_at                 TIMESTAMPTZ NOT NULL  When the change was committed to history
occurred_by                 UUIDv4    NOT NULL  FK → authentication.principals.id
                                                The user whose action caused the change
+ audit columns (mostly mirror occurred_*)
```

**Three operations capture all assignment changes cleanly:**

| Operation | from_value_id | to_value_id | Meaning |
|---|---|---|---|
| `ASSIGN` | NULL | new value | First value set on this (node, classifier) |
| `UNASSIGN` | old value | NULL | Value removed entirely |
| `REASSIGN` | old value | new value | Value changed from one to another |

This is cleaner than just logging raw INSERT/UPDATE/DELETE because the operation type tells you the semantic intent directly — you don't have to infer change patterns from sequences of row operations.

**Justification for separating history from the hot table:**

The hot table stays small and fast. Every read query against current state is a simple lookup with no date-filter wrapper. The history table is append-only and can grow without affecting hot-path performance. If the history table gets large over time, it can be archived to cold storage without touching the hot table.

This is **Pattern B — Audit Shadow** in the broader temporal-data design space. See §14 for write policy.

---

## 7. THE THREE SELECTION MODES

### 7.1 SINGLE_SELECT

The classifier accepts exactly one value (or zero if not required). The most common mode. Examples: Priority, RAG Status, Phase, Effort Size.

Assigning a new value implicitly removes any existing assignment for that classifier on the same node.

### 7.2 MULTIPLE_SELECT

The classifier accepts multiple values up to `max_selected`. Examples: Strategic Theme (up to 3), Tags, Affected Teams.

Each value is a separate row in the assignment table. Removing one value doesn't affect the others.

### 7.3 HIERARCHICAL (schema-supported, UI deferred)

The classifier's values form a tree via `parent_value_id`. Assigning a value implies the chain from root to that value. Filtering at any level matches all descendants.

**v1 UI behaviour:** hide hierarchical classifiers from selection UIs rather than rendering broken.
**v2 UI behaviour:** drill-down or breadcrumb-style picker.

The schema supports it now so v2 doesn't need a migration.

---

## 8. THE FOUR CONSTRAINT TYPES

Defined on `classifier_value_constraint`:

| Type | Behaviour | Example |
|---|---|---|
| REQUIRES | If source is set, target must also be set. | RISK_PROBABILITY=VERY_HIGH REQUIRES PRIORITY=CRITICAL |
| PROHIBITS | If source is set, target cannot be set. | PRIORITY=LOW PROHIBITS RAG_STATUS=RED |
| WARNS | Surface a warning but don't block. | DOMAIN=GOVERNANCE WARNS PHASE=DRAFT |

(IMPLIES was considered as a fourth type — auto-assign target if source is set — but rejected for v1. Implicit assignments are confusing; users should explicitly choose values.)

Constraints in v1 are **advisory** — the UI shows a warning icon, but writes are not blocked. v2 may add hard enforcement via the assignment service.

---

## 9. SCOPE RESOLUTION — WHICH CLASSIFIERS APPLY TO A NODE?

When the assignment UI opens on a node, the system computes the **scoped classifier set** — the classifiers the user can choose from. The algorithm has two stages: first determine which classifiers are **visible** (definition-tier scope), then determine which are **active for assignment** (profile-tier scope).

```
STAGE 1 — Definition visibility (which classifiers exist for this tenant?)

1. Find all classifiers visible to this tenant:
     visible_set = SELECT * FROM classifier
                    WHERE company_id IN (tenant_id, PLATFORM_TENANT_ID)
                      AND is_active = TRUE.
2. Filter by classifier.applies_to_node_types matching the target node's anchor type.

STAGE 2 — Profile activation (which of those are in scope for assignment?)

3. Walk the target node's ancestor chain (closure table) to get
   ancestor_ids = [target_node_id, parent_id, grandparent_id, ..., root_id].
4. Walk the same ancestor chain looking for active profile attachments.
   For each attachment whose scope_hint covers the target node:
     For each member classifier of the attached profile:
         Add to active_set (intersect with visible_set).
5. If active_set is empty (no profile attached anywhere up the chain):
     Fall back to the module's PROJECT_CLS_SCOPE — a configured array
     of classifier IDs that apply by default when no profile is in scope.
6. Return active_set.
```

**Multiple profiles attaching at different ancestors are unioned.** If a Programme has Profile A (subtree) and a Project below it has Profile B (subtree), descendant nodes see classifiers from both.

**All tenant classifiers are visible across the tenant.** Visibility is tenant-wide; *applicability* is controlled by profile attachments. A PM who needs different classifier scoping for their project subtree achieves it via profile attachments, not via classifier-level node scoping.

**Platform classifiers (company_id = PLATFORM_TENANT_ID) are visible to all tenants.** They are tenant-wide by construction.

---

## 10. UI COMPONENTS

The framework comes with two reusable React components that every module uses:

### 10.1 ClassifiersPanel

Right-hand-panel collapsible section showing the classifier values currently on a node. Sits alongside other right-panel sections (Summary, KPIs, Activity, etc.). Shows: value chips, who assigned, when. Has a "+ Assign classifier" link that opens the modal.

### 10.2 ClassifierAssignmentModal

Centred modal for assigning, changing, and unassigning. Shows the profile context strip (which profile is in scope), the assigned section (current values, expandable to change), and the available section (classifiers not yet assigned).

Both components are pure and prop-driven. Each module passes its own data and callbacks. Module colour (`moduleColour` prop) themes the accent — pink for Freeflow, blue for ProjectOps, etc.

Reference implementation: `Classifiers.jsx` and `RightPanel.jsx` extracted from ProjectOps v16.22.

---

## 11. WHAT'S IN AND OUT FOR v2

### In scope

- All 11 entities + 1 lookup with the fields specified in §6, split across the classifier service (8 + lookup) and consuming modules (3)
- SINGLE_SELECT and MULTIPLE_SELECT selection modes (full UI)
- HIERARCHICAL selection mode (schema only, UI hidden)
- REQUIRES / PROHIBITS / WARNS constraints (advisory in UI)
- Profile bundles with subtree inheritance
- Platform vs tenant `source` distinction
- The two UI components (ClassifiersPanel, ClassifierAssignmentModal)
- Tenant-public classifier visibility (anyone in the tenant can see all classifiers)
- Read replica / cache pattern per consuming module (§12)
- Change propagation via service events (§12)

### Out of scope for v2 (revisit in v3)

- HIERARCHICAL UI (drill-down picker)
- Hard constraint enforcement (currently advisory only)
- Visibility ACL (intra-tenant classifier permissions — who in the tenant can see/use which classifier)
- Per-user classifiers (private vocabularies)
- Bulk assignment / grid views ("rows of nodes × columns of classifiers")
- Cross-module assignment views (the UI side of cross-module knowledge management)
- Classifier authoring UI (creating classifiers / values themselves — admin-only for now)
- Internationalisation of classifier names

### Out of scope permanently

- Temporal assignment history (effective_from / effective_to)
- Free-text values without controlled vocabulary (use a different mechanism — tags, notes, etc.)
- Hard cross-microservice FKs (deliberately replaced by soft references — see §1.9)

---

## 12. CONSUMING THE CLASSIFIER SERVICE

This section replaces v1's "Who Adopts This Framework" with a more precise
service-consumption pattern.

### 12.1 What modules own vs what the service owns

| What | Owner | Where it lives |
|---|---|---|
| Classifier definitions | Classifier service | Central service DB |
| Values, policies, constraints | Classifier service | Central service DB |
| Profiles, profile members | Classifier service | Central service DB |
| Profile attachments to nodes | Consuming module | Module's local DB |
| Value assignments on nodes | Consuming module | Module's local DB |
| Assignment history | Consuming module | Module's local DB |
| Read replica / cache of definitions | Consuming module (optional, recommended) | Module's local DB |

### 12.2 The consumer pattern (recommended: Pattern B — Read replica)

A consuming module:

1. **Maintains a read-only local replica** of the classifier service tables it
   cares about — typically `classifier`, `classifier_value`, `classifier_value_policy`,
   `classifier_profile`, `classifier_profile_member`. These local tables are
   prefixed `<module>_classifier_replica_*` to make their origin explicit and
   prevent local writes (RLS or app-level enforcement).

2. **Subscribes to the classifier service's change feed.** When a classifier
   definition changes (new value added, soft-retired, policy edited), the
   service publishes an event. The module's subscriber updates its local replica.

3. **Reads classifier definitions locally** for hot UI paths (the classifier
   chip render, the assignment modal). Cross-service network calls don't happen
   on read.

4. **Writes assignments locally** to its own `<module>_node_classifier_assignment`
   table, referencing the centrally-owned `classifier_id` and `classifier_value_id`
   as soft references (UUID columns with no FK to a remote DB).

5. **Writes a history row locally** on modal close per Policy D (see §14).

### 12.3 Why Pattern B over alternatives

- **Pattern A — Direct API calls per request**: Simple but adds latency on every
  classifier query. Fails closed if the classifier service is down — the module
  becomes unusable for any work involving classifiers.
- **Pattern B — Read replica (recommended)**: Local-speed reads. Module remains
  functional during transient service outages (definitions read from cache, new
  assignments still write locally). Eventual consistency on definition changes
  is acceptable because definitions change rarely.
- **Pattern C — Periodic sync (materialised view)**: Lazier than B. Stale data
  windows. Suitable only for modules with extremely low classifier-write activity.

### 12.4 The consumer manifest

Each module declares which classifiers it consumes via a **consumer manifest**
posted to the classifier service. The manifest enables three things:

- The classifier service knows which modules to notify on change
- The classifier service can warn admins before deleting a classifier that's
  still consumed somewhere ("PRIORITY is consumed by ProjectOps and Freeflow")
- The classifier service can show usage analytics across the platform

Manifest shape (example):

```json
{
  "module": "projectops",
  "consumes": [
    { "classifier_code": "PRIORITY",     "applies_to_node_types": ["TASK","STORY","FEATURE","RISK"] },
    { "classifier_code": "RAG_STATUS",   "applies_to_node_types": ["PROJECT","PROGRAMME"] },
    { "classifier_code": "RISK_PROBABILITY", "applies_to_node_types": ["RISK"] },
    { "classifier_code": "RISK_IMPACT",  "applies_to_node_types": ["RISK"] }
  ],
  "consumes_profiles": [
    "STANDARD_DELIVERY",
    "RISK_GOVERNANCE"
  ]
}
```

Tenant classifiers don't need to be in the manifest — they're inherently
tenant-bound and the service knows which tenants own which custom classifiers.

### 12.5 Onboarding a new module (e.g. PeopleOps)

A new Omadeas module that wants classifier support:

1. **Build the 3 module-local entities**: `<module>_node_classifier_profile_attachment`,
   `<module>_node_classifier_assignment`, `<module>_node_classifier_assignment_history`.
   Use the schemas in §6.7, §6.8, §6.10.
2. **Build the read replica** of the classifier service's definition tables
   (or call the service synchronously if low volume).
3. **Submit a consumer manifest** declaring which classifiers and profiles
   the module uses.
4. **Subscribe to the change feed** (or accept staleness if using Pattern A).
5. **Integrate `Classifiers.jsx`** — the UI components are framework-provided
   and work with the centralised definitions out of the box.

That's it. No new classifier definitions need to be created for the module —
it reuses the platform's PRIORITY, RAG_STATUS, etc. directly. If the module
needs module-specific classifiers (e.g. PeopleOps "Headcount Status"), they
can be requested as platform-seeded classifiers via the classifier service's
admin process.

---

## 13. NON-NEGOTIABLES

If you change anything else in this service architecture, that's fine. These ten
things you don't change:

1. **11 entities (6 central definition/profile + 2 central history + 1 module attachment + 2 module assignment/history) plus 1 lookup**, with the central/module-local split described in §0 and §12.
2. **Three selection modes named exactly** SINGLE_SELECT / MULTIPLE_SELECT / HIERARCHICAL.
3. **`source` field** on classifier and profile with `('platform','tenant')` enum.
4. **Denormalised `classifier_id`** on the assignment table with CHECK coherence.
5. **No temporal fields** on the assignment table. History lives in its own table.
6. **Profiles inherit via subtree** by default; the scope_hint field on the attachment is the override.
7. **History writes follow Policy D — Two-Tier Write.** See §14.
8. **Cross-service references are soft, not FK.** Database FKs do not cross the
   service boundary. Integrity is application-validated plus cache-invalidation-driven.
9. **Uniqueness rules are enforced at the DB level.**
   - Within `classifier`: `UNIQUE (company_id, code)` and `UNIQUE (company_id, name)`
   - Within `classifier_value`: `UNIQUE (classifier_id, code)` and `UNIQUE (classifier_id, name)`
   - Within `classifier_profile`: `UNIQUE (company_id, code)` and `UNIQUE (company_id, name)`
10. **`assignment_method` is captured per assignment AND per history row.** Every
    write records how the value was determined (`manual`, `computed`,
    `suggested_accepted`, `suggested_overridden`). v2 implements only `manual`;
    the field exists now so v3 needs no migration.
11. **Calculations live in the Calc Engine, not the classifier service.** The
    classifier service stores soft FKs to calculations (`default_calculation_id`,
    `calculation_overrides`, `calculation_id`, `calculation_rule_id`) but does
    NOT define, evaluate, or own them. The Calc Engine is a separate central
    service. See §16.

---

## 14. HISTORY WRITE POLICY (Policy D — Two-Tier Write)

The history table records *deliberate changes*, not exploratory clicks. The framework adopts a single policy: history writes happen at modal-close, not on every interaction.

### 14.1 The two tiers

**Tier 1 — Live writes (immediate).**
Every assignment change (assign, change, unassign) writes to the hot assignment table immediately. The user sees the change reflected in the UI instantly. This is the existing behaviour and does not change.

**Tier 2 — History writes (deferred to session close).**
The history table is written only when the modal closes (× / Esc / click outside / navigate away). For each classifier whose state changed between modal-open and modal-close, exactly one history event is written.

### 14.2 The algorithm

```
On modal open:
  Snapshot current assignments for this node → sessionStartState
  Generate a session_id (UUID)

On each assign / change / unassign:
  Write to the hot assignment table immediately
  No history write yet

On modal close:
  For each classifier C in (sessionStartState ∪ currentState):
    Compare sessionStartState[C] to currentState[C]
    If they differ:
      Determine operation:
        - sessionStartState empty, currentState set → ASSIGN
        - sessionStartState set,   currentState empty → UNASSIGN
        - sessionStartState set,   currentState set, different value → REASSIGN
      Write ONE history row with the from/to values and the session_id
    If they match, no history write for this classifier
```

### 14.3 What this means in practice

- User clicks Critical → High → Critical on the same classifier: **zero** history events. They ended where they started.
- User opens modal, clicks around, never commits, closes modal: **zero** history events.
- User assigns Priority=High and unassigns RAG=Green in one session: **two** history events with the same `session_id`.
- User reassigns Priority from Medium to Critical: **one** REASSIGN event with `from_value_id=Medium`, `to_value_id=Critical`.

### 14.4 What's preserved

The hot table's audit columns (`created_at`, `created_by`, `updated_at`, `updated_by`) still capture the most recent change attribution on each row. Combined with the history table's append-only log, you have both "what is the current state and who changed it last" (hot table) and "what was the sequence of changes" (history table).

### 14.5 The governance escalation

When a classifier's policy has `requires_reason_on_change=TRUE`, the UI behaviour changes:

- On every value change for that classifier, the UI prompts inline for a reason
- The reason text is captured in client state alongside the change
- On modal close, the reason is written to the history row's `reason` field

This is the only behavioural change for governance-tracked classifiers. The two-tier write policy still applies.

### 14.6 Edge cases

**Modal crash / tab killed mid-session.**
The hot table is correct (every change was a live write). The history write doesn't happen. Net effect: the user's changes are persisted, but no history event is recorded. Acceptable trade — the alternative (writing history on every click) is noisier and more expensive.

**Session ID reuse.**
The `session_id` is generated fresh on every modal open. If a user opens, closes, opens again, that's two sessions with two different IDs even on the same node.

**Bulk operations (future).**
When v2 introduces grid-style bulk assignment (multiple nodes × multiple classifiers in one view), the session concept still applies — one session per user, with potentially many history rows on close.

### 14.7 What this policy does NOT do

- It does not provide undo. Once the modal closes, the change is committed. Undo would require additional UI affordance (and is out of scope for v1).
- It does not capture click-by-click exploration. If you need to know "the user considered High before settling on Critical", this policy will not tell you. The policy treats exploration as private and unobservable; only deliberate decisions are recorded.
- It does not buffer changes for a Save button. Live writes are immediate; the only deferral is the history write.

### 14.8 Definition-level history (a separate concern)

§14.1–14.7 cover **assignment** history — when values are assigned to nodes. There is a parallel concern: **definition** history — when the classifiers and profiles themselves change (value added, policy edited, constraint added, profile member changed, etc.).

Definition history uses a different approach from assignment history:

- **No session model.** Definition edits happen in admin screens (Classifier Detail, Profile Detail), one operation at a time. Each operation writes one history row immediately. There is no modal-close deferral.
- **Append-only.** History rows are never updated or deleted. Each row is an immutable record of one definition-level event.
- **Driven by the audit shadow tables `classifier_history` and `classifier_profile_history`.** See §14.9 for the schemas.
- **Renders on the Activity tab** of both Classifier Detail and Profile Detail screens.

The reason for the difference: assignment edits cluster (the user opens a modal, makes several changes, closes — that's one "session"). Definition edits don't cluster — adding a value, editing a policy, and adding a constraint are three distinct deliberate actions, each worth recording separately.

### 14.9 Definition history schemas

`classifier_history` — definition-level audit log for classifiers:

```
id                          UUIDv4    PK
classifier_id               UUIDv4    NOT NULL  FK → classifier.id (cascade on delete)
operation                   VARCHAR   NOT NULL  CHECK ('created','activated','retired',
                                                      'value_added','value_retired','value_edited',
                                                      'policy_edit','constraint_added','constraint_removed',
                                                      'renamed','reordered')
occurred_at                 TIMESTAMPTZ NOT NULL DEFAULT now()
occurred_by                 UUIDv4    NOT NULL  Soft FK → principal.id
summary                     VARCHAR   NOT NULL  Short headline ("Value LOW added", "Policy edited", ...)
detail                      TEXT      NULL      Verbose description ("Display order 30, colour #94A3B8")
payload                     JSONB     NULL      Full before/after snapshot for forensics
+ audit columns
```

`classifier_profile_history` — same pattern for profiles:

```
id                          UUIDv4    PK
profile_id                  UUIDv4    NOT NULL  FK → classifier_profile.id (cascade on delete)
operation                   VARCHAR   NOT NULL  CHECK ('created','activated','retired',
                                                      'member_added','member_removed','member_edited',
                                                      'scope_changed','renamed','reordered',
                                                      'attached','detached')
occurred_at                 TIMESTAMPTZ NOT NULL DEFAULT now()
occurred_by                 UUIDv4    NOT NULL  Soft FK → principal.id
summary                     VARCHAR   NOT NULL
detail                      TEXT      NULL
payload                     JSONB     NULL      Full before/after snapshot
+ audit columns
```

**Indexes:** both tables index `(classifier_id|profile_id, occurred_at DESC)` to power the Activity timeline (most recent first).

**Retention:** indefinite by default. v2 ships without retention; v3 may add policy.

**Justification of fields:**

- `operation` is enumerated rather than free-form so the UI can render distinct icons and colours per operation type (matches the Activity timeline UI).
- `summary` is a short headline rendered as a chip badge; `detail` is the human-readable description; `payload` captures the structured before/after for forensic queries (e.g., "what was the old `max_selected` value?"). All three live together because they're written together.
- `occurred_by` is a soft FK to the identity service's principal table. The BFF resolves it to a display name for rendering.
- The history tables are **classifier-service-owned** (unlike the assignment history which is module-owned per the spec's table split). Definition changes happen entirely within the classifier service; consuming modules don't author definition edits.

---

## 15. CHANGE PROPAGATION (classifier service ↔ consumers)

The classifier service publishes change events to a message bus (or equivalent
event stream) that consuming modules subscribe to. Six event types cover all
definition-layer changes:

| Event | Triggered when | Consumer action |
|---|---|---|
| `classifier.created` | New classifier row added | Replicate row to local cache |
| `classifier.updated` | name / description / order / applies_to_node_types changes | Update local cache |
| `classifier.retired` | is_active flips to false | Update local cache. UI hides from new selection. |
| `value.created` | New classifier_value row added | Replicate. UI offers the new value. |
| `value.updated` | name / colour / icon / order changes | Update local cache. UI re-renders chips. |
| `value.retired` | is_active flips to false | Update local cache. UI hides from new selection. Existing assignments remain valid. |
| `policy.updated` | Selection mode, defaults, constraints change | Update local cache. UI re-renders modal behaviour. |
| `constraint.created/updated/retired` | Constraint rules change | Update local cache. Constraint engine picks up new rules on next evaluation. |
| `profile.created/updated/retired` | Profile or member changes | Update local cache. Scope resolution uses new profile composition. |

### 15.1 Event payload shape

Each event carries the full row state plus the event metadata:

```json
{
  "event_type": "value.retired",
  "event_id": "evt-uuid-...",
  "occurred_at": "2026-03-15T14:30:00Z",
  "tenant_id": "00000000-0000-0000-0000-000000000001",   // platform or tenant
  "classifier_id": "cls-uuid-priority",
  "row": {
    "id": "val-uuid-low",
    "classifier_id": "cls-uuid-priority",
    "code": "LOW",
    "name": "Low",
    "is_active": false,
    "updated_at": "2026-03-15T14:30:00Z",
    "updated_by": "pr-uuid-admin"
  }
}
```

### 15.2 Idempotency and ordering

- **Idempotent handlers**: consumers must handle replays gracefully. If the
  same event arrives twice (network glitch, message bus retry), applying it
  twice should be a no-op.
- **Ordering by occurred_at**: consumers should apply events in occurred_at
  order. Out-of-order events are safe if handlers compare timestamps before
  applying (e.g. don't overwrite a newer state with an older event).
- **Replay-from-beginning supported**: a consumer that comes online for the
  first time (or rebuilds its cache) can request a full replay from the service.

### 15.3 What modules do NOT receive events for

- **Assignment events** — those are module-local; the classifier service has no
  visibility into them and doesn't care.
- **History events** — same reason.
- **Other modules' consumer manifests** — modules see only their own subscription.

### 15.4 Failure modes and recovery

- **Consumer offline / behind**: events queue up; consumer catches up on reconnect.
- **Consumer cache corrupted**: drop local cache, request full replay from service.
- **Service publishes an invalid event**: consumers log and skip. Service must
  publish corrections via a subsequent `*.updated` event.

---

## 16. THE CALCULATION ENGINE — INTERFACE CONTRACT (separate service)

v2 implements **manual assignment only**. When v3 lands, computed and suggested
classifier values will be produced by the **Calculation Engine** (Calc Engine) —
a separate central platform service. The classifier service stores soft FKs to
calculations and snapshots of inputs, but does not define or evaluate them.

This section documents the interface between the two services so the v2 schema
slots line up cleanly with the Calc Engine when it ships.

### 16.1 Why a separate service

Calculations are not classifier-specific. The same Calc Engine that produces a
RAG_STATUS value can also produce:

- A numeric "health score" for a custom-field display
- A boolean "needs intervention" trigger for a notification
- A "burn rate" projection used by reports
- A "predicted completion" date for the timeline view

Coupling calculations to the classifier service would cap their reach to
classification-shaped outputs only. Treating the Calc Engine as a peer of the
classifier service — both centrally owned, both cross-service consumers —
keeps each focused on what it does best.

The classifier service is responsible for **vocabulary and labelling**.
The Calc Engine is responsible for **derived values**.
They combine via soft reference.

### 16.2 What the Calc Engine owns (not in this spec)

The Calc Engine has its own data model. The v2 classifier service spec does
not define it; that lives in a separate Calc Engine v1 spec. For context, the
Calc Engine is expected to own at minimum:

- `calculation` — the calculation header. Name, description, input schema,
  output type (`classifier_value` / `number` / `boolean` / `date` / ...),
  source (platform / tenant), is_active.
- `calculation_rule` — rules within a calculation. Multiple rules per
  calculation; priority_order; condition_expression; output value/expression.

Calculations whose output type is `classifier_value` are the ones referenced
by classifier policy and assignment. The same calculation could emit
RED/AMBER/GREEN for a RAG classifier or output a number for a custom field
elsewhere — the Calc Engine decides; the consumer reads what it needs.

### 16.3 What the classifier service stores

Four touch points carry soft FKs into the Calc Engine:

| Field | Entity | Purpose |
|---|---|---|
| `default_calculation_id` | `classifier_value_policy` | Tenant-wide default calculation when computation_mode != 'manual' |
| `calculation_overrides` | `<module>_node_classifier_profile_attachment` | Per-project override of which calculation applies (JSONB map) |
| `calculation_id` | `<module>_node_classifier_assignment` | Which calculation produced this value |
| `calculation_rule_id` | `<module>_node_classifier_assignment` | Which rule within the calculation actually fired |
| `calculation_inputs` | `<module>_node_classifier_assignment` | Snapshot of inputs at evaluation time |

The history table mirrors `calculation_id`, `calculation_rule_id`, and
`calculation_inputs` so audits work across deleted assignments.

**The classifier service does NOT store:**
- Calculation definitions
- Calculation rule definitions
- Input schema declarations
- DSL or expression syntax
- Evaluation logic

All of these live in the Calc Engine.

### 16.4 The four assignment methods

These four values populate `assignment_method` on the assignment and history rows.

| Method | What happens | User experience |
|---|---|---|
| `manual` | User picks the value in the modal | Pick freely; no Calc Engine call |
| `computed` | Calc Engine evaluates; user cannot override | UI shows value as read-only with the calculation name |
| `suggested_accepted` | Calc Engine suggests; user clicks Accept | UI shows suggested value; user explicitly confirms |
| `suggested_overridden` | Calc Engine suggests; user picks something else | UI shows suggested value as hint; user's choice wins |

Each assignment row records exactly one of these. v2 always writes `manual`.
v3 writes the appropriate value based on the policy's `computation_mode` and
the user action.

### 16.5 Resolution — which calculation applies on a node?

**The principle: calculation selection happens at the execution point**, i.e.
where the assignment is being written, on the assignee node. The Classifier
Service stores the *defaults and overrides* (policy.default_calculation_id and
profile_attachment.calculation_overrides) but does not initiate evaluation.

The classifier service is responsible for **vocabulary**. The Calc Engine is
responsible for **derivation**. The decision to invoke a calculation lives at
the consumption point — on the assignee node — not at classifier definition
time. This keeps the Classifier Service screens focused on labels rather than
becoming a control surface for the Calc Engine.

When v3 evaluates a computed/suggested classifier on a node, the resolution
walks the same closure pattern as classifier visibility:

```
1. Look up the classifier's policy.computation_mode.
   If 'manual', no calculation runs — user picks the value.

2. Walk the node's ancestor chain. For each active profile attachment found,
   innermost first:
     If attachment.calculation_overrides has an entry for this classifier_id:
         Use that calculation_id. Stop.

3. If no attachment override found:
     Use the classifier's policy.default_calculation_id.

4. If that's also NULL:
     The classifier behaves as manual (warn — misconfiguration).

5. With the resolved calculation_id, call the Calc Engine:
     POST /calc-engine/evaluate
     { calculation_id, context: { node_id, project_id, ... } }

   Calc Engine returns:
     { output_value_id, rule_id_fired, inputs_observed }

6. Write the assignment with calculation_id, calculation_rule_id,
   calculation_inputs populated from the Calc Engine response.
```

This is **option C** from the v2 design — sensible global default at policy
level, per-project override at attachment level. Innermost wins.

### 16.6 Per-project variability — RAG for Project 1 vs Project 2

The PM wants Project 1 to run RAG via Financial Health, and Project 2 to
run RAG via Composite. They both share the platform RAG_STATUS classifier.

```
Classifier service:
  RAG_STATUS (platform classifier)
    └── policy.default_calculation_id = "calc-composite"  (tenant default)

Profile: "Project Alpha Profile"
  Attached to Project1
  calculation_overrides: { "RAG_STATUS": "calc-financial-health" }

Profile: "Project Beta Profile"
  Attached to Project2
  calculation_overrides: { "RAG_STATUS": "calc-composite" }   (or omitted — same as default)

Calc Engine (separate service):
  calc-composite          — owned by Calc Engine, with its own rules
  calc-financial-health   — owned by Calc Engine, with its own rules
  calc-delivery-health    — owned by Calc Engine, with its own rules
```

When v3 evaluates RAG on a node in Project 1, the classifier service walks
up, finds the Project Alpha Profile attached at Project 1, sees the override
specifying `calc-financial-health`, and calls the Calc Engine with that
calculation_id. Project 2 gets `calc-composite` (the default, or the
explicit override — same thing).

Same classifier vocabulary, different calculations, different outcomes — and
the mechanism is explicit and auditable across two services.

### 16.7 Cross-service contract requirements

For the boundary to work in v3, the Calc Engine must provide:

1. **An evaluation endpoint** that takes `(calculation_id, context)` and
   returns `(output_value_id, rule_id_fired, inputs_observed)`. The output
   value must be one of the classifier's valid values when the calculation
   is producing a classifier_value type.

2. **A coherence check API** that lets the classifier service validate a
   reference at write time. E.g. when an admin sets
   `policy.default_calculation_id`, the classifier service calls
   `GET /calc-engine/calculations/{id}` to confirm (a) it exists, (b) its
   output type is `classifier_value`, (c) its output values are a subset of
   the classifier's values.

3. **Change events** when calculations are retired or modified, so the
   classifier service can refresh caches and surface "calculation
   unavailable" states in the UI.

4. **Soft retire, not hard delete** for any calculation referenced by a
   classifier assignment. Historical assignments may still reference retired
   calculations and need the metadata for audit display.

These are Calc Engine v1 spec items, listed here so the classifier service
knows what to expect.

### 16.8 What's needed in v3 vs already done in v2

| Concern | v2 status | v3 work |
|---|---|---|
| `computation_mode` on policy | ✅ schema exists | Implement non-manual modes in app |
| `default_calculation_id` on policy | ✅ schema slot, NULL in v2 | Wire to Calc Engine on writes |
| `assignment_method` on assignment | ✅ schema exists; defaults to `manual` | Set appropriately on writes |
| `calculation_id` on assignment | ✅ schema slot, NULL in v2 | Populate from Calc Engine response |
| `calculation_rule_id` on assignment | ✅ schema slot, NULL in v2 | Populate from Calc Engine response |
| `calculation_inputs` on assignment | ✅ schema slot, NULL in v2 | Populate from Calc Engine response |
| Same fields on history | ✅ exists | Mirror at history-write time |
| `calculation_overrides` on attachment | ✅ schema slot, NULL in v2 | UI for editing; Calc Engine call to validate IDs |
| Calc Engine service itself | ❌ separate service | Build the Calc Engine (separate v1 spec) |
| Coherence check API | ❌ not yet defined | Calc Engine v1 |
| Change event subscription | ❌ not yet wired | Both services in v3 |
| UI for picking a calculation | ❌ doesn't exist | Build in Calc Engine UI; classifier service UI links to it |
| UI badges in modal ("computed by X") | ❌ doesn't exist | Build inline indicator |

**The principle**: v2 is forward-compatible with the Calc Engine. When v3
lands, no schema migrations on the hot tables in the classifier service.
The Calc Engine is built fresh in its own service; the classifier service
populates the soft FK columns it already has.

### 16.9 What's still tenant-decided vs PM-decided

| Decision | Who decides | Mechanism |
|---|---|---|
| Whether RAG_STATUS exists as a classifier | Platform | classifier row, source='platform' |
| Whether RAG_STATUS is manual / computed / suggested | Platform | policy.computation_mode |
| The default calculation for RAG_STATUS platform-wide | Platform | policy.default_calculation_id (soft FK to Calc Engine) |
| Available platform calculations | Platform | Calc Engine: calculation rows, source='platform' |
| A tenant-specific calculation | Tenant admin | Calc Engine: calculation row, source='tenant', scope=NULL |
| A project-specific calculation | PM | Calc Engine: calculation row, source='tenant', scope=project node |
| Which calculation applies to "our project" | PM (via profile) | classifier service: profile_attachment.calculation_overrides |
| Whether to override the suggested value | End user | classifier service: assignment_method='suggested_overridden' |

The decision-making is now four levels across two services: platform vocabulary
(classifier service), platform/tenant/PM calculations (Calc Engine),
tenant/project calculation selection (classifier service via attachment), and
end-user override at assignment time (classifier service). Each captured in
a different row, each with its own audit trail.

### 16.10 Why the split matters operationally

A worked example: imagine the tenant decides "we want to start running
'Delivery Health' RAG instead of 'Financial Health' RAG across all our projects".

**Without the split** (if calculations lived in the classifier service):
- You'd edit the classifier_value_computation rows.
- Anything else in the platform that used those computations would change too,
  because they're entangled with the classifier definition.
- Reuse across consumers would require duplicating logic.

**With the split** (Calc Engine separate):
- The tenant admin updates `policy.default_calculation_id` on the RAG_STATUS
  policy to point to `calc-delivery-health` instead of `calc-financial-health`.
- The Delivery Health calculation already exists in the Calc Engine; it's
  also used by a custom-field display and a notification trigger.
- Those other consumers continue using the calculation unchanged. Only the
  RAG_STATUS classifier policy's default changes.
- One row updated. No duplication. No cross-consumer impact.

This is what the boundary buys you. The classifier service handles "what does
labelling look like?". The Calc Engine handles "how is this value derived?".
Both questions are useful platform-wide; both deserve their own home.

---

*End of First Principles · Omadeas Classifier Service v2.0*

*This document is the single source of truth. If you find divergent behaviour in
the classifier service or any module's consumption, this document wins. Open a
review item if you believe the service needs to change.*
