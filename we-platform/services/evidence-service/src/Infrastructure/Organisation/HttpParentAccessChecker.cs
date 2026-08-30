using System.Net.Http.Headers;
using EvidenceService.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EvidenceService.Infrastructure.Organisation;

public sealed class HttpParentAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpParentAccessChecker> logger) : IParentAccessChecker
{
    public async Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Organisation:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Organisation base URL is not configured.");
            return false;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/access/parent/{Uri.EscapeDataString(parentUserId)}/student/{Uri.EscapeDataString(studentUserId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
