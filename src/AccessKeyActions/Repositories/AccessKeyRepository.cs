using AccessKeyActions.Models;
using AccessKeyActions.Options;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessKeyActions.Repositories;

public class AccessKeyRepository : IAccessKeyRepository
{
    private readonly DynamoDBContext _context;
    private readonly DynamoDBOptions _options;
    private readonly ILogger<AccessKeyRepository> _logger;

    public AccessKeyRepository(IAmazonDynamoDB amazonDynamoDb, IOptions<DynamoDBOptions> options,
        ILogger<AccessKeyRepository> logger)
    {
        _context = new DynamoDBContext(amazonDynamoDb);
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<AccessKeyEntity> GetByIdAsync(string id)
    {
        var tableName = _options.TableName;
        _logger.LogDebug("Calling LoadAsync<> on {tableName} with hashKey={hashKey}", tableName, id);
        
        return await _context.LoadAsync<AccessKeyEntity>(id, 
            operationConfig: new DynamoDBOperationConfig { OverrideTableName = _options.TableName });
    }

    public async Task<IEnumerable<AccessKeyEntity>> GetByUsernameAsync(string username)
    {
        var (tableName, indexName) = (_options.TableName, _options.IndexName);
        _logger.LogDebug("Calling QueryAsync<> on {tableName} with hashKey={hashKey} for index={indexName}", tableName, username, indexName);

        return await _context.QueryAsync<AccessKeyEntity>(username, 
                new DynamoDBOperationConfig { OverrideTableName = tableName, IndexName = indexName })
                    .GetRemainingAsync();
    }
}