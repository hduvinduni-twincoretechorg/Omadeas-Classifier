CREATE OR REPLACE FUNCTION sp_AddClassifier(
    _companyId UUID,
    _source VARCHAR,
    _code VARCHAR,
    _name VARCHAR,
    _description TEXT,
    _order INTEGER,
    _appliesToNodeTypes JSONB,
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
    INSERT INTO classifier (
        id,
        company_id,
        source,
        code,
        name,
        description,
        "order",
        applies_to_node_types,
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
        _appliesToNodeTypes,
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
        applies_to_node_types::text AS "AppliesToNodeTypes",
        is_active AS "IsActive",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy";
END;
$$ LANGUAGE plpgsql;
