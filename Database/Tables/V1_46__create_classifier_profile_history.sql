-- classifier_profile_history — spec §14.9, plan §5.8
-- Append-only profile definition audit log. One row per deliberate edit.
-- profile_id is a real intra-service FK (CASCADE).
-- 'attached'/'detached' are kept for forward-compat (attachment is module-owned; the classifier
-- service only writes them if a module reports the event back, post-v2).

CREATE TABLE classifier_profile_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    profile_id UUID NOT NULL,
    operation VARCHAR(32) NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    occurred_by UUID NOT NULL,
    summary VARCHAR(200) NOT NULL,
    detail TEXT,
    payload JSONB,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT fk_classifier_profile_history_profile
        FOREIGN KEY (profile_id) REFERENCES classifier_profile (id) ON DELETE CASCADE,
    CONSTRAINT ck_classifier_profile_history_operation CHECK (operation IN (
        'created', 'activated', 'retired', 'renamed', 'reordered', 'scope_changed',
        'member_added', 'member_removed', 'member_edited',
        'attached', 'detached'
    ))
);

CREATE INDEX idx_classifier_profile_history_profile_occurred
    ON classifier_profile_history (profile_id, occurred_at DESC);
