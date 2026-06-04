-- Updates a member's is_required override and display order.
-- profile_id and classifier_id are immutable (a different pairing is a different member).
CREATE OR REPLACE PROCEDURE sp_UpdateClassifierProfileMember(
    _id UUID,
    _isRequired BOOLEAN,
    _order INTEGER,
    _updatedAt TIMESTAMPTZ,
    _updatedBy UUID
)
AS $$
BEGIN
    UPDATE classifier_profile_member
    SET
        is_required = _isRequired,
        "order" = _order,
        updated_at = _updatedAt,
        updated_by = _updatedBy
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
