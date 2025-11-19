using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using Serilog;

namespace Cosmos.BulkOperation.CLI.Handlers;

/// <summary>
/// Logs warnings if a Cosmos DB message doesn't return a successful HTTP status.
/// </summary>
public class LoggingRequestHandler : RequestHandler
{
    /// <summary>
    /// Sends a request and logs a warning if the response is not successful.
    /// </summary>
    /// <param name="request">The Cosmos DB request message.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response message from Cosmos DB.</returns>
    public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        ResponseMessage response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Log.Warning("Received HTTP - {@HTTPCode} for {@HTTPMethod} {@URI}", response.StatusCode, request.Method.Method, request.RequestUri);
        }

        return response;
    }
}
