using Aliyun.Api.LogService;
using Aliyun.Api.LogService.Domain.Log;
using Aliyun.Api.LogService.Infrastructure.Protocol.Http;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.Aliyun.Batched;

/// <summary>
/// The default Aliyun sink, for use in combination with <see cref="PeriodicBatchingSink"/>.
/// </summary>
public sealed class BatchedAliyunSink : IBatchedLogEventSink
{
    private readonly string _logStore;
    private readonly string _topic;
    private readonly HttpLogServiceClient _client;
    private readonly string _source;

    public BatchedAliyunSink(string accessKeyId,
        string accessKeySecret,
        string endpoint,
        string project,
        string logStore,
        string? topic = null,
        string? source = null,
        int requestTimeout = 1000)
    {
        _logStore = logStore;
        _topic = topic ?? string.Empty;
        _source = source ?? string.Empty;
        _client = LogServiceClientBuilders.HttpBuilder
            .Endpoint(endpoint, project)
            .Credential(accessKeyId, accessKeySecret)
            .RequestTimeout(requestTimeout)
            .Build();
    }


    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
    {
        var logs = new List<LogInfo>();
        foreach (var logEvent in events)
        {
            var log = new LogInfo
            {
                Time = logEvent.Timestamp
            };

            log.Contents.Add("Level", logEvent.Level.ToString());
            log.Contents.Add("Message", logEvent.RenderMessage());


            if (logEvent.Exception != null)
            {
                log.Contents.Add("Exception", logEvent.Exception.ToString());
            }

            foreach (var prop in logEvent.Properties)
            {
                log.Contents.Add(prop.Key, prop.Value.ToString().Trim('"'));
            }

            logs.Add(log);
        }

        var response = await _client.PostLogStoreLogsAsync(new PostLogsRequest(_logStore, new LogGroupInfo
        {
            Topic = _topic,
            Source = _source,
            Logs = logs
        }));

        if (!response.IsSuccess)
        {
            SelfLog.WriteLine(response.Error.ErrorMessage);
        }
    }
}