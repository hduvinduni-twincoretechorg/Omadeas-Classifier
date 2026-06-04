using Dapper;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Repositories;

public class ClassifierValuePolicyRepository : IClassifierValuePolicyRepository
{
    private readonly IDapperWrapper _dapper;

    public ClassifierValuePolicyRepository(IDapperWrapper dapperWrapper)
    {
        _dapper = dapperWrapper;
    }

    public async Task<ClassifierValuePolicyDto?> GetClassifierValuePolicyByClassifierAsync(Guid classifierId)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierValuePolicyByClassifier(@ClassifierId)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ClassifierId", classifierId);
        return await _dapper.QuerySingleOrDefaultAsync<ClassifierValuePolicyDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierValuePolicyDto> AddClassifierValuePolicyAsync(ClassifierValuePolicyDto policy)
    {
        const string storedProcedure = "SELECT * FROM sp_AddClassifierValuePolicy(" +
            "@ClassifierId, " +
            "@SelectionModeId, " +
            "@MinSelected, " +
            "@MaxSelected, " +
            "@IsRequired, " +
            "@DefaultValueId, " +
            "@AllowCustomValues, " +
            "@RequiresReasonOnChange, " +
            "@ComputationMode, " +
            "@DefaultCalculationId, " +
            "@CreatedAt, " +
            "@CreatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ClassifierId", policy.ClassifierId);
        parameters.Add("SelectionModeId", policy.SelectionModeId);
        parameters.Add("MinSelected", policy.MinSelected);
        parameters.Add("MaxSelected", policy.MaxSelected);
        parameters.Add("IsRequired", policy.IsRequired);
        parameters.Add("DefaultValueId", policy.DefaultValueId);
        parameters.Add("AllowCustomValues", policy.AllowCustomValues);
        parameters.Add("RequiresReasonOnChange", policy.RequiresReasonOnChange);
        parameters.Add("ComputationMode", policy.ComputationMode);
        parameters.Add("DefaultCalculationId", policy.DefaultCalculationId);
        parameters.Add("CreatedAt", DateTime.UtcNow);
        parameters.Add("CreatedBy", policy.CreatedBy);
        ClassifierValuePolicyDto? created = await _dapper.QuerySingleOrDefaultAsync<ClassifierValuePolicyDto>(storedProcedure, parameters);
        return created!;
    }

    public async Task UpdateClassifierValuePolicyAsync(ClassifierValuePolicyDto policy)
    {
        const string storedProcedure = "CALL sp_UpdateClassifierValuePolicy(" +
            "@ClassifierId, " +
            "@SelectionModeId, " +
            "@MinSelected, " +
            "@MaxSelected, " +
            "@IsRequired, " +
            "@DefaultValueId, " +
            "@AllowCustomValues, " +
            "@RequiresReasonOnChange, " +
            "@ComputationMode, " +
            "@DefaultCalculationId, " +
            "@UpdatedAt, " +
            "@UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ClassifierId", policy.ClassifierId);
        parameters.Add("SelectionModeId", policy.SelectionModeId);
        parameters.Add("MinSelected", policy.MinSelected);
        parameters.Add("MaxSelected", policy.MaxSelected);
        parameters.Add("IsRequired", policy.IsRequired);
        parameters.Add("DefaultValueId", policy.DefaultValueId);
        parameters.Add("AllowCustomValues", policy.AllowCustomValues);
        parameters.Add("RequiresReasonOnChange", policy.RequiresReasonOnChange);
        parameters.Add("ComputationMode", policy.ComputationMode);
        parameters.Add("DefaultCalculationId", policy.DefaultCalculationId);
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", policy.UpdatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }
}
