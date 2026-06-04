CREATE OR REPLACE FUNCTION sp_GetClassifierProfileHistory(
    _profileId UUID
)
RETURNS TABLE (
    "Id" UUID,
    "ProfileId" UUID,
    "Operation" VARCHAR,
    "OccurredAt" TIMESTAMPTZ,
    "OccurredBy" UUID,
    "Summary" VARCHAR,
    "Detail" TEXT,
    "Payload" TEXT,
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
        operation AS "Operation",
        occurred_at AS "OccurredAt",
        occurred_by AS "OccurredBy",
        summary AS "Summary",
        detail AS "Detail",
        payload::text AS "Payload",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy"
    FROM classifier_profile_history
    WHERE profile_id = _profileId
    ORDER BY occurred_at DESC;
END;
$$ LANGUAGE plpgsql;
