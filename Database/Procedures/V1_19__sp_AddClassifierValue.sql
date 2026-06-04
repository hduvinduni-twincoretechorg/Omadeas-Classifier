CREATE OR REPLACE FUNCTION sp_AddClassifierValue(
    _classifierId UUID,
    _parentValueId UUID,
    _code VARCHAR,
    _name VARCHAR,
    _description TEXT,
    _order INTEGER,
    _colour VARCHAR,
    _icon VARCHAR,
    _createdAt TIMESTAMPTZ,
    _createdBy UUID
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
    INSERT INTO classifier_value (
        id,
        classifier_id,
        parent_value_id,
        code,
        name,
        description,
        "order",
        colour,
        icon,
        created_at,
        created_by
    )
    VALUES (
        gen_random_uuid(),
        _classifierId,
        _parentValueId,
        _code,
        _name,
        _description,
        _order,
        _colour,
        _icon,
        _createdAt,
        _createdBy
    )
    RETURNING
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
        updated_by AS "UpdatedBy";
END;
$$ LANGUAGE plpgsql;
