-- Soft-retire / reactivate a classifier. Definition entities are never hard-deleted
-- (spec §13 #1) — retiring keeps values, assignments and history intact.
CREATE OR REPLACE PROCEDURE sp_SetClassifierActive(
    _id UUID,
    _isActive BOOLEAN,
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    UPDATE classifier
    SET
        is_active = _isActive,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
