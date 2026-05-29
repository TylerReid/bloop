using Bloop.Core;
using TUnit.Assertions.Extensions;

namespace Bloop.Core.Tests;

public class RequestHttpStringTests
{
    [Test]
    public async Task RoundTrip_MethodAndUri_Preserved()
    {
        var original = new Request { Method = HttpMethod.Post, Uri = "https://example.com/api" };

        var roundTripped = Request.FromHttpString(original.ToHttpString());

        await Assert.That(roundTripped.Method).IsEqualTo(original.Method);
        await Assert.That(roundTripped.Uri).IsEqualTo(original.Uri);
    }

    [Test]
    public async Task RoundTrip_SingleHeader_Preserved()
    {
        var original = new Request
        {
            Uri = "https://example.com",
            Headers = new() { ["Authorization"] = "Bearer token123" },
        };

        var roundTripped = Request.FromHttpString(original.ToHttpString());

        await Assert.That(roundTripped.Headers).ContainsKey("Authorization");
        await Assert.That(roundTripped.Headers["Authorization"]).IsEqualTo("Bearer token123");
    }

    [Test]
    public async Task RoundTrip_MultipleHeaders_Preserved()
    {
        var original = new Request
        {
            Uri = "https://example.com",
            Headers = new()
            {
                ["Content-Type"] = "application/json",
                ["X-Request-Id"] = "abc-123",
                ["Accept"] = "application/json",
            },
        };

        var roundTripped = Request.FromHttpString(original.ToHttpString());

        await Assert.That(roundTripped.Headers.Count).IsEqualTo(3);
        await Assert.That(roundTripped.Headers["Content-Type"]).IsEqualTo("application/json");
        await Assert.That(roundTripped.Headers["X-Request-Id"]).IsEqualTo("abc-123");
        await Assert.That(roundTripped.Headers["Accept"]).IsEqualTo("application/json");
    }

    [Test]
    public async Task RoundTrip_PlainTextBody_Preserved()
    {
        var original = new Request
        {
            Uri = "https://example.com",
            Body = "hello world",
        };

        var roundTripped = Request.FromHttpString(original.ToHttpString());

        await Assert.That(roundTripped.Body).IsEqualTo("hello world");
    }

    [Test]
    public async Task RoundTrip_NoBody_IsNull()
    {
        var original = new Request { Uri = "https://example.com" };

        var roundTripped = Request.FromHttpString(original.ToHttpString());

        await Assert.That(roundTripped.Body).IsNull();
    }

    [Test]
    public async Task RoundTrip_HeadersAndBody_BothPreserved()
    {
        var original = new Request
        {
            Method = HttpMethod.Put,
            Uri = "https://api.example.com/items/42",
            Headers = new() { ["Content-Type"] = "text/plain" },
            Body = "updated value",
        };

        var roundTripped = Request.FromHttpString(original.ToHttpString());

        await Assert.That(roundTripped.Method).IsEqualTo(original.Method);
        await Assert.That(roundTripped.Uri).IsEqualTo(original.Uri);
        await Assert.That(roundTripped.Headers["Content-Type"]).IsEqualTo("text/plain");
        await Assert.That(roundTripped.Body).IsEqualTo("updated value");
    }

    [Test]
    public async Task RoundTrip_NoHeaders_EmptyDictionary()
    {
        var original = new Request { Uri = "https://example.com", Body = "payload" };

        var roundTripped = Request.FromHttpString(original.ToHttpString());

        await Assert.That(roundTripped.Headers.Count).IsEqualTo(0);
        await Assert.That(roundTripped.Body).IsEqualTo("payload");
    }

    [Test]
    public async Task RoundTrip_FormBody_PreservedAsBody()
    {
        var original = new Request
        {
            Uri = "https://example.com/login",
            Form = new() { ["username"] = "alice", ["password"] = "secret" },
        };

        var roundTripped = Request.FromHttpString(original.ToHttpString());

        await Assert.That(roundTripped.Body).IsNotNull();
        await Assert.That(roundTripped.Body!.Contains("username=alice")).IsTrue();
        await Assert.That(roundTripped.Body!.Contains("password=secret")).IsTrue();
    }

    [Test]
    public async Task RoundTrip_AllHttpMethods()
    {
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Patch })
        {
            var original = new Request { Method = method, Uri = "https://example.com" };
            var roundTripped = Request.FromHttpString(original.ToHttpString());
            await Assert.That(roundTripped.Method).IsEqualTo(method);
        }
    }
}
