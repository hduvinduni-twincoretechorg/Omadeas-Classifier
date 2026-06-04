-- Updates the policy for a classifier (1:1, keyed by classifier_id).
CREATE OR REPLACE PROCEDURE sp_UpdateClassifierValuePolicy(
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
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    UPDATE classifier_value_policy
    SET
        selection_mode_id = _selectionModeId,
        min_selected = _minSelected,
        max_selected = _maxSelected,
        is_required = _isRequired,
        default_value_id = _defaultValueId,
        allow_custom_values = _allowCustomValues,
        requires_reason_on_change = _requiresReasonOnChange,
        computation_mode = _computationMode,
        default_calculation_id = _defaultCalculationId,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE classifier_id = _classifierId;
END;
$$ LANGUAGE plpgsql;
