-- classifier_value — spec §6.2, plan §5.2
-- Allowed values for a classifier. classifier_id and parent_value_id are real intra-service FKs.
-- parent_value_id is the self-FK for HIERARCHICAL classifiers (UI deferred); for flat classifiers
-- it stays NULL. Coherence R3 (child.classifier_id = parent.classifier_id) is enforced app-layer.

CREATE TABLE classifier_value (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    classifier_id UUID NOT NULL,
    parent_value_id UUID,
    code VARCHAR(50) NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    "order" INTEGER NOT NULL DEFAULT 100,
    colour VARCHAR(7),
    icon VARCHAR(20),

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT fk_classifier_value_classifier
        FOREIGN KEY (classifier_id) REFERENCES classifier (id),
    CONSTRAINT fk_classifier_value_parent
        FOREIGN KEY (parent_value_id) REFERENCES classifier_value (id),
    CONSTRAINT uq_classifier_value_code UNIQUE (classifier_id, code),
    CONSTRAINT uq_classifier_value_name UNIQUE (classifier_id, name),
    CONSTRAINT ck_classifier_value_code CHECK (code ~ '^[A-Z][A-Z0-9_]+$'),
    CONSTRAINT ck_classifier_value_colour CHECK (colour IS NULL OR colour ~ '^#[0-9a-fA-F]{6}$'),
    CONSTRAINT ck_classifier_value_description_len CHECK (description IS NULL OR char_length(description) <= 500)
);

CREATE INDEX idx_classifier_value_classifier ON classifier_value (classifier_id);
CREATE INDEX idx_classifier_value_parent ON classifier_value (parent_value_id);
