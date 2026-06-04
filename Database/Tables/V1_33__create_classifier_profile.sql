-- classifier_profile — spec §6.5, plan §5.5
-- A named bundle of classifiers. Same platform/tenant ownership model as classifier.
-- company_id is a cross-service soft FK. Soft-retire only (definition entity, §4.6).

CREATE TABLE classifier_profile (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    company_id UUID NOT NULL,                     -- softFK -> company; PLATFORM_TENANT_ID for platform rows
    source VARCHAR(20) NOT NULL,
    code VARCHAR(50) NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    "order" INTEGER NOT NULL DEFAULT 100,
    scope_default VARCHAR(20) NOT NULL DEFAULT 'subtree',   -- default inheritance when attached

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT uq_classifier_profile_company_code UNIQUE (company_id, code),
    CONSTRAINT uq_classifier_profile_company_name UNIQUE (company_id, name),
    CONSTRAINT ck_classifier_profile_source CHECK (source IN ('platform', 'tenant')),
    CONSTRAINT ck_classifier_profile_code   CHECK (code ~ '^[A-Z][A-Z0-9_]+$'),
    CONSTRAINT ck_classifier_profile_scope  CHECK (scope_default IN ('self', 'subtree')),
    CONSTRAINT ck_classifier_profile_description_len CHECK (description IS NULL OR char_length(description) <= 500)
);

CREATE INDEX idx_classifier_profile_company ON classifier_profile (company_id);
