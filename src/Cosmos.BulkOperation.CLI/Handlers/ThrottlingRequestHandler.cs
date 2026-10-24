using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

using Serilog;

namespace Cosmos.BulkOperation.CLI.Handlers;

/// <summary>
/// Wraps Cosmos DB messages around custom retry policies.
/// </summary>
public class ThrottlingRequestHandler : RequestHandler
{
    const string REQ_URI_KEY = "uri";
    const string REQ_METHOD_KEY = "method";

    private static readonly AsyncRetryPolicy<ResponseMessage> ExponentialRetryPolicy = Policy
            .HandleResult<ResponseMessage>(res => res.StatusCode == HttpStatusCode.TooManyRequests)
            .Or<BrokenCircuitException>()
            .Or<CosmosException>(ex => ex.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
            [
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(16),
                TimeSpan.FromSeconds(32),
            ], (_, _, retryAttempt, context) =>
            {
                const string MSG_TEMPLATE = "Too many requests. Custom retry attempt number: #{@RetryAttempt} | Req: {@Method}-{@Request}";
                context.TryGetValue(REQ_URI_KEY, out var uri);
                context.TryGetValue(REQ_METHOD_KEY, out var method);

                Log.Error(MSG_TEMPLATE, retryAttempt, method, uri);
            });

    // This policy exists to stop all attempts across all threads
    private static readonly AsyncCircuitBreakerPolicy<ResponseMessage> TooManyRequestAsyncRetryPolicy = Policy
        .HandleResult<ResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .CircuitBreakerAsync(1, TimeSpan.FromSeconds(2));

    /// <summary>
    /// Sends a request with retry and circuit breaker policies for handling throttling.
    /// </summary>
    /// <param name="request">The Cosmos DB request message.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response message from Cosmos DB.</returns>
    public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
        => await ExponentialRetryPolicy.ExecuteAsync((ctx, ct) =>
            {
                if (!ctx.Contains(REQ_URI_KEY))
                {
                    ctx.Add(REQ_URI_KEY, request.RequestUri.ToString());
                }

                if (!ctx.Contains(REQ_METHOD_KEY))
                {
                    ctx.Add(REQ_METHOD_KEY, request.Method.Method);
                }

                return TooManyRequestAsyncRetryPolicy.ExecuteAsync(() => base.SendAsync(request, ct));
            }, [], cancellationToken);
}
