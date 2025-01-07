using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Aliyun;
using Serilog.Sinks.Aliyun.Batched;

namespace Serilog
{
    /// <summary>
    /// Extends Serilog configuration to write events to Aliyun SLS.
    /// </summary>
    public static class AliyunLoggerConfigurationExtensions
    {
        private const int DefaultBatchPostingLimit = 1000;
        private static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(2);
        private const int DefaultQueueSizeLimit = 100000;
        private const int DefaultRequestTimeout = 1000;

        /// <summary>
        /// Write log events to a Aliyun SLS server.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger configuration.</param>
        /// <param name="endpoint">SLS <see href="https://help.aliyun.com/zh/sls/developer-reference/endpoints">endpoint</see></param>
        /// <param name="project">SLS Project <see href="https://help.aliyun.com/zh/sls/developer-reference/data-model"/></param>
        /// <param name="logStore">SLS LogStore</param>
        /// <param name="topic">SLS Topic</param>
        /// <param name="source">SLS Source</param>
        /// <param name="accessKeyId">Aliyun AccessKeyId</param>
        /// <param name="accessKeySecret">Aliyun AccessKeySecret</param>
        /// <param name="logMessageTemplate"></param>
        /// <param name="requestTimeout">Timeout in milliseconds, when sending request to SLS</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required 
        /// in order to write an event to the sink.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="controlLevelSwitch">If provided, controls the minimum log event level. Do not specify <paramref name="restrictedToMinimumLevel"/> with this setting.</param>
        /// <param name="queueSizeLimit">The maximum number of events that will be held in-memory while waiting to ship them to
        /// SLS. Beyond this limit, events will be dropped. The default is 100,000.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration Aliyun(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string accessKeyId,
            string accessKeySecret,
            string endpoint,
            string project,
            string logStore,
            string? topic = null,
            string? source = null,
            bool logMessageTemplate = true,
            int requestTimeout = DefaultRequestTimeout,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = DefaultBatchPostingLimit,
            TimeSpan? period = null,
            LoggingLevelSwitch? controlLevelSwitch = null,
            int queueSizeLimit = DefaultQueueSizeLimit)
        {
            if (queueSizeLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(queueSizeLimit), "Queue size limit must be non-zero.");

            var defaultedPeriod = period ?? DefaultPeriod;
            var controlledSwitch = new ControlledLevelSwitch(controlLevelSwitch);

            var batchedSink = new BatchedAliyunSink(
                accessKeyId,
                accessKeySecret,
                endpoint,
                project,
                logStore,
                topic,
                source,
                logMessageTemplate,
                requestTimeout);

            var options = new BatchingOptions
            {
                BatchSizeLimit = batchPostingLimit,
                BufferingTimeLimit = defaultedPeriod,
                QueueLimit = queueSizeLimit
            };

            return loggerSinkConfiguration.Conditional(
                controlledSwitch.IsIncluded,
                wt => wt.Sink(batchedSink, options, restrictedToMinimumLevel, levelSwitch: null));
        }
    }
}