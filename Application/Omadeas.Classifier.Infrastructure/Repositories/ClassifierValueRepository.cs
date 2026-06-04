using Dapper;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Repositories;

public class ClassifierValueRepository : IClassifierValueRepository
{
    private readonly IDapperWrapper _dapper;

    public ClassifierValueRepository(IDapperWrapper dapperWrapper)
    {
        _dapper = dapperWrapper;
    }

    public async Task<IEnumerable<ClassifierValueDto>> GetClassifierValuesByClassifierAsync(Guid classifierId, bool? isActive)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierValuesByClassifier(@ClassifierId, @IsActive)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ClassifierId", classifierId);
        parameters.Add("IsActive", isActive);
        return await _dapper.QueryAsync<ClassifierValueDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierValueDto?> GetClassifierValueByIdAsync(Guid id)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierValueById(@Id)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        return await _dapper.QuerySingleOrDefaultAsync<ClassifierValueDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierValueDto> AddClassifierValueAsync(ClassifierValueDto value)
    {
        const string storedProcedure = "SELECT * FROM sp_AddClassifierValue(" +
            "@ClassifierId, " +
            "@ParentValueId, " +
            "@Code, " +
            "@Name, " +
            "@Description, " +
            "@Order, " +
            "@Colour, " +
            "@Icon, " +
            "@CreatedAt, " +
            "@CreatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ClassifierId", value.ClassifierId);
        parameters.Add("ParentValueId", value.ParentValueId);
        parameters.Add("Code", value.Code);
        parameters.Add("Name", value.Name);
        parameters.Add("Description", value.Description);
        parameters.Add("Order", value.Order);
        parameters.Add("Colour", value.Colour);
        parameters.Add("Icon", value.Icon);
        parameters.Add("CreatedAt", DateTime.UtcNow);
        parameters.Add("CreatedBy", value.CreatedBy);
        ClassifierValueDto? created = await _dapper.QuerySingleOrDefaultAsync<ClassifierValueDto>(storedProcedure, parameters);
        return created!;
    }

    public async Task UpdateClassifierValueAsync(ClassifierValueDto value)
    {
        const string storedProcedure = "CALL sp_UpdateClassifierValue(" +
            "@Id, " +
            "@Name, " +
            "@Description, " +
            "@Order, " +
            "@Colour, " +
            "@Icon, " +
            "@UpdatedAt, " +
            "@UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", value.Id);
        parameters.Add("Name", value.Name);
        parameters.Add("Description", value.Description);
        parameters.Add("Order", value.Order);
        parameters.Add("Colour", value.Colour);
        parameters.Add("Icon", value.Icon);
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", value.UpdatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }

    public async Task SetClassifierValueActiveAsync(Guid id, bool isActive, Guid? updatedBy)
    {
        const string storedProcedure = "CALL sp_SetClassifierValueActive(@Id, @IsActive, @UpdatedAt, @UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        parameters.Add("IsActive", isActive);
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", updatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }

    public async Task<IEnumerable<ClassifierValueDto>> FindClassifierValuesByCodeOrNameAsync(Guid classifierId, string code, string name)
    {
        const string storedProcedure = "SELECT * FROM sp_FindClassifierValuesByCodeOrName(@ClassifierId, @Code, @Name)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ClassifierId", classifierId);
        parameters.Add("Code", code);
        parameters.Add("Name", name);
        return await _dapper.QueryAsync<ClassifierValueDto>(storedProcedure, parameters);
    }
}
