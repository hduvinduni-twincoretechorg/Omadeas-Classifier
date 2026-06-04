CREATE OR REPLACE PROCEDURE sp_UpdateClassifierValue(
    _id UUID,
    _name VARCHAR,
    _description TEXT,
    _order INTEGER,
    _colour VARCHAR,
    _icon VARCHAR,
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    -- code, classifier_id and parent_value_id are immutable after creation (reparenting is v2+).
    UPDATE classifier_value
    SET
        name = _name,
        description = _description,
        "order" = _order,
        colour = _colour,
        icon = _icon,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
