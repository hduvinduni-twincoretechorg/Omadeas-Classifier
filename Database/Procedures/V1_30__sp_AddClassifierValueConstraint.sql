CREATE OR REPLACE FUNCTION sp_AddClassifierValueConstraint(
    _sourceClassifierId UUID,
    _sourceValueId UUID,
    _targetClassifierId UUID,
    _targetValueId UUID,
    _constraintType VARCHAR,
    _createdAt TIMESTAMPTZ,
    _createdBy UUID
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
    INSERT INTO classifier_value_constraint (
        id,
        source_classifier_id,
        source_value_id,
        target_classifier_id,
        target_value_id,
        constraint_type,
        created_at,
        created_by
    )
    VALUES (
        gen_random_uuid(),
        _sourceClassifierId,
        _sourceValueId,
        _targetClassifierId,
        _targetValueId,
        _constraintType,
        _createdAt,
        _createdBy
    )
    RETURNING
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
        updated_by AS "UpdatedBy";
END;
$$ LANGUAGE plpgsql;
