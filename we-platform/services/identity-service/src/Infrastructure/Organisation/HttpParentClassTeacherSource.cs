using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityService.Application.Auth;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Infrastructure.Organisation;

public sealed class HttpParentClassTeacherSource(HttpClient httpClient, IConfiguration configuration)
    : IParentClassTeacherSource
{
    public async Task<IReadOnlyList<string>> ListTeacherUserIdsAsync(
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Organisation:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Organisation base URL is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/parents/me/class-teachers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Organisation teacher lookup failed ({(int)response.StatusCode}).");
        }

        var teacherIds = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken);
        return teacherIds ?? [];
    }
}
