-- Updates a constraint's type and active state. Source/target references are immutable
-- (changing them is a different constraint — delete and re-add).
CREATE OR REPLACE PROCEDURE sp_UpdateClassifierValueConstraint(
    _id UUID,
    _constraintType VARCHAR,
    _isActive BOOLEAN,
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    UPDATE classifier_value_constraint
    SET
        constraint_type = _constraintType,
        is_active = _isActive,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
