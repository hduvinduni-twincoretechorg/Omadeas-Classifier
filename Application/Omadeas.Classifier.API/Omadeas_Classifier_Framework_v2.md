# Omadeas Classifier Framework

### v2.0 · Framework Primer · May 2026

> **Audience:** Developers and architects new to Omadeas. Read this before
> the service specification.
>
> **Purpose:** Explain what a classifier is, why classifiers are owned by
> a separate service, and what concepts a developer needs in their head
> before opening the implementation spec.
>
> **Length:** ~30 minute read. The companion service spec is ~3 hours.

---

## TABLE OF CONTENTS

1. [What is a classifier?](#1-what-is-a-classifier)
2. [The three layers — definition, profile, assignment](#2-the-three-layers--definition-profile-assignment)
3. [Why a separate service?](#3-why-a-separate-service)
4. [The 11 entities + 1 lookup at a glance](#4-the-11-entities--1-lookup-at-a-glance)
5. [Two authoring tiers — platform and tenant](#5-two-authoring-tiers--platform-and-tenant)
6. [Profiles, tags, and the scope problem](#6-profiles-tags-and-the-scope-problem)
7. [Scope resolution — which classifiers apply where](#7-scope-resolution--which-classifiers-apply-where)
8. [Assignment, computation, and history](#8-assignment-computation-and-history)
9. [The Calc Engine boundary](#9-the-calc-engine-boundary)
10. [Cross-service integration — soft FKs and the BFF pattern](#10-cross-service-integration--soft-fks-and-the-bff-pattern)
11. [What's in v2, what's in v3](#11-whats-in-v2-whats-in-v3)
12. [Glossary](#12-glossary)
13. [Where to go next](#13-where-to-go-next)

---

## 1. WHAT IS A CLASSIFIER?

A **classifier** is a named, controlled vocabulary that can be attached
to a record. "Priority", "RAG Status", "Effort Size", "Strategic Theme"
are all classifiers. Each classifier has a curated list of allowed
**values** (CRITICAL / HIGH / MEDIUM / LOW for Priority).

Classifiers are *not* free-text fields. The value list is curated. Either
the platform ships it (PRIORITY is universal — every tenant gets it) or
a tenant admin defines it (a bank tenant might add REGULATORY_DOMAIN
with values BASEL_III / MIFID_II / FCA_CONDUCT).

The framework's job is to make these vocabularies behave consistently
across every Omadeas module — ProjectOps, Freeflow, PeopleOps, and so
on. Same shape, same UI, same write path.

### 1.1 Why labels rather than free text?

Three reasons, in order of importance:

1. **Aggregation and reporting.** "Show me all CRITICAL risks across
   the programme" requires a controlled value. Free text gives you
   "Critical", "critical", "CRIT", "high (but actually critical)", and
   no clean count.
2. **Validation and constraints.** A constraint like
   *PRIORITY=CRITICAL requires RAG_STATUS=RED* depends on both sides
   being from a known vocabulary.
3. **Cross-module consistency.** A risk recorded in ProjectOps and
   referenced from Freeflow should mean the same thing. Free text
   doesn't deliver that; a shared classifier does.

### 1.2 Why not just use enums?

Enums are static. Classifier values are governed but mutable: a tenant
admin can add a new strategic theme, retire an old priority level, change
a colour. The framework treats vocabulary as data, not as code.

This is the central design choice. Once you accept that vocabularies are
data, you need a place to store them, a way to govern who edits them,
and a contract for reading them across modules. That place is the
**Classifier Service**.

---

## 2. THE THREE LAYERS — DEFINITION, PROFILE, ASSIGNMENT

If you understand nothing else, understand these three layers. They are
the most common source of confusion in classifier systems.

| Layer | Question it answers | Who edits | How often |
|---|---|---|---|
| **Definition** | What classifiers exist? What values? What rules? | Admins | Rarely |
| **Profile** | Which classifiers are in scope for which corner of the data? | Project owners | Occasionally |
| **Assignment** | Which value is on which record? | All users | Constantly |

A simple analogy: the **definition** is the dictionary; the **profile** is
the curriculum (which words a course teaches); the **assignment** is the
actual essay (which words you used). Three different jobs, three different
write rates, three different audiences.

### 2.1 Why the separation matters

Mixing these up causes specific problems:

- If the definition and the profile are the same thing, you can't say
  "PRIORITY exists globally but isn't relevant on this project". You
  either get every classifier on every record (chaos at scale) or you
  delete the classifier (losing it everywhere).
- If the profile and the assignment are the same thing, you can't
  pre-curate "the classifiers that apply on this programme" before any
  data has been entered. Every user has to know which classifiers to
  use from scratch.

The framework solves all three problems by keeping the layers distinct.

### 2.2 Reading is cheap, writing is governed

Reading classifier assignments is unrestricted — anyone with access to a
record can see its labels. Writing assignments must pass policy checks
(single vs multi, required, constraint enforcement). The framework's
job is to make **reads simple** and **writes safe**.

---

## 3. WHY A SEPARATE SERVICE?

The instinct is to put classifier data inside each module that uses it.
ProjectOps has its own classifier table. Freeflow has its own. Each
module owns its vocabulary.

We rejected that. Here's why.

### 3.1 The four problems with module-owned classifiers

**Problem 1 — Inconsistent vocabulary across modules.**

If ProjectOps defines PRIORITY with values CRITICAL/HIGH/MEDIUM/LOW
and Freeflow defines its own PRIORITY with values URGENT/IMPORTANT/
NORMAL, a user navigating between modules sees two different priority
systems. Reporting across modules becomes impossible without mapping
tables.

**Problem 2 — Duplicate platform-seeded data.**

PRIORITY, RAG_STATUS, and the standard selection modes are universally
useful. If every module ships them, every module's release process
includes vocabulary migration. When the platform team adds a new
canonical classifier, every module has to adopt it.

**Problem 3 — No cross-module governance.**

A tenant admin who wants to add REGULATORY_DOMAIN has to add it to
ProjectOps, Freeflow, and every other module separately. The admin
screen multiplies by the number of modules.

**Problem 4 — No single source of truth.**

When the data integrity question "is HIGH a valid priority?" arrives,
there's no single place to ask. The answer depends on which module
you're in.

### 3.2 The centralisation argument

A separate Classifier Service answers all four:

- **One vocabulary store**, referenced by every module via soft FK (UUID
  in the consuming module's row points at the classifier service's row)
- **Platform classifiers** shipped once, available to every module
- **Tenant classifiers** authored once, available to every module
- **One admin surface** for managing the vocabulary
- **One source of truth** for "what does HIGH mean?"

### 3.3 What stays in each module

Three things stay module-owned, by design:

| Module-owned | Why |
|---|---|
| `<module>_node_classifier_profile_attachment` | Each module knows which of its nodes have which profile attached. The classifier service doesn't know about node IDs. |
| `<module>_node_classifier_assignment` | The hot table — which value is on which record — lives where the record lives. Joins stay local. |
| `<module>_node_classifier_assignment_history` | Assignment audit log lives with the assignment table. |

The classifier service owns the definitions; each module owns the
mapping of those definitions to its own records. This split is described
in §3 of the service spec.

### 3.4 Trade-offs we accepted

Centralisation has costs. Honest list:

- **Cross-service join** — reading a record's classifier assignments now
  requires a join across service boundaries (record → assignment table
  in module → classifier in service). Solved by a BFF that aggregates.
- **Soft FKs only** — database-level FK constraints can't span service
  boundaries. Integrity is application-validated.
- **Cache invalidation** — when a classifier value is renamed, every
  consuming module needs to know. Solved by change-propagation events
  (see service spec §15).

We accepted these because the alternative — vocabulary fragmentation —
is operationally worse.

---

## 4. THE 11 ENTITIES + 1 LOOKUP AT A GLANCE

The framework has 11 entities split across two ownership boundaries
(classifier service and consuming modules), plus 1 lookup. Read the
diagram top-down.

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
║  PROFILE LAYER (named bundles)                                   ║
║  ┌─────────────────────────────────────┐                         ║
║  │  5. classifier_profile              │  ── named bundles       ║
║  │  6. classifier_profile_member       │  ── junction            ║
║  └─────────────────────────────────────┘                         ║
║                                                                  ║
║  DEFINITION HISTORY (audit log)                                  ║
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
║  ATTACHMENT (where profiles apply)                               ║
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
```

### 4.1 Definition layer (entities 1-4)

These four entities together define **what classifiers exist**.

**1. `classifier`** — the classifier itself. One row per classifier
(PRIORITY, RAG_STATUS, etc.). Has source (platform/tenant), code, name,
description, ordinal, applies_to_node_types, is_active.

**2. `classifier_value`** — the allowed values for each classifier.
One row per value (CRITICAL, HIGH, MEDIUM, LOW for Priority). Has FK
to classifier, code, name, colour, icon, ordinal, parent_value_id (for
hierarchical), is_active.

**3. `classifier_value_policy`** — the selection rules. One row per
classifier. Has selection_mode_id (FK to lookup), is_required,
min_selected, max_selected, allow_custom_values, computation_mode,
default_value_id, default_calculation_id (soft FK to Calc Engine),
requires_reason_on_change.

**4. `classifier_value_constraint`** — inter-value rules across
classifiers. One row per constraint. Has constraint_type (REQUIRES /
EXCLUDES / IMPLIES / CONFLICTS_WITH), source_classifier_id,
source_value_id, target_classifier_id, target_value_id, is_active.

### 4.2 Profile layer (entities 5-6)

**5. `classifier_profile`** — a named bundle of classifiers. Has source,
code, name, description, scope_default (self/subtree), is_active, order.

**6. `classifier_profile_member`** — junction. Which classifiers are in
which profile. Has profile_id, classifier_id, is_required (can override
the classifier-level policy on a per-profile basis), order.

### 4.3 Definition history layer (entities 7-8)

**7. `classifier_history`** — append-only audit log for classifier
definition edits. One row per deliberate edit. Has classifier_id,
operation (created / activated / retired / renamed / value_added /
value_retired / policy_edit / constraint_added / etc.), occurred_at,
occurred_by, summary, detail, payload (JSONB).

**8. `classifier_profile_history`** — same pattern for profiles.

### 4.4 Lookup

**`classifier_selection_mode`** — three platform-seeded rows referenced
by classifier_value_policy. SINGLE_SELECT (allows_multiple=false),
MULTIPLE_SELECT (allows_multiple=true), HIERARCHICAL (UI v2.5+,
allows_multiple=true).

### 4.5 Module-owned entities (9-11)

**9. `<module>_node_classifier_profile_attachment`** — which profile is
attached to which node in this module. Has assignee_node_id,
profile_id, scope_hint, calculation_overrides (JSONB, v3-reserved),
applied_at, applied_by, is_active.

**10. `<module>_node_classifier_assignment`** — the hot table. Which
value is on which record right now. Has assignee_node_id, classifier_id
(denormalised with CHECK coherence), classifier_value_id, assigned_at,
assigned_by, assignment_method (manual / computed / suggested), and
v3-reserved soft FKs to Calc Engine.

**11. `<module>_node_classifier_assignment_history`** — append-only audit
log for assignments. Driven by Policy D (Two-Tier Write, spec §14).

---

## 5. TWO AUTHORING TIERS — PLATFORM AND TENANT

Every classifier and every profile is authored at one of two tiers.

| Tier | Owner | Visibility | Editability |
|---|---|---|---|
| **Platform** | Anthropic / platform team | All tenants | Read-only to tenants |
| **Tenant** | Tenant admin | Tenant-wide | Tenant admins edit |

That's it. Two tiers, no more. An earlier version of this design had a
third "node-scoped" tier where a Project Manager could author classifiers
visible only within their project subtree. We dropped it. Here's why and
what we did instead.

### 5.1 Why we dropped node-scoped authoring

The motivation for node-scoped was: a PM wants their own vocabulary
(e.g. SPRINT_READINESS with values READY / NEEDS_GROOMING / BLOCKED)
without polluting the tenant-wide list.

The problem: at scale (hundreds of PMs across a large organisation),
the tenant vocabulary becomes polluted *anyway*, just via a different
mechanism. And node-scoped authoring adds significant complexity:
composite uniqueness constraints, ancestor-walk visibility checks,
admin-screen filtering.

A clean two-tier model with two supporting mechanisms (profiles and
tags) solves the PM use case without the complexity.

### 5.2 How PMs get their own vocabulary now

**For light-touch labelling that doesn't warrant a formal classifier:**
use **tags**. Tags are a separate, lightweight mechanism — free-form
labels on individual records. The PM tags their plan items READY /
NEEDS_GROOMING / BLOCKED. No vocabulary infrastructure required.

**For runtime scoping of which tenant classifiers apply where:**
use **profiles**. The PM attaches a profile to their project subtree
that includes only the classifiers they want active there. The
underlying classifier definitions stay tenant-managed; the attachment
controls visibility.

**For promotion to formal classifier:** when a tagging pattern proves
valuable enough, the tenant admin promotes it to a tenant classifier.
The boundary stays sharp: classifiers are governed vocabulary, tags
are ad-hoc.

### 5.3 In a large organisation

Large organisations naturally have a lineage along business units,
legal entities, or divisions. Vocabulary proliferation gets controlled
there — the tenant admin role typically exists per-division or
per-legal-entity, and a given admin's vocabulary scope is naturally
bounded. The two-tier model is sufficient for this real-world structure.

---

## 6. PROFILES, TAGS, AND THE SCOPE PROBLEM

Without scoping mechanisms, every tenant classifier would appear on
every record. At 30+ classifiers per tenant (realistic), this is
unusable. The framework has two scoping mechanisms.

### 6.1 The scope problem

Imagine a tenant with these classifiers:
- PRIORITY, RAG_STATUS, RISK_PROBABILITY (universal)
- DOMAIN, COMPLEXITY (project-management style)
- EFFORT_SIZE, PLAN_TYPE (sprint-style)
- STRATEGIC_THEME, ARCHITECTURE_DOMAIN (governance-style)

Each of these is useful *in some context*. None of them is useful on
*every record*. PRIORITY makes sense on a plan item; EFFORT_SIZE
doesn't make sense on a strategic risk; ARCHITECTURE_DOMAIN doesn't
make sense on a marketing task.

The framework needs a way to say "for nodes in this corner of the
tree, these are the relevant classifiers".

### 6.2 Profiles — the curated bundle

A **profile** is a named bundle of classifiers (an ordered list of
classifier IDs, with per-member is_required flags). Examples:

- "Software Delivery Profile" — PRIORITY, RAG_STATUS, EFFORT_SIZE,
  DOMAIN, COMPLEXITY
- "Risk Governance Profile" — PRIORITY, RAG_STATUS, RISK_PROBABILITY
- "Strategic Programme Profile" — STRATEGIC_THEME, RAG_STATUS,
  RISK_PROBABILITY, DOMAIN

A profile is attached to a node via the module's
`<module>_node_classifier_profile_attachment` table. The attachment
has a `scope_hint` (self / subtree).

When a user opens a record and looks at its classifier section, the
framework computes which classifiers are in scope by walking the node's
ancestors and unioning the profiles attached anywhere up the chain.

### 6.3 Tags — the ad-hoc mechanism

For labels that don't warrant a formal classifier, the platform provides
**tags**. Tags are out of scope for the classifier framework itself —
they're a separate, lightweight feature. The distinction:

| Classifier | Tag |
|---|---|
| Curated vocabulary | Free-form |
| Governed write path | Personal/local |
| Cross-module reportable | Module-local |
| Validated with constraints | No validation |
| Edited by admins | Edited by anyone |

When in doubt: use a tag. If the tag pattern catches on, promote it.

### 6.4 The promotion path

```
1. User starts tagging records with "needs-review"
2. Pattern spreads — 50 users now tag this way
3. Tenant admin notices the pattern
4. Admin creates a classifier REVIEW_STATUS with values:
   PENDING_REVIEW / IN_REVIEW / REVIEWED
5. Admin adds REVIEW_STATUS to relevant profiles
6. Users transition from ad-hoc tag to controlled classifier
```

This path is the safety valve that keeps the framework lean. PMs don't
need to ask the admin for new vocabulary every time they want a label —
they tag freely, and the admin promotes when a pattern is established.

---

## 7. SCOPE RESOLUTION — WHICH CLASSIFIERS APPLY WHERE

When the UI opens the classifier assignment panel on a node, it needs to
compute the **scoped classifier set** — the classifiers the user can
pick from. The algorithm has two stages.

### 7.1 Stage 1 — Definition visibility

Which classifiers exist for this tenant?

```sql
visible_set = SELECT * FROM classifier
              WHERE company_id IN (tenant_id, PLATFORM_TENANT_ID)
                AND is_active = TRUE
                AND (applies_to_node_types IS NULL
                     OR target_node.anchor_type IN applies_to_node_types)
```

Two filters: tenant + platform, and entity-type applicability. The
`applies_to_node_types` field is a JSONB array — empty means "any type",
populated means "only these types". This is how RISK_PROBABILITY ends
up only on RISK-anchored nodes.

### 7.2 Stage 2 — Profile activation

Which of those visible classifiers are *active for assignment* here?

```sql
1. ancestor_ids = closure_walk(target_node.id)
2. attachments = SELECT * FROM <module>_node_classifier_profile_attachment
                 WHERE assignee_node_id IN ancestor_ids
                   AND is_active = TRUE
                   AND scope_hint_covers(target_node)
3. active_set = ∅
   For each attachment, add its profile's members to active_set
4. If active_set is empty, fall back to PROJECT_CLS_SCOPE
   (a module-configured default array)
5. Return active_set ∩ visible_set
```

Three properties worth noting:

- **Multiple profiles attaching at different ancestors are unioned.**
  A Programme can have Profile A; a Project below can have Profile B.
  Descendants see classifiers from both.
- **All tenant classifiers are visible across the tenant** at the
  definition level. Visibility is tenant-wide. Applicability is
  controlled by attachments.
- **Empty profile set falls back to a default scope.** If no profile
  is attached anywhere up the chain, the module's `PROJECT_CLS_SCOPE`
  array provides a sensible default.

### 7.3 Why two stages?

The two stages separate two questions:
1. "Could this classifier ever appear on this record?" (definition)
2. "Should it appear on this record right now?" (profile)

The definition question is about the schema; the profile question is
about the working state. Keeping them separate means admins manage
definitions; PMs manage attachments. No overlap.

---

## 8. ASSIGNMENT, COMPUTATION, AND HISTORY

### 8.1 Assignment

An **assignment** is a row in `<module>_node_classifier_assignment`. It
says "this node has this value on this classifier". Plus audit columns
(assigned_at, assigned_by, assignment_method).

Assignment rows are **module-owned**. The classifier service doesn't
know about them. This is by design — joins between a record and its
classifier values stay within the module.

### 8.2 Assignment method

Each assignment row carries an `assignment_method` field:

| Method | Meaning | v2 status |
|---|---|---|
| `manual` | User picked the value | ✓ Live |
| `computed` | The Calc Engine produced it | v3-reserved |
| `suggested_accepted` | Calc Engine suggested, user accepted | v3-reserved |
| `suggested_overridden` | Calc Engine suggested, user overrode | v3-reserved |

v2 only uses `manual`. The other three are reserved for the Calc
Engine integration in v3.

### 8.3 History — two distinct concerns

The framework has **two audit shadows**, for two distinct concerns:

**Definition history (classifier-service-owned):**
- `classifier_history` — every edit to a classifier definition
- `classifier_profile_history` — every edit to a profile definition
- Written immediately, one row per edit
- Renders on the Activity tab of admin screens

**Assignment history (module-owned):**
- `<module>_node_classifier_assignment_history` — every assignment change
- Written on modal close per Policy D (Two-Tier Write, spec §14)
- One event per classifier that changed during the modal session

The two are distinct because the access patterns differ. Definition edits
happen one at a time in admin screens. Assignment edits cluster — a user
opens a record, makes several changes, closes the modal. The framework
treats each cluster as one session and writes one history row per
classifier that actually changed.

### 8.4 What "actually changed" means

If a user clicks CRITICAL → HIGH → CRITICAL and then closes the modal,
zero history rows are written. They ended where they started. Only
*net* changes write to history. This is Policy D in the service spec.

---

## 9. THE CALC ENGINE BOUNDARY

The Classifier Service v2 ships without the Calc Engine. The schema
reserves capability — soft FK fields are present, set to NULL — but the
behaviour is v3-reserved.

### 9.1 What the Calc Engine will do

The Calc Engine is a separate service that produces computed values for
classifiers (and other fields). Examples:

- A "Financial Health" calculation reads budget/burn/forecast inputs
  and produces a RAG_STATUS value
- A "Risk Score" calculation reads probability/impact inputs and
  produces a PRIORITY value
- A "Sprint Readiness" calculation reads acceptance-criteria and
  blocker fields and produces a READINESS value

### 9.2 The classifier-side hooks

Five soft FK slots in the schema connect classifier to Calc Engine:

| Slot | Where | Meaning |
|---|---|---|
| `classifier_value_policy.default_calculation_id` | classifier service | Default Calc Engine calculation for this classifier |
| `<module>_node_classifier_profile_attachment.calculation_overrides` | module | JSONB map: per-classifier override of which calculation runs |
| `<module>_node_classifier_assignment.calculation_id` | module | Which calculation produced this assignment (when method != manual) |
| `<module>_node_classifier_assignment.calculation_rule_id` | module | Which rule within the calculation fired |
| `<module>_node_classifier_assignment.calculation_inputs` | module | JSONB snapshot of inputs at assignment time |

All NULL in v2. Become live in v3 when the Calc Engine ships.

### 9.3 Why the boundary lives here

Computed values are not unique to classifiers. Other fields will use the
Calc Engine too. So the calculation infrastructure belongs in a separate
service, with the classifier service referencing it via soft FK.

This boundary is documented in §16 of the service spec.

---

## 10. CROSS-SERVICE INTEGRATION — SOFT FKs AND THE BFF PATTERN

Centralisation means crossing service boundaries. The framework's
integration pattern has two principles.

### 10.1 Soft FKs only across service boundaries

Database FK constraints don't span service boundaries. So all
cross-service references are **soft FKs**:

- `<module>_node_classifier_assignment.classifier_id` references
  `classifier.id` but no DB-level constraint enforces this
- `<module>_node_classifier_profile_attachment.profile_id` references
  `classifier_profile.id` similarly
- `classifier_history.occurred_by` references
  `principal.id` (identity service) similarly

Integrity is **application-validated** at write time, plus
cache-invalidation-driven (when a classifier value is renamed, the
classifier service publishes an event and consumers update their
caches).

### 10.2 The BFF pattern

Screens don't call the classifier service API directly. They call a
**Backend-for-Frontend** layer that:

1. Aggregates multiple API calls into a single screen-shaped response
2. Resolves foreign keys to display names (e.g.
   `classifier.created_by` → `principal.display_name`)
3. Joins data across services (fanning out to consuming-module
   assignment count APIs)
4. Caches what's cacheable (the lookup table, platform classifiers,
   selection modes)

This means the front-end depends only on the BFF contract, not on the
underlying APIs. When an API changes, the BFF absorbs the change; the
screen is unaffected.

The BFF contract is documented in the Service Contracts XLSX.

### 10.3 Caching

Three caching tiers:

| Tier | Cache | TTL |
|---|---|---|
| Platform-seeded data (selection modes) | Long-lived | Hours |
| Tenant definitions (classifiers, values, policies) | Medium | Minutes |
| Per-record assignments | Short / per-request | Seconds |

Cache invalidation is driven by classifier service events. See §15 of
the service spec.

---

## 11. WHAT'S IN v2, WHAT'S IN v3

### 11.1 What v2 ships

- All 11 entities + 1 lookup with the fields specified in §6 of the
  spec, split across the classifier service (8 + lookup) and consuming
  modules (3)
- Two authoring tiers (platform + tenant)
- Profile-based scope resolution via closure walk
- Single-select and multi-select selection modes (Hierarchical is
  schema-reserved, UI v2.5+)
- Constraint definitions (REQUIRES / EXCLUDES / IMPLIES /
  CONFLICTS_WITH) — soft enforcement (advisory) in v2
- Definition history (classifier_history, classifier_profile_history)
- Assignment history per Policy D (Two-Tier Write)
- Change propagation events (6 event types, spec §15)

### 11.2 What v2 reserves but doesn't ship

- The Calc Engine itself — schema slots exist (soft FK fields), all
  NULL in v2
- Hierarchical selection mode UI — schema row exists, UI in v2.5+
- Hard constraint enforcement — v2 is advisory, v2.5+ may enforce
- Custom values (`allow_custom_values`) — field exists, behaviour TBD
- RBAC for finer-grained write permissions — application-layer in v2,
  formal in v3

### 11.3 What v2 explicitly does not do

- Node-scoped classifier authoring (dropped in favour of profiles + tags)
- Free-text labels (use tags)
- Cross-tenant vocabulary sharing (platform tier is the mechanism)
- Schema migration at v3 cutover — v2's schema is forward-compatible

---

## 12. GLOSSARY

**Anchor** — A node in a planning hierarchy (PROJECT, DELIVERABLE, etc.).
Cross-cuts with classifiers: every anchor type can have classifier
assignments. From the ProjectOps Anchor Model.

**Assignment** — A row pairing a record (a "node") with a classifier
value. The actual labelling event.

**BFF** — Backend-for-Frontend. The integration layer between screens
and underlying APIs. Aggregates, joins, caches, shapes.

**Calc Engine** — Separate service that produces computed classifier
values. v3-reserved.

**Classifier** — A named controlled vocabulary attached to records.

**Classifier value** — One of the allowed values for a classifier
(CRITICAL is a value of PRIORITY).

**Closure walk** — Walking up the ancestor chain of a node via the
closure table, used for scope resolution.

**Computed (assignment method)** — Value produced by the Calc Engine,
not by a user.

**Constraint** — Inter-value rule across classifiers. Four types.

**Definition (layer)** — The "what classifiers exist" layer. Edited
rarely by admins.

**Manual (assignment method)** — Value picked by a user. The only
method live in v2.

**Module** — An Omadeas product surface (ProjectOps, Freeflow, etc.)
that consumes classifiers.

**Platform tier** — Classifiers authored by the platform team,
available to all tenants, read-only to tenants.

**Policy** — The selection rules for a classifier (single vs multi,
required, min/max selected).

**Profile** — A named bundle of classifiers. Attached to nodes to
scope which classifiers are active.

**Scope resolution** — The algorithm that computes which classifiers
apply to a given record at a given time.

**Selection mode** — Platform-seeded lookup of three selection
behaviours: SINGLE_SELECT, MULTIPLE_SELECT, HIERARCHICAL.

**Soft FK** — A logical foreign key reference that isn't enforced by
the database (because it spans services).

**Tag** — A free-form label, separate mechanism from classifiers. The
PM-level lightweight labelling tool.

**Tenant tier** — Classifiers authored by a tenant admin, available
across that tenant.

**Two-Tier Write (Policy D)** — Assignment history write policy: live
writes immediate, history writes deferred to modal close. See spec §14.

---

## 13. WHERE TO GO NEXT

After reading this:

1. **For implementation details:** read the full service specification
   — `Omadeas_Classifier_Service_v2.md`. ~1700 lines, ~3 hours.
2. **For schema:** see the data model XLSX —
   `Omadeas_Classifier_Service_v2_DataModel.xlsx`. Four sheets:
   INDEX, entity_map, field_inventory, relationships.
3. **For BFF and API contracts:** see the service contracts XLSX —
   `Omadeas_Classifier_Service_v2_ServiceContracts.xlsx`. Six sheets
   covering screens, service files, BFF endpoints, API endpoints, and
   field-by-field traceability back to the data model.
4. **For the UI:** see the dogfood JSX prototype —
   `ProjectOps_-_Dogfood_Plan_Visual_v16.36.jsx`. Five screens
   covering the full admin surface.
5. **For test data:** see the JSON fixtures —
   `fixtures/*.json`. Eleven files mirroring the BFF response shapes.
6. **For the build plan:** see the project plan XLSX —
   `Omadeas_Classifier_Service_v2_ProjectPlan.xlsx`. The anchor-model
   plan that produced this handover.

---

*Framework primer v2.0 · supersedes v1.0 · single source of truth*
