using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using io.ucedo.labs.cv.domain;

namespace io.ucedo.labs.cv;

public class CacheManager
{
    private readonly IDynamoDBContext _dynamoDBContext;
    public CacheManager(IDynamoDBContext dynamoDBContext)
    {
        _dynamoDBContext = dynamoDBContext ?? throw new ArgumentNullException(nameof(dynamoDBContext));
    }

    public async Task Add(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            key = Constants.DEFAULT;

        var cacheItem = new LabsCv
        {
            Id = key,
            Body = value.ToString()
        };

        await _dynamoDBContext.SaveAsync(cacheItem);
    }

    public async Task<bool> Contains(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.");
        }

        var cacheItem = await _dynamoDBContext.LoadAsync<LabsCv>(key);
        return cacheItem != null;
    }

    public async Task<string> Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            key = Constants.DEFAULT;

        var cacheItem = await _dynamoDBContext.LoadAsync<LabsCv>(key);
        if (cacheItem != null)
            return cacheItem.Body;

        return string.Empty;
    }

    public async Task Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            key = Constants.DEFAULT;

        await _dynamoDBContext.DeleteAsync<LabsCv>(key);
    }

    public async Task Clear()
    {
        var scanConfig = new ScanOperationConfig
        {
            Select = SelectValues.SpecificAttributes,
            AttributesToGet = new List<string> { "Id" }
        };

        var scan = _dynamoDBContext.FromScanAsync<LabsCv>(scanConfig);
        var results = await scan.GetRemainingAsync();

        foreach (var item in results)
        {
            await _dynamoDBContext.DeleteAsync<LabsCv>(item.Id);
        }
    }
}
