using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nostra.DataLoad.Infrastructure
{
    public class DatabasePerformanceInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<DatabasePerformanceInterceptor> _logger;
        private readonly TimeSpan _slowQueryThreshold;

        public DatabasePerformanceInterceptor(ILogger<DatabasePerformanceInterceptor> logger, TimeSpan? slowQueryThreshold = null)
        {
            _logger = logger;
            _slowQueryThreshold = slowQueryThreshold ?? TimeSpan.FromSeconds(3);
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithTimingAsync(
                command,
                eventData,
                () => base.ReaderExecutingAsync(command, eventData, result, cancellationToken));
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithTimingAsync(
                command,
                eventData,
                () => base.NonQueryExecutingAsync(command, eventData, result, cancellationToken));
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithTimingAsync(
                command,
                eventData,
                () => base.ScalarExecutingAsync(command, eventData, result, cancellationToken));
        }

        private async Task<TResult> ExecuteWithTimingAsync<TResult>(
            DbCommand command,
            CommandEventData eventData,
            Func<Task<TResult>> operation)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                return await operation();
            }
            finally
            {
                stopwatch.Stop();
                var elapsed = stopwatch.Elapsed;

                if (elapsed > _slowQueryThreshold)
                {
                    _logger.LogWarning(
                        "Slow SQL query detected: {ElapsedMilliseconds}ms, Command: {CommandText}, Parameters: {Parameters}",
                        elapsed.TotalMilliseconds,
                        command.CommandText,
                        string.Join(", ", command.Parameters));
                }
                else
                {
                    _logger.LogDebug(
                        "SQL query executed: {ElapsedMilliseconds}ms, Command: {CommandText}",
                        elapsed.TotalMilliseconds,
                        command.CommandText);
                }
            }
        }
    }

    public static class DatabasePerformanceLoggingExtensions
    {
        public static DbContextOptionsBuilder AddPerformanceLogging(
            this DbContextOptionsBuilder optionsBuilder,
            ILoggerFactory loggerFactory,
            TimeSpan? slowQueryThreshold = null)
        {
            var logger = loggerFactory.CreateLogger<DatabasePerformanceInterceptor>();
            optionsBuilder.AddInterceptors(new DatabasePerformanceInterceptor(logger, slowQueryThreshold));
            return optionsBuilder;
        }
    }
}