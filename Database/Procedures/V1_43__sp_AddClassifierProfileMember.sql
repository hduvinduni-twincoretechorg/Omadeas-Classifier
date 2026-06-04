CREATE OR REPLACE FUNCTION sp_AddClassifierProfileMember(
    _profileId UUID,
    _classifierId UUID,
    _isRequired BOOLEAN,
    _order INTEGER,
    _createdAt TIMESTAMPTZ,
    _createdBy UUID
)
RETURNS TABLE (
    "Id" UUID,
    "ProfileId" UUID,
    "ClassifierId" UUID,
    "IsRequired" BOOLEAN,
    "Order" INTEGER,
    "CreatedAt" TIMESTAMPTZ,
    "UpdatedAt" TIMESTAMPTZ,
    "CreatedBy" UUID,
    "UpdatedBy" UUID
)
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO classifier_profile_member (
        id,
        profile_id,
        classifier_id,
        is_required,
        "order",
        created_at,
        created_by
    )
    VALUES (
        gen_random_uuid(),
        _profileId,
        _classifierId,
        _isRequired,
        _order,
        _createdAt,
        _createdBy
    )
    RETURNING
        id AS "Id",
        profile_id AS "ProfileId",
        classifier_id AS "ClassifierId",
        is_required AS "IsRequired",
        "order" AS "Order",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy";
END;
$$ LANGUAGE plpgsql;
