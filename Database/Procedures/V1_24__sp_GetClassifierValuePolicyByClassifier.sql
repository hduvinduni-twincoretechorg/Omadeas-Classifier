CREATE OR REPLACE FUNCTION sp_GetClassifierValuePolicyByClassifier(
    _classifierId UUID
)
RETURNS TABLE (
    "Id" UUID,
    "ClassifierId" UUID,
    "SelectionModeId" UUID,
    "MinSelected" INTEGER,
    "MaxSelected" INTEGER,
    "IsRequired" BOOLEAN,
    "DefaultValueId" UUID,
    "AllowCustomValues" BOOLEAN,
    "RequiresReasonOnChange" BOOLEAN,
    "ComputationMode" VARCHAR,
    "DefaultCalculationId" UUID,
    "IsActive" BOOLEAN,
    "CreatedAt" TIMESTAMPTZ,
    "UpdatedAt" TIMESTAMPTZ,
    "CreatedBy" UUID,
    "UpdatedBy" UUID
)
AS $$
BEGIN
    RETURN QUERY
    SELECT
        id AS "Id",
        classifier_id AS "ClassifierId",
        selection_mode_id AS "SelectionModeId",
        min_selected AS "MinSelected",
        max_selected AS "MaxSelected",
        is_required AS "IsRequired",
        default_value_id AS "DefaultValueId",
        allow_custom_values AS "AllowCustomValues",
        requires_reason_on_change AS "RequiresReasonOnChange",
        computation_mode AS "ComputationMode",
        default_calculation_id AS "DefaultCalculationId",
        is_active AS "IsActive",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy"
    FROM classifier_value_policy
    WHERE classifier_id = _classifierId;
END;
$$ LANGUAGE plpgsql;
