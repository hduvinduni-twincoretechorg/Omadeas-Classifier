-- classifier_selection_mode (lookup) — spec §6.9, plan §5.9
-- Platform-seeded, read-only. Three modes ship with the platform.

CREATE TABLE classifier_selection_mode (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    company_id UUID,                              -- always NULL for platform-seeded modes
    code VARCHAR(50) NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    allows_multiple BOOLEAN NOT NULL,             -- denormalised flag

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT uq_classifier_selection_mode_code UNIQUE (code),
    CONSTRAINT ck_classifier_selection_mode_code
        CHECK (code IN ('SINGLE_SELECT', 'MULTIPLE_SELECT', 'HIERARCHICAL'))
);

-- Platform seed (stable UUIDs so policies can reference them deterministically).
INSERT INTO classifier_selection_mode (id, code, name, description, allows_multiple)
VALUES
    ('11111111-0000-0000-0000-000000000001', 'SINGLE_SELECT',   'Single Select',   'Exactly one value (or zero if not required).',      FALSE),
    ('11111111-0000-0000-0000-000000000002', 'MULTIPLE_SELECT', 'Multiple Select', 'Multiple values up to max_selected.',               TRUE),
    ('11111111-0000-0000-0000-000000000003', 'HIERARCHICAL',    'Hierarchical',    'Tree of values via parent_value_id (UI deferred).', TRUE);
