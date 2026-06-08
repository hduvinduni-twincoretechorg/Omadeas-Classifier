using Dapper;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Repositories;

public class ClassifierValueConstraintRepository : IClassifierValueConstraintRepository
{
    private readonly IDapperWrapper _dapper;

    public ClassifierValueConstraintRepository(IDapperWrapper dapperWrapper)
    {
        _dapper = dapperWrapper;
    }

    public async Task<IEnumerable<ClassifierValueConstraintDto>> GetClassifierValueConstraintsAsync(Guid? classifierId, bool? isActive)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierValueConstraints(@ClassifierId, @IsActive)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ClassifierId", classifierId);
        parameters.Add("IsActive", isActive);
        return await _dapper.QueryAsync<ClassifierValueConstraintDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierValueConstraintDto?> GetClassifierValueConstraintByIdAsync(Guid id)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierValueConstraintById(@Id)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        return await _dapper.QuerySingleOrDefaultAsync<ClassifierValueConstraintDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierValueConstraintDto> AddClassifierValueConstraintAsync(ClassifierValueConstraintDto constraint)
    {
        const string storedProcedure = "SELECT * FROM sp_AddClassifierValueConstraint(" +
            "@SourceClassifierId, " +
            "@SourceValueId, " +
            "@TargetClassifierId, " +
            "@TargetValueId, " +
            "@ConstraintType, " +
            "@CreatedAt, " +
            "@CreatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("SourceClassifierId", constraint.SourceClassifierId);
        parameters.Add("SourceValueId", constraint.SourceValueId);
        parameters.Add("TargetClassifierId", constraint.TargetClassifierId);
        parameters.Add("TargetValueId", constraint.TargetValueId);
        parameters.Add("ConstraintType", constraint.ConstraintType);
        parameters.Add("CreatedAt", DateTime.UtcNow);
        parameters.Add("CreatedBy", constraint.CreatedBy);
        ClassifierValueConstraintDto? created = await _dapper.QuerySingleOrDefaultAsync<ClassifierValueConstraintDto>(storedProcedure, parameters);
        return created!;
    }

    public async Task UpdateClassifierValueConstraintAsync(ClassifierValueConstraintDto constraint)
    {
        const string storedProcedure = "CALL sp_UpdateClassifierValueConstraint(" +
            "@Id, " +
            "@ConstraintType, " +
            "@IsActive, " +
            "@UpdatedAt, " +
            "@UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", constraint.Id);
        parameters.Add("ConstraintType", constraint.ConstraintType);
        parameters.Add("IsActive", constraint.IsActive);
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", constraint.UpdatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }

    public async Task DeleteClassifierValueConstraintAsync(Guid id)
    {
        const string storedProcedure = "CALL sp_DeleteClassifierValueConstraint(@Id)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }
}
