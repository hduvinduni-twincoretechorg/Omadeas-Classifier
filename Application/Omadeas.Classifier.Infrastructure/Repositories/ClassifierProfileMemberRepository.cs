using Dapper;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Repositories;

public class ClassifierProfileMemberRepository : IClassifierProfileMemberRepository
{
    private readonly IDapperWrapper _dapper;

    public ClassifierProfileMemberRepository(IDapperWrapper dapperWrapper)
    {
        _dapper = dapperWrapper;
    }

    public async Task<IEnumerable<ClassifierProfileMemberDto>> GetClassifierProfileMembersAsync(Guid profileId)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierProfileMembers(@ProfileId)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ProfileId", profileId);
        return await _dapper.QueryAsync<ClassifierProfileMemberDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierProfileMemberDto?> GetClassifierProfileMemberByIdAsync(Guid id)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierProfileMemberById(@Id)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        return await _dapper.QuerySingleOrDefaultAsync<ClassifierProfileMemberDto>(storedProcedure, parameters);
    }

    public async Task<ClassifierProfileMemberDto> AddClassifierProfileMemberAsync(ClassifierProfileMemberDto member)
    {
        const string storedProcedure = "SELECT * FROM sp_AddClassifierProfileMember(" +
            "@ProfileId, " +
            "@ClassifierId, " +
            "@IsRequired, " +
            "@Order, " +
            "@CreatedAt, " +
            "@CreatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ProfileId", member.ProfileId);
        parameters.Add("ClassifierId", member.ClassifierId);
        parameters.Add("IsRequired", member.IsRequired);
        parameters.Add("Order", member.Order);
        parameters.Add("CreatedAt", DateTime.UtcNow);
        parameters.Add("CreatedBy", member.CreatedBy);
        ClassifierProfileMemberDto? created = await _dapper.QuerySingleOrDefaultAsync<ClassifierProfileMemberDto>(storedProcedure, parameters);
        return created!;
    }

    public async Task UpdateClassifierProfileMemberAsync(ClassifierProfileMemberDto member)
    {
        const string storedProcedure = "CALL sp_UpdateClassifierProfileMember(" +
            "@Id, " +
            "@IsRequired, " +
            "@Order, " +
            "@UpdatedAt, " +
            "@UpdatedBy)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", member.Id);
        parameters.Add("IsRequired", member.IsRequired);
        parameters.Add("Order", member.Order);
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("UpdatedBy", member.UpdatedBy);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }

    public async Task DeleteClassifierProfileMemberAsync(Guid id)
    {
        const string storedProcedure = "CALL sp_DeleteClassifierProfileMember(@Id)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("Id", id);
        await _dapper.ExecuteAsync(storedProcedure, parameters);
    }
}
