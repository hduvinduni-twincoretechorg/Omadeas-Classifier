CREATE OR REPLACE PROCEDURE sp_UpdateClassifierSelectionMode(
    _id UUID,
    _name VARCHAR,
    _description TEXT,
    _allowsMultiple BOOLEAN,
    _isActive BOOLEAN,
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    -- code and company_id are immutable after creation.
    UPDATE classifier_selection_mode
    SET
        name = _name,
        description = _description,
        allows_multiple = _allowsMultiple,
        is_active = _isActive,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
