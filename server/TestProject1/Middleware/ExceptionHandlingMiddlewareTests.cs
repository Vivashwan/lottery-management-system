using System.Text.Json;
using ex1.Exceptions;
using ex1.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace TestProject1.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, JsonElement Body)> InvokeWith(Exception? toThrow)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/lottery/raffle/1";
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => toThrow is null ? Task.CompletedTask : throw toThrow,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = context.Response.Body.Length == 0
            ? default
            : await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        return (context.Response.StatusCode, body);
    }

    [Theory]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(BusinessRuleException), 400)]
    public async Task Maps_typed_exceptions_to_status_codes_with_their_message(Type exceptionType, int expectedStatus)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Prize 1 has already been drawn.")!;

        var (status, body) = await InvokeWith(exception);

        status.Should().Be(expectedStatus);
        body.GetProperty("status").GetInt32().Should().Be(expectedStatus);
        body.GetProperty("detail").GetString().Should().Be("Prize 1 has already been drawn.");
        body.GetProperty("instance").GetString().Should().Be("/api/lottery/raffle/1");
    }

    [Fact]
    public async Task Hides_details_of_unexpected_errors()
    {
        var (status, body) = await InvokeWith(new InvalidOperationException("connection string: Password=secret"));

        status.Should().Be(500);
        body.TryGetProperty("detail", out var detail).Should().BeFalse(
            because: "internal error messages may contain sensitive data, but got {0}", detail);
    }

    [Fact]
    public async Task Leaves_successful_responses_untouched()
    {
        var (status, _) = await InvokeWith(null);

        status.Should().Be(200);
    }
}
