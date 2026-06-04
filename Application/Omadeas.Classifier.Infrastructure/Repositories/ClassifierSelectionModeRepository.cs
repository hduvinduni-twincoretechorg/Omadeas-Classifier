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
}
