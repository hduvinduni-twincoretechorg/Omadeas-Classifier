-- Hard delete is allowed for constraints (junction entity, spec §4.6).
CREATE OR REPLACE PROCEDURE sp_DeleteClassifierValueConstraint(
    _id UUID
)
AS $$
BEGIN
    DELETE FROM classifier_value_constraint
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
