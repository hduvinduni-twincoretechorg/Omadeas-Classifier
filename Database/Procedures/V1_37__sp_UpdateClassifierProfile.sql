CREATE OR REPLACE PROCEDURE sp_UpdateClassifierProfile(
    _id UUID,
    _name VARCHAR,
    _description TEXT,
    _order INTEGER,
    _scopeDefault VARCHAR,
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    -- code, source and company_id are immutable after creation.
    UPDATE classifier_profile
    SET
        name = _name,
        description = _description,
        "order" = _order,
        scope_default = _scopeDefault,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
