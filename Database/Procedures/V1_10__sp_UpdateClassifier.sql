CREATE OR REPLACE PROCEDURE sp_UpdateClassifier(
    _id UUID,
    _name VARCHAR,
    _description TEXT,
    _order INTEGER,
    _appliesToNodeTypes JSONB,
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    -- code, source and company_id are immutable after creation.
    UPDATE classifier
    SET
        name = _name,
        description = _description,
        "order" = _order,
        applies_to_node_types = _appliesToNodeTypes,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
