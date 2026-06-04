CREATE OR REPLACE FUNCTION sp_GetClassifierValueConstraintById(
    _id UUID
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
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
