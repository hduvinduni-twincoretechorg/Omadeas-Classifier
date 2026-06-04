using System.Text.Json;
using Dapper;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Repositories;

public class ClassifierRepository : IClassifierRepository
{
    private readonly IDapperWrapper _dapper;

    public ClassifierRepository(IDapperWrapper dapperWrapper)
    {
        _dapper = dapperWrapper;
    }

    public async Task<IEnumerable<ClassifierDto>> GetAllClassifiersAsync(Guid? companyId, string? source, bool? isActive)
    {
        const string storedProcedure = "SELECT * FROM sp_GetAllClassifiers(@CompanyId, @Source, @IsActive)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("CompanyId", companyId);
        parameters.Add("Source", source);
        parameters.Add("IsActive", isActive);
        return await _dapper.QueryAsync<ClassifierDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierDto?> GetClassifierByIdAsync(Guid id)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierById(@Id)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        return await _dapper.QuerySingleOrDefaultAsync<ClassifierDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierDto> AddClassifierAsync(ClassifierDto classifier)
    {
        const string storedProcedure = "SELECT * FROM sp_AddClassifier(" +
            "@CompanyId, " +
            "@Source, " +
            "@Code, " +
            "@Name, " +
            "@Description, " +
            "@Order, " +
            "@AppliesToNodeTypes::jsonb, " +
            "@CreatedAt, " +
            "@CreatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("CompanyId", classifier.CompanyId);
        parameters.Add("Source", classifier.Source);
        parameters.Add("Code", classifier.Code);
        parameters.Add("Name", classifier.Name);
        parameters.Add("Description", classifier.Description);
        parameters.Add("Order", classifier.Order);
        parameters.Add("AppliesToNodeTypes", SerializeNodeTypes(classifier.AppliesToNodeTypes));
        parameters.Add("CreatedAt", DateTime.UtcNow);
        parameters.Add("CreatedBy", classifier.CreatedBy);
        ClassifierDto? created = await _dapper.QuerySingleOrDefaultAsync<ClassifierDto>(storedProcedure, parameters);
        return created!;
    }

    public async Task UpdateClassifierAsync(ClassifierDto classifier)
    {
        const string storedProcedure = "CALL sp_UpdateClassifier(" +
            "@Id, " +
            "@Name, " +
            "@Description, " +
            "@Order, " +
            "@AppliesToNodeTypes::jsonb, " +
            "@UpdatedAt, " +
            "@UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", classifier.Id);
        parameters.Add("Name", classifier.Name);
        parameters.Add("Description", classifier.Description);
        parameters.Add("Order", classifier.Order);
        parameters.Add("AppliesToNodeTypes", SerializeNodeTypes(classifier.AppliesToNodeTypes));
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", classifier.UpdatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }

    public async Task SetClassifierActiveAsync(Guid id, bool isActive, Guid? updatedBy)
    {
        const string storedProcedure = "CALL sp_SetClassifierActive(@Id, @IsActive, @UpdatedAt, @UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        parameters.Add("IsActive", isActive);
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", updatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }

    public async Task<IEnumerable<ClassifierDto>> FindClassifiersByCodeOrNameAsync(Guid companyId, string code, string name)
    {
        const string storedProcedure = "SELECT * FROM sp_FindClassifiersByCodeOrName(@CompanyId, @Code, @Name)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("CompanyId", companyId);
        parameters.Add("Code", code);
        parameters.Add("Name", name);
        return await _dapper.QueryAsync<ClassifierDto>(storedProcedure, parameters);
    }

    private static string? SerializeNodeTypes(List<string>? nodeTypes)
        => nodeTypes is null ? null : JsonSerializer.Serialize(nodeTypes);
}
