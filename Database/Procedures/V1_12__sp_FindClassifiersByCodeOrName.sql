-- Uniqueness pre-check: returns any classifiers in the company whose code OR name matches.
-- The service inspects the result to raise a clear 400 before hitting the UNIQUE constraints.
CREATE OR REPLACE FUNCTION sp_FindClassifiersByCodeOrName(
    _companyId UUID,
    _code VARCHAR,
    _name VARCHAR
)
RETURNS TABLE (
    "Id" UUID,
    "CompanyId" UUID,
    "Source" VARCHAR,
    "Code" VARCHAR,
    "Name" VARCHAR,
    "Description" TEXT,
    "Order" INTEGER,
    "AppliesToNodeTypes" TEXT,
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
        company_id AS "CompanyId",
        source AS "Source",
        code AS "Code",
        name AS "Name",
        description AS "Description",
        "order" AS "Order",
        applies_to_node_types::text AS "AppliesToNodeTypes",
        is_active AS "IsActive",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy"
    FROM classifier
    WHERE company_id = _companyId
      AND (code = _code OR name = _name);
END;
$$ LANGUAGE plpgsql;
