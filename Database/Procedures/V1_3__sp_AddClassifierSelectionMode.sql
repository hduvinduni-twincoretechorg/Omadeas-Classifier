CREATE OR REPLACE FUNCTION sp_AddClassifierSelectionMode(
    _companyId UUID,
    _code VARCHAR,
    _name VARCHAR,
    _description TEXT,
    _allowsMultiple BOOLEAN,
    _createdAt TIMESTAMPTZ,
    _createdBy UUID
)
RETURNS TABLE (
    "Id" UUID,
    "CompanyId" UUID,
    "Code" VARCHAR,
    "Name" VARCHAR,
    "Description" TEXT,
    "AllowsMultiple" BOOLEAN,
    "IsActive" BOOLEAN,
    "CreatedAt" TIMESTAMPTZ,
    "UpdatedAt" TIMESTAMPTZ,
    "CreatedBy" UUID,
    "UpdatedBy" UUID
)
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO classifier_selection_mode (
        id,
        company_id,
        code,
        name,
        description,
        allows_multiple,
        created_at,
        created_by
    )
    VALUES (
        gen_random_uuid(),
        _companyId,
        _code,
        _name,
        _description,
        _allowsMultiple,
        _createdAt,
        _createdBy
    )
    RETURNING
        id AS "Id",
        company_id AS "CompanyId",
        code AS "Code",
        name AS "Name",
        description AS "Description",
        allows_multiple AS "AllowsMultiple",
        is_active AS "IsActive",
        created_at AS "CreatedAt",
        updated_at AS "UpdatedAt",
        created_by AS "CreatedBy",
        updated_by AS "UpdatedBy";
END;
$$ LANGUAGE plpgsql;
