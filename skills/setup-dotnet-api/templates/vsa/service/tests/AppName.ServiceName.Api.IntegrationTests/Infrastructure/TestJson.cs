using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.IntegrationTests.Infrastructure;

/// <summary>Matches the serializer the API configures in <c>ConfigureHttpJsonOptions</c>.</summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Fails with the response body included. A bare EnsureSuccessStatusCode hides the
    /// ProblemDetails the API returned, which is the only useful part of the failure.
    /// </summary>
    public static async Task<HttpResponseMessage> EnsureSuccessAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var body = await response.Content.ReadAsStringAsync();

        throw new HttpRequestException(
            $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} " +
            $"returned {(int)response.StatusCode} {response.StatusCode}: {body}");
    }

    /// <summary>Checks the status and the ProblemDetails type of an error response, and returns the problem.</summary>
    public static async Task<ProblemDetails> ShouldBeProblemAsync(
        this HttpResponseMessage response,
        HttpStatusCode status,
        string type)
    {
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(status, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json", body);

        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, Options)!;

        problem.Type.Should().Be(type, body);
        problem.Status.Should().Be((int)status, body);

        return problem;
    }

    public static Task<T?> ReadAsync<T>(this HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<T>(Options);

    /// <summary>Reads the body and fails the call when the API returned null.</summary>
    public static async Task<T> ReadRequiredAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Options);

        return value ?? throw new InvalidOperationException(
            $"Expected a {typeof(T).Name} body but the response was empty. " +
            $"Status: {(int)response.StatusCode}.");
    }

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PostAsJsonAsync(url, body, Options);

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PutAsJsonAsync(url, body, Options);

    public static Task<HttpResponseMessage> PatchJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PatchAsJsonAsync(url, body, Options);
}
