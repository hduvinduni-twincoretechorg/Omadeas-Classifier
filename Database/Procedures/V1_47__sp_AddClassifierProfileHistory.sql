CREATE OR REPLACE FUNCTION sp_AddClassifierProfileHistory(
    _profileId UUID,
    _operation VARCHAR,
    _occurredBy UUID,
    _summary VARCHAR,
    _detail TEXT,
    _payload JSONB,
    _occurredAt TIMESTAMPTZ
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
    INSERT INTO classifier_profile_history (
        id,
        profile_id,
        operation,
        occurred_at,
        occurred_by,
        summary,
        detail,
        payload,
        created_by
    )
    VALUES (
        gen_random_uuid(),
        _profileId,
        _operation,
        COALESCE(_occurredAt, now()),
        _occurredBy,
        _summary,
        _detail,
        _payload,
        _occurredBy
    )
    RETURNING
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
        updated_by AS "UpdatedBy";
END;
$$ LANGUAGE plpgsql;
