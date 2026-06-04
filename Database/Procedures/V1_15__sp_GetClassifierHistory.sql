CREATE OR REPLACE FUNCTION sp_GetClassifierHistory(
    _classifierId UUID
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
    SELECT
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
        updated_by AS "UpdatedBy"
    FROM classifier_history
    WHERE classifier_id = _classifierId
    ORDER BY occurred_at DESC;
END;
$$ LANGUAGE plpgsql;
