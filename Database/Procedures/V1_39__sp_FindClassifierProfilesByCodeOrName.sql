-- Uniqueness pre-check: returns profiles in the company whose code OR name matches.
CREATE OR REPLACE FUNCTION sp_FindClassifierProfilesByCodeOrName(
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
    "ScopeDefault" VARCHAR,
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
        scope_default AS "ScopeDefault",
        is_active AS "IsActive",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy"
    FROM classifier_profile
    WHERE company_id = _companyId
      AND (code = _code OR name = _name);
END;
$$ LANGUAGE plpgsql;
