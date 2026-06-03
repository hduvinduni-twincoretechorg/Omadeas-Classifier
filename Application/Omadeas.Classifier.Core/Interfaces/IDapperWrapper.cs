namespace Omadeas.Classifier.Core.Interfaces;

public interface IDapperWrapper
{
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null);
    Task<int> ExecuteAsync(string sql, object? param = null);
}
