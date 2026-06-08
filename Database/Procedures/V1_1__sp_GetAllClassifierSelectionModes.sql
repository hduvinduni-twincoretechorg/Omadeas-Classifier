CREATE OR REPLACE FUNCTION sp_GetAllClassifierSelectionModes(
    _isActive BOOLEAN DEFAULT NULL
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
    SELECT
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
        updated_by AS "UpdatedBy"
    FROM classifier_selection_mode
    WHERE (_isActive IS NULL OR is_active = _isActive)
    ORDER BY code;
END;
$$ LANGUAGE plpgsql;
