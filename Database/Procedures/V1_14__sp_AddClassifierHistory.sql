CREATE OR REPLACE FUNCTION sp_AddClassifierHistory(
    _classifierId UUID,
    _operation VARCHAR,
    _occurredBy UUID,
    _summary VARCHAR,
    _detail TEXT,
    _payload JSONB,
    _occurredAt TIMESTAMPTZ
)
RETURNS TABLE (
    "Id" UUID,
    "ClassifierId" UUID,
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
    INSERT INTO classifier_history (
        id,
        classifier_id,
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
        _classifierId,
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
        classifier_id AS "ClassifierId",
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
