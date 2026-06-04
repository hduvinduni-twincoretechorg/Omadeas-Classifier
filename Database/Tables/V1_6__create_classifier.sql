-- classifier — spec §6.1, plan §5.1
-- Root definition entity. company_id is a cross-service soft FK (no REFERENCES).

CREATE TABLE classifier (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    company_id UUID NOT NULL,                     -- softFK -> company; PLATFORM_TENANT_ID for platform rows
    source VARCHAR(20) NOT NULL,
    code VARCHAR(50) NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    "order" INTEGER NOT NULL DEFAULT 100,
    applies_to_node_types JSONB,                  -- array of anchor codes; NULL/[] = any

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT uq_classifier_company_code UNIQUE (company_id, code),
    CONSTRAINT uq_classifier_company_name UNIQUE (company_id, name),
    CONSTRAINT ck_classifier_source CHECK (source IN ('platform', 'tenant')),
    CONSTRAINT ck_classifier_code   CHECK (code ~ '^[A-Z][A-Z0-9_]+$'),
    CONSTRAINT ck_classifier_description_len CHECK (description IS NULL OR char_length(description) <= 500)
);

CREATE INDEX idx_classifier_company ON classifier (company_id);
