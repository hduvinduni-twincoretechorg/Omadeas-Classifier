CREATE OR REPLACE FUNCTION sp_GetClassifierValuesByClassifier(
    _classifierId UUID,
    _isActive BOOLEAN DEFAULT NULL
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
      AND (_isActive IS NULL OR is_active = _isActive)
    ORDER BY "order", name;
END;
$$ LANGUAGE plpgsql;
