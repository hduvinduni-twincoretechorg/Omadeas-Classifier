using Dapper;
using Omadeas.Classifier.Core.DTOs;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Repositories;

public class ClassifierHistoryRepository : IClassifierHistoryRepository
{
    private readonly IDapperWrapper _dapper;

    public ClassifierHistoryRepository(IDapperWrapper dapperWrapper)
    {
        _dapper = dapperWrapper;
    }

    public async Task<ClassifierHistoryDto> AddClassifierHistoryAsync(ClassifierHistoryDto history)
    {
        const string storedProcedure = "SELECT * FROM sp_AddClassifierHistory(" +
            "@ClassifierId, " +
            "@Operation, " +
            "@OccurredBy, " +
            "@Summary, " +
            "@Detail, " +
            "@Payload::jsonb, " +
            "@OccurredAt)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ClassifierId", history.ClassifierId);
        parameters.Add("Operation", history.Operation);
        parameters.Add("OccurredBy", history.OccurredBy);
        parameters.Add("Summary", history.Summary);
        parameters.Add("Detail", history.Detail);
        parameters.Add("Payload", history.Payload);
        parameters.Add("OccurredAt", history.OccurredAt == default ? DateTime.UtcNow : history.OccurredAt);
        ClassifierHistoryDto? created = await _dapper.QuerySingleOrDefaultAsync<ClassifierHistoryDto>(storedProcedure, parameters);
        return created!;
    }

    public async Task<IEnumerable<ClassifierHistoryDto>> GetClassifierHistoryAsync(Guid classifierId)
    {
        const string storedProcedure = "SELECT * FROM sp_GetClassifierHistory(@ClassifierId)";
        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("ClassifierId", classifierId);
        return await _dapper.QueryAsync<ClassifierHistoryDto>(storedProcedure, parameters);
    }
}
