using Dapper;
using Npgsql;
using Omadeas.Classifier.Core.Interfaces;

namespace Omadeas.Classifier.Infrastructure.Utilities;

public class DapperWrapper : IDapperWrapper
{
    private readonly string _connectionString;

    public DapperWrapper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryAsync<T>(sql, param);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QuerySingleOrDefaultAsync<T>(sql, param);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteAsync(sql, param);
    }
}
