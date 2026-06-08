-- Uniqueness pre-check within a classifier: returns values whose code OR name matches.
-- The service inspects the result to raise a clear 400 before hitting the UNIQUE constraints.
CREATE OR REPLACE FUNCTION sp_FindClassifierValuesByCodeOrName(
    _classifierId UUID,
    _code VARCHAR,
    _name VARCHAR
)
RETURNS TABLE (
    "Id" UUID,
    "ClassifierId" UUID,
    "ParentValueId" UUID,
    "Code" VARCHAR,
    "Name" VARCHAR,
    "Description" TEXT,
    "Order" INTEGER,
    "Colour" VARCHAR,
    "Icon" VARCHAR,
    "IsActive" BOOLEAN,
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
        parent_value_id AS "ParentValueId",
        code AS "Code",
        name AS "Name",
        description AS "Description",
        "order" AS "Order",
        colour AS "Colour",
        icon AS "Icon",
        is_active AS "IsActive",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy"
    FROM classifier_value
    WHERE classifier_id = _classifierId
      AND (code = _code OR name = _name);
END;
$$ LANGUAGE plpgsql;
