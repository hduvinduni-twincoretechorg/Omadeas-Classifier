-- Hard delete removes the classifier from the profile (junction entity, spec §4.6).
CREATE OR REPLACE PROCEDURE sp_DeleteClassifierProfileMember(
    _id UUID
)
AS $$
BEGIN
    DELETE FROM classifier_profile_member
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;
