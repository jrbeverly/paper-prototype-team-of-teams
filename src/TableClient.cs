using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Backend;

public sealed class TableClient
{
    private readonly IAmazonDynamoDB _dynamo;
    private readonly string _tableName;

    public TableClient(IAmazonDynamoDB dynamo, IConfiguration config)
    {
        _dynamo = dynamo;
        _tableName = config["TABLE_NAME"]
            ?? throw new InvalidOperationException("TABLE_NAME environment variable is not set");
    }

    public async Task PutAsync(string pk, string sk, Dictionary<string, AttributeValue> attrs)
    {
        var item = new Dictionary<string, AttributeValue>(attrs)
        {
            ["PK"] = new AttributeValue(pk),
            ["SK"] = new AttributeValue(sk),
        };
        await _dynamo.PutItemAsync(_tableName, item);
    }

    public async Task<Dictionary<string, AttributeValue>?> GetAsync(string pk, string sk)
    {
        var response = await _dynamo.GetItemAsync(_tableName, new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue(pk),
            ["SK"] = new AttributeValue(sk),
        });
        return response.IsItemSet ? response.Item : null;
    }

    public async Task<List<Dictionary<string, AttributeValue>>> QueryAsync(
        string pk, string? skPrefix = null)
    {
        var expr = skPrefix is null ? "PK = :pk" : "PK = :pk AND begins_with(SK, :skp)";
        var values = new Dictionary<string, AttributeValue> { [":pk"] = new(pk) };
        if (skPrefix is not null)
            values[":skp"] = new(skPrefix);

        var response = await _dynamo.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = expr,
            ExpressionAttributeValues = values,
        });
        return response.Items;
    }

    public async Task DeleteAsync(string pk, string sk)
    {
        await _dynamo.DeleteItemAsync(_tableName, new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue(pk),
            ["SK"] = new AttributeValue(sk),
        });
    }

    public async Task<List<Dictionary<string, AttributeValue>>> QueryGsi1Async(
        string gsi1pk, string? gsi1skPrefix = null)
    {
        var expr = gsi1skPrefix is null
            ? "GSI1PK = :pk"
            : "GSI1PK = :pk AND begins_with(GSI1SK, :skp)";
        var values = new Dictionary<string, AttributeValue> { [":pk"] = new(gsi1pk) };
        if (gsi1skPrefix is not null)
            values[":skp"] = new(gsi1skPrefix);

        var response = await _dynamo.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI1",
            KeyConditionExpression = expr,
            ExpressionAttributeValues = values,
        });
        return response.Items;
    }

    public async Task TransactPutAsync(
        IEnumerable<(string Pk, string Sk, Dictionary<string, AttributeValue> Attrs)> items)
    {
        var writes = items.Select(i =>
        {
            var item = new Dictionary<string, AttributeValue>(i.Attrs)
            {
                ["PK"] = new AttributeValue(i.Pk),
                ["SK"] = new AttributeValue(i.Sk),
            };
            return new TransactWriteItem
            {
                Put = new Put { TableName = _tableName, Item = item }
            };
        }).ToList();

        await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems = writes,
        });
    }

    public async Task<List<Dictionary<string, AttributeValue>>> ScanByPkPrefixAsync(
        string pkPrefix, string sk)
    {
        var results = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? lastKey = null;

        do
        {
            var req = new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = "begins_with(PK, :pkp) AND SK = :sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pkp"] = new(pkPrefix),
                    [":sk"] = new(sk),
                },
            };
            if (lastKey is not null) req.ExclusiveStartKey = lastKey;

            var response = await _dynamo.ScanAsync(req);
            results.AddRange(response.Items);
            lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastKey is not null);

        return results;
    }

    public async Task<List<Dictionary<string, AttributeValue>>> ScanAsync(
        string filterExpression,
        Dictionary<string, AttributeValue> expressionValues,
        Dictionary<string, string>? expressionNames = null)
    {
        var results = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? lastKey = null;

        do
        {
            var req = new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = filterExpression,
                ExpressionAttributeValues = expressionValues,
            };
            if (expressionNames is not null)
                req.ExpressionAttributeNames = expressionNames;
            if (lastKey is not null)
                req.ExclusiveStartKey = lastKey;

            var response = await _dynamo.ScanAsync(req);
            results.AddRange(response.Items);
            lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastKey is not null);

        return results;
    }
}
