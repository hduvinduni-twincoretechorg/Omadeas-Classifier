using Dapper;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Repositories;

public class ClassifierProfileRepository : IClassifierProfileRepository
{
    private readonly IDapperWrapper _dapper;

    public ClassifierProfileRepository(IDapperWrapper dapperWrapper)
    {
        _dapper = dapperWrapper;
    }

    public async Task<IEnumerable<ClassifierProfileDto>> GetAllClassifierProfilesAsync(Guid? companyId, string? source, bool? isActive)
    {
        const string storedProcedure = "SELECT * FROM sp_GetAllClassifierProfiles(@CompanyId, @Source, @IsActive)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("CompanyId", companyId);
        parameters.Add("Source", source);
        parameters.Add("IsActive", isActive);
        return await _dapper.QueryAsync<ClassifierProfileDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierProfileDto?> GetClassifierProfileByIdAsync(Guid id)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierProfileById(@Id)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        return await _dapper.QuerySingleOrDefaultAsync<ClassifierProfileDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierProfileDto> AddClassifierProfileAsync(ClassifierProfileDto profile)
    {
        const string storedProcedure = "SELECT * FROM sp_AddClassifierProfile(" +
            "@CompanyId, " +
            "@Source, " +
            "@Code, " +
            "@Name, " +
            "@Description, " +
            "@Order, " +
            "@ScopeDefault, " +
            "@CreatedAt, " +
            "@CreatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("CompanyId", profile.CompanyId);
        parameters.Add("Source", profile.Source);
        parameters.Add("Code", profile.Code);
        parameters.Add("Name", profile.Name);
        parameters.Add("Description", profile.Description);
        parameters.Add("Order", profile.Order);
        parameters.Add("ScopeDefault", profile.ScopeDefault);
        parameters.Add("CreatedAt", DateTime.UtcNow);
        parameters.Add("CreatedBy", profile.CreatedBy);
        ClassifierProfileDto? created = await _dapper.QuerySingleOrDefaultAsync<ClassifierProfileDto>(storedProcedure, parameters);
        return created!;
    }

    public async Task UpdateClassifierProfileAsync(ClassifierProfileDto profile)
    {
        const string storedProcedure = "CALL sp_UpdateClassifierProfile(" +
            "@Id, " +
            "@Name, " +
            "@Description, " +
            "@Order, " +
            "@ScopeDefault, " +
            "@UpdatedAt, " +
            "@UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", profile.Id);
        parameters.Add("Name", profile.Name);
        parameters.Add("Description", profile.Description);
        parameters.Add("Order", profile.Order);
        parameters.Add("ScopeDefault", profile.ScopeDefault);
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", profile.UpdatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }

    public async Task SetClassifierProfileActiveAsync(Guid id, bool isActive, Guid? updatedBy)
    {
        const string storedProcedure = "CALL sp_SetClassifierProfileActive(@Id, @IsActive, @UpdatedAt, @UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        parameters.Add("IsActive", isActive);
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", updatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }

    public async Task<IEnumerable<ClassifierProfileDto>> FindClassifierProfilesByCodeOrNameAsync(Guid companyId, string code, string name)
    {
        const string storedProcedure = "SELECT * FROM sp_FindClassifierProfilesByCodeOrName(@CompanyId, @Code, @Name)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("CompanyId", companyId);
        parameters.Add("Code", code);
        parameters.Add("Name", name);
        return await _dapper.QueryAsync<ClassifierProfileDto>(storedProcedure, parameters);
    }
}
