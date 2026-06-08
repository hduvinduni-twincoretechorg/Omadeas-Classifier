-- classifier_profile_member — spec §6.6, plan §5.6
-- Junction: which classifiers are in which profile. Hard delete removes a classifier from the
-- bundle (junction entity, §4.6). is_required overrides the policy's is_required per profile.

CREATE TABLE classifier_profile_member (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    profile_id UUID NOT NULL,
    classifier_id UUID NOT NULL,
    is_required BOOLEAN NOT NULL DEFAULT FALSE,
    "order" INTEGER NOT NULL DEFAULT 100,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT uq_classifier_profile_member UNIQUE (profile_id, classifier_id),
    CONSTRAINT fk_cpm_profile
        FOREIGN KEY (profile_id) REFERENCES classifier_profile (id) ON DELETE CASCADE,
    CONSTRAINT fk_cpm_classifier
        FOREIGN KEY (classifier_id) REFERENCES classifier (id)
);

CREATE INDEX idx_cpm_profile ON classifier_profile_member (profile_id);
