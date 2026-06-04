using Dapper;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Repositories;

public class ClassifierSelectionModeRepository : IClassifierSelectionModeRepository
{
    private readonly IDapperWrapper _dapper;

    public ClassifierSelectionModeRepository(IDapperWrapper dapperWrapper)
    {
        _dapper = dapperWrapper;
    }

    public async Task<IEnumerable<ClassifierSelectionModeDto>> GetAllClassifierSelectionModesAsync(bool? isActive)
    {
        const string storedProcedure = "SELECT * FROM sp_GetAllClassifierSelectionModes(@IsActive)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("IsActive", isActive);
        return await _dapper.QueryAsync<ClassifierSelectionModeDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierSelectionModeDto?> GetClassifierSelectionModeByIdAsync(Guid id)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierSelectionModeById(@Id)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        return await _dapper.QuerySingleOrDefaultAsync<ClassifierSelectionModeDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierSelectionModeDto> AddClassifierSelectionModeAsync(ClassifierSelectionModeDto selectionMode)
    {
        const string storedProcedure = "SELECT * FROM sp_AddClassifierSelectionMode(" +
            "@CompanyId, " +
            "@Code, " +
            "@Name, " +
            "@Description, " +
            "@AllowsMultiple, " +
            "@CreatedAt, " +
            "@CreatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("CompanyId", selectionMode.CompanyId);
        parameters.Add("Code", selectionMode.Code);
        parameters.Add("Name", selectionMode.Name);
        parameters.Add("Description", selectionMode.Description);
        parameters.Add("AllowsMultiple", selectionMode.AllowsMultiple);
        parameters.Add("CreatedAt", DateTime.UtcNow);
        parameters.Add("CreatedBy", selectionMode.CreatedBy);
        ClassifierSelectionModeDto? created =
            await _dapper.QuerySingleOrDefaultAsync<ClassifierSelectionModeDto>(storedProcedure, parameters);
        return created!;
    }

    public async Task UpdateClassifierSelectionModeAsync(ClassifierSelectionModeDto selectionMode)
    {
        const string storedProcedure = "CALL sp_UpdateClassifierSelectionMode(" +
            "@Id, " +
            "@Name, " +
            "@Description, " +
            "@AllowsMultiple, " +
            "@IsActive, " +
            "@UpdatedAt, " +
            "@UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", selectionMode.Id);
        parameters.Add("Name", selectionMode.Name);
        parameters.Add("Description", selectionMode.Description);
        parameters.Add("AllowsMultiple", selectionMode.AllowsMultiple);
        parameters.Add("IsActive", selectionMode.IsActive);
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", selectionMode.UpdatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }

    public async Task DeleteClassifierSelectionModeAsync(Guid id)
    {
        const string storedProcedure = "CALL sp_DeleteClassifierSelectionMode(@Id)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }
}
