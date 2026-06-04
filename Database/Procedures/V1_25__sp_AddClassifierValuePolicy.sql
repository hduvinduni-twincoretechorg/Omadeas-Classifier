CREATE OR REPLACE FUNCTION sp_AddClassifierValuePolicy(
    _classifierId UUID,
    _selectionModeId UUID,
    _minSelected INTEGER,
    _maxSelected INTEGER,
    _isRequired BOOLEAN,
    _defaultValueId UUID,
    _allowCustomValues BOOLEAN,
    _requiresReasonOnChange BOOLEAN,
    _computationMode VARCHAR,
    _defaultCalculationId UUID,
    _createdAt TIMESTAMPTZ,
    _createdBy UUID
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
    INSERT INTO classifier_value_policy (
        id,
        classifier_id,
        selection_mode_id,
        min_selected,
        max_selected,
        is_required,
        default_value_id,
        allow_custom_values,
        requires_reason_on_change,
        computation_mode,
        default_calculation_id,
        created_at,
        created_by
    )
    VALUES (
        gen_random_uuid(),
        _classifierId,
        _selectionModeId,
        _minSelected,
        _maxSelected,
        _isRequired,
        _defaultValueId,
        _allowCustomValues,
        _requiresReasonOnChange,
        _computationMode,
        _defaultCalculationId,
        _createdAt,
        _createdBy
    )
    RETURNING
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
        updated_by AS "UpdatedBy";
END;
$$ LANGUAGE plpgsql;
