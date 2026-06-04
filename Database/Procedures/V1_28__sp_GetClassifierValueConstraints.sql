-- Returns constraints, optionally filtered by a classifier (matching source OR target) and active state.
CREATE OR REPLACE FUNCTION sp_GetClassifierValueConstraints(
    _classifierId UUID DEFAULT NULL,
    _isActive BOOLEAN DEFAULT NULL
)
RETURNS TABLE (
    "Id" UUID,
    "SourceClassifierId" UUID,
    "SourceValueId" UUID,
    "TargetClassifierId" UUID,
    "TargetValueId" UUID,
    "ConstraintType" VARCHAR,
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
        source_classifier_id AS "SourceClassifierId",
        source_value_id AS "SourceValueId",
        target_classifier_id AS "TargetClassifierId",
        target_value_id AS "TargetValueId",
        constraint_type AS "ConstraintType",
        is_active AS "IsActive",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy"
    FROM classifier_value_constraint
    WHERE (_classifierId IS NULL
           OR source_classifier_id = _classifierId
           OR target_classifier_id = _classifierId)
      AND (_isActive IS NULL OR is_active = _isActive)
    ORDER BY created_at;
END;
$$ LANGUAGE plpgsql;
