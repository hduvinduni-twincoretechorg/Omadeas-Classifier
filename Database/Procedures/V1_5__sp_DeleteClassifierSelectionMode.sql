CREATE OR REPLACE PROCEDURE sp_DeleteClassifierSelectionMode(
    _id UUID
)
AS $$
BEGIN
    DELETE FROM classifier_selection_mode
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
