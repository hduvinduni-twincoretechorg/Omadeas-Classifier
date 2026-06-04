using Dapper;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Repositories;

public class ClassifierProfileHistoryRepository : IClassifierProfileHistoryRepository
{
    private readonly IDapperWrapper _dapper;

    public ClassifierProfileHistoryRepository(IDapperWrapper dapperWrapper)
    {
        _dapper = dapperWrapper;
    }

    public async Task<ClassifierProfileHistoryDto> AddClassifierProfileHistoryAsync(ClassifierProfileHistoryDto history)
    {
        const string storedProcedure = "SELECT * FROM sp_AddClassifierProfileHistory(" +
            "@ProfileId, " +
            "@Operation, " +
            "@OccurredBy, " +
            "@Summary, " +
            "@Detail, " +
            "@Payload::jsonb, " +
            "@OccurredAt)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ProfileId", history.ProfileId);
        parameters.Add("Operation", history.Operation);
        parameters.Add("OccurredBy", history.OccurredBy);
        parameters.Add("Summary", history.Summary);
        parameters.Add("Detail", history.Detail);
        parameters.Add("Payload", history.Payload);
        parameters.Add("OccurredAt", history.OccurredAt == default ? DateTime.UtcNow : history.OccurredAt);
        ClassifierProfileHistoryDto? created = await _dapper.QuerySingleOrDefaultAsync<ClassifierProfileHistoryDto>(storedProcedure, parameters);
        return created!;
    }

    public async Task<IEnumerable<ClassifierProfileHistoryDto>> GetClassifierProfileHistoryAsync(Guid profileId)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierProfileHistory(@ProfileId)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ProfileId", profileId);
        return await _dapper.QueryAsync<ClassifierProfileHistoryDto>(storedProcedure, parameters);
    }
}
