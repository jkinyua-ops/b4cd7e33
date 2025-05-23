using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Nostra.DataLoad.Infrastructure
{
    public static class DatabaseRetryPolicy
    {
        private static readonly int[] SqlTransientErrors = { 
            -2, // Timeout
            4060, // Cannot open database
            40197, // Error processing request
            40501, // Service busy
            40613, // Database unavailable
            49918, // Not enough resources
            49919, // Cannot process request
            49920, // Service too busy
        };

        public static IAsyncPolicy CreateRetryPolicy(ILogger logger)
        {
            return Policy
                .Handle<SqlException>(ex => SqlTransientErrors.Contains(ex.Number))
                .Or<TimeoutException>()
                .Or<DbUpdateException>(ex => ex.InnerException is SqlException sqlEx && 
                                            SqlTransientErrors.Contains(sqlEx.Number))
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogWarning(
                            exception,
                            "Database operation failed on attempt {RetryCount}. Waiting {RetryTimeSpan} before next retry.",
                            retryCount,
                            timeSpan);
                    }
                );
        }

        public static async Task ExecuteWithRetryAsync(ILogger logger, Func<Task> operation)
        {
            var policy = CreateRetryPolicy(logger);
            await policy.ExecuteAsync(operation);
        }

        public static async Task<T> ExecuteWithRetryAsync<T>(ILogger logger, Func<Task<T>> operation)
        {
            var policy = CreateRetryPolicy(logger);
            return await policy.ExecuteAsync(operation);
        }
    }
}