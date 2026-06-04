-- Soft-retire / reactivate a value. Definition entities are never hard-deleted (spec §13 #1);
-- existing assignments keep referencing retired values for historical integrity.
CREATE OR REPLACE PROCEDURE sp_SetClassifierValueActive(
    _id UUID,
    _isActive BOOLEAN,
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    UPDATE classifier_value
    SET
        is_active = _isActive,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
