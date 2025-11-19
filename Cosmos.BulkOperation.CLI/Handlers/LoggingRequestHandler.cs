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
