CREATE OR REPLACE FUNCTION sp_AddClassifierProfile(
    _companyId UUID,
    _source VARCHAR,
    _code VARCHAR,
    _name VARCHAR,
    _description TEXT,
    _order INTEGER,
    _scopeDefault VARCHAR,
    _createdAt TIMESTAMPTZ,
    _createdBy UUID
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
    INSERT INTO classifier_profile (
        id,
        company_id,
        source,
        code,
        name,
        description,
        "order",
        scope_default,
        created_at,
        created_by
    )
    VALUES (
        gen_random_uuid(),
        _companyId,
        _source,
        _code,
        _name,
        _description,
        _order,
        _scopeDefault,
        _createdAt,
        _createdBy
    )
    RETURNING
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
        updated_by AS "UpdatedBy";
END;
$$ LANGUAGE plpgsql;
