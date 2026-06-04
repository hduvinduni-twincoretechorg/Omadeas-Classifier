CREATE OR REPLACE FUNCTION sp_GetClassifierProfileMembers(
    _profileId UUID
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
    SELECT
        id AS "Id",
        profile_id AS "ProfileId",
        classifier_id AS "ClassifierId",
        is_required AS "IsRequired",
        "order" AS "Order",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy"
    FROM classifier_profile_member
    WHERE profile_id = _profileId
    ORDER BY "order";
END;
$$ LANGUAGE plpgsql;
