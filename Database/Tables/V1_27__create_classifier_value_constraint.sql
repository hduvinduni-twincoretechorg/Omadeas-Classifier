-- classifier_value_constraint — spec §6.4, plan §5.4
-- Inter-value rules across classifiers (dual source/target). Advisory only in v2.
-- Coherence R8/R9 (source_value belongs to source_classifier, target_value to target_classifier)
-- is enforced app-layer. Junction entity: hard delete allowed (spec §4.6).

CREATE TABLE classifier_value_constraint (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    source_classifier_id UUID NOT NULL,
    source_value_id UUID NOT NULL,
    target_classifier_id UUID NOT NULL,
    target_value_id UUID NOT NULL,
    constraint_type VARCHAR(20) NOT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT fk_cvc_source_classifier FOREIGN KEY (source_classifier_id) REFERENCES classifier (id),
    CONSTRAINT fk_cvc_source_value      FOREIGN KEY (source_value_id)      REFERENCES classifier_value (id),
    CONSTRAINT fk_cvc_target_classifier FOREIGN KEY (target_classifier_id) REFERENCES classifier (id),
    CONSTRAINT fk_cvc_target_value      FOREIGN KEY (target_value_id)      REFERENCES classifier_value (id),
    CONSTRAINT ck_cvc_type CHECK (constraint_type IN ('REQUIRES', 'PROHIBITS', 'WARNS'))
);

CREATE INDEX idx_cvc_source_classifier ON classifier_value_constraint (source_classifier_id);
CREATE INDEX idx_cvc_target_classifier ON classifier_value_constraint (target_classifier_id);
