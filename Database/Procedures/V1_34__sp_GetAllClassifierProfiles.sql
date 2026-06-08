CREATE OR REPLACE FUNCTION sp_GetAllClassifierProfiles(
    _companyId UUID DEFAULT NULL,
    _source VARCHAR DEFAULT NULL,
    _isActive BOOLEAN DEFAULT NULL
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
    WHERE (_companyId IS NULL OR company_id = _companyId)
      AND (_source IS NULL OR source = _source)
      AND (_isActive IS NULL OR is_active = _isActive)
    ORDER BY "order", name;
END;
$$ LANGUAGE plpgsql;
