-- classifier_value_policy — spec §6.3, plan §5.3
-- Selection rules for a classifier. 1:1 with classifier (UNIQUE classifier_id).
-- selection_mode_id and default_value_id are real intra-service FKs.
-- default_calculation_id is a Calc-Engine soft FK (NULL in v2).

CREATE TABLE classifier_value_policy (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    classifier_id UUID NOT NULL,
    selection_mode_id UUID NOT NULL,
    min_selected INTEGER,
    max_selected INTEGER,
    is_required BOOLEAN NOT NULL DEFAULT FALSE,
    default_value_id UUID,
    allow_custom_values BOOLEAN NOT NULL DEFAULT FALSE,
    requires_reason_on_change BOOLEAN NOT NULL DEFAULT FALSE,
    computation_mode VARCHAR(20) NOT NULL DEFAULT 'manual',
    default_calculation_id UUID,                  -- softFK -> Calc Engine; NULL in v2

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ,
    created_by UUID,
    updated_by UUID,

    CONSTRAINT uq_classifier_value_policy_classifier UNIQUE (classifier_id),
    CONSTRAINT fk_classifier_value_policy_classifier
        FOREIGN KEY (classifier_id) REFERENCES classifier (id) ON DELETE CASCADE,
    CONSTRAINT fk_classifier_value_policy_mode
        FOREIGN KEY (selection_mode_id) REFERENCES classifier_selection_mode (id),
    CONSTRAINT fk_classifier_value_policy_default_value
        FOREIGN KEY (default_value_id) REFERENCES classifier_value (id),
    CONSTRAINT ck_classifier_value_policy_computation_mode
        CHECK (computation_mode IN ('manual', 'computed', 'suggested'))
);
