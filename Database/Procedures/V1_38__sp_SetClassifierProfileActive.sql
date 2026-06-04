-- Soft-retire / reactivate a profile. Definition entities are never hard-deleted (spec §13 #1);
-- attachments remain but the profile is excluded from scope resolution while inactive.
CREATE OR REPLACE PROCEDURE sp_SetClassifierProfileActive(
    _id UUID,
    _isActive BOOLEAN,
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    UPDATE classifier_profile
    SET
        is_active = _isActive,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
