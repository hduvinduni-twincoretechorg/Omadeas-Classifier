-- classifier_history — spec §14.9, plan §5.7
-- Append-only definition audit log. One row per deliberate edit.
-- classifier_id is a real intra-service FK (CASCADE), per plan §2 FK rules.

CREATE TABLE classifier_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    classifier_id UUID NOT NULL,
    operation VARCHAR(32) NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    occurred_by UUID NOT NULL,                    -- softFK -> principals (SYSTEM when no principal)
    summary VARCHAR(200) NOT NULL,
    detail TEXT,
    payload JSONB,                                -- before/after snapshot for forensics

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT fk_classifier_history_classifier
        FOREIGN KEY (classifier_id) REFERENCES classifier (id) ON DELETE CASCADE,
    CONSTRAINT ck_classifier_history_operation CHECK (operation IN (
        'created', 'activated', 'retired', 'renamed', 'reordered',
        'value_added', 'value_retired', 'value_edited',
        'policy_edit', 'constraint_added', 'constraint_removed'
    ))
);

CREATE INDEX idx_classifier_history_classifier_occurred
    ON classifier_history (classifier_id, occurred_at DESC);
