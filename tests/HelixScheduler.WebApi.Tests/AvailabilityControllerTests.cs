using System.Net;
using System.Text.Json;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class AvailabilityControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AvailabilityControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Missing_FromDate_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?toDate=2025-03-10&resourceIds=1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_Date_Format_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025/03/10&toDate=2025-03-10&resourceIds=1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FromDate_After_ToDate_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-11&toDate=2025-03-10&resourceIds=1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Range_Over_31_Days_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-01&toDate=2025-04-05&resourceIds=1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Range_At_31_Days_Is_Accepted()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-01&toDate=2026-03-31&resourceIds=5,4");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        Assert.NotEqual(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Missing_ResourceIds_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-10&toDate=2025-03-10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Too_Many_ResourceIds_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-10&toDate=2025-03-10&resourceIds=1,2,3,4,5,6,7,8,9,10,11");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonPositive_ResourceIds_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-10&toDate=2025-03-10&resourceIds=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_PropertyIds_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-10&toDate=2025-03-10&resourceIds=1&propertyIds=a,b");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonPositive_PropertyIds_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-10&toDate=2025-03-10&resourceIds=1&propertyIds=-2");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_Slots_With_Expected_Shape()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-10&toDate=2025-03-10&resourceIds=7,1");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.NotEqual(0, doc.RootElement.GetArrayLength());

        var slot = doc.RootElement[0];
        Assert.True(slot.TryGetProperty("startUtc", out var startUtc));
        Assert.True(slot.TryGetProperty("endUtc", out var endUtc));
        Assert.True(slot.TryGetProperty("resourceIds", out var resourceIds));

        Assert.Equal(JsonValueKind.Array, resourceIds.ValueKind);
        Assert.True(startUtc.GetString()?.EndsWith("Z") ?? false);
        Assert.True(endUtc.GetString()?.EndsWith("Z") ?? false);
    }

    [Fact]
    public async Task Availability_Returns_Split_Slots_For_MultiDay_Window()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-10&toDate=2025-03-12&resourceIds=7,1");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var slots = doc.RootElement;

        var expected = new[]
        {
            ("2025-03-10T14:00:00Z", "2025-03-10T15:00:00Z"),
            ("2025-03-10T16:00:00Z", "2025-03-10T18:00:00Z"),
            ("2025-03-12T14:00:00Z", "2025-03-12T14:30:00Z"),
            ("2025-03-12T15:00:00Z", "2025-03-12T18:00:00Z")
        };

        Assert.Equal(expected.Length, slots.GetArrayLength());
        for (var i = 0; i < expected.Length; i++)
        {
            var slot = slots[i];
            Assert.Equal(expected[i].Item1, slot.GetProperty("startUtc").GetString());
            Assert.Equal(expected[i].Item2, slot.GetProperty("endUtc").GetString());
        }
    }

    [Fact]
    public async Task Capacity_Allows_Slot_When_Occupancy_Is_Below_Capacity()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2025-03-10&toDate=2025-03-10&resourceIds=99");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var slots = doc.RootElement;

        AssertSlots(slots, new[] { "2025-03-10T14:00:00Z-2025-03-10T18:00:00Z" });
    }

    [Fact]
    public async Task Explain_False_Returns_Slot_Array()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=5,4&explain=false");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task Explain_True_Returns_Explanations_For_Empty_Result()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-10&toDate=2026-03-10&resourceIds=5,4&explain=true");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);

        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("slots", out var slots));
        Assert.True(doc.RootElement.TryGetProperty("explanations", out var explanations));
        Assert.Equal(0, slots.GetArrayLength());
        Assert.True(explanations.GetArrayLength() >= 1);
        Assert.Equal("NoPositiveRule", explanations[0].GetProperty("reason").GetString());
    }

    [Theory]
    [MemberData(nameof(Availability2026Ranges))]
    public async Task Availability_2026_Returns_Expected_Slots(string query, string[] expectedRanges)
    {
        var response = await _client.GetAsync(query);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var slots = doc.RootElement;

        AssertSlots(slots, expectedRanges);
    }

    [Theory]
    [InlineData("/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=5",
        "2026-03-09T09:00:00Z-2026-03-09T10:00:00Z",
        "2026-03-09T11:00:00Z-2026-03-09T13:00:00Z")]
    [InlineData("/api/availability/slots?fromDate=2026-03-12&toDate=2026-03-12&resourceIds=4",
        "2026-03-12T09:00:00Z-2026-03-12T10:30:00Z",
        "2026-03-12T11:00:00Z-2026-03-12T13:00:00Z")]
    public async Task Availability_2026_SingleResource_Splits_Busy(string query, params string[] expectedRanges)
    {
        var response = await _client.GetAsync(query);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var slots = doc.RootElement;

        AssertSlots(slots, expectedRanges);
    }

    [Fact]
    public async Task Availability_ResourceId_Order_Does_Not_Change_Output()
    {
        var first = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-19&resourceIds=5,4");
        var second = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-19&resourceIds=4,5");

        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();

        using var firstStream = await first.Content.ReadAsStreamAsync();
        using var secondStream = await second.Content.ReadAsStreamAsync();
        var firstDoc = await JsonDocument.ParseAsync(firstStream);
        var secondDoc = await JsonDocument.ParseAsync(secondStream);

        var firstRanges = GetRanges(firstDoc.RootElement);
        var secondRanges = GetRanges(secondDoc.RootElement);

        Assert.Equal(firstRanges, secondRanges);
    }

    [Fact]
    public async Task OrGroups_Valid_Request_Returns_Slots()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=5&orGroups=4");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.NotEqual(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task OrGroups_Invalid_Format_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=5&orGroups=4,a");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OrGroups_Too_Many_Groups_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=5&orGroups=1|2|3|4|5|6");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OrGroups_Too_Many_Items_In_Group_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=5&orGroups=1,2,3,4,5,6,7,8,9,10,11");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OrGroups_Total_Resources_Over_20_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=1,2,3,4,5,6,7,8,9,10&orGroups=11,12,13|14,15,16|17,18,19|20,21,22");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public static IEnumerable<object[]> Availability2026Ranges()
    {
        yield return new object[]
        {
            "/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-19&resourceIds=5,4",
            new[]
            {
                "2026-03-09T09:00:00Z-2026-03-09T10:00:00Z",
                "2026-03-09T11:00:00Z-2026-03-09T13:00:00Z",
                "2026-03-12T09:00:00Z-2026-03-12T10:30:00Z",
                "2026-03-12T11:00:00Z-2026-03-12T13:00:00Z",
                "2026-03-16T09:00:00Z-2026-03-16T13:00:00Z",
                "2026-03-19T09:00:00Z-2026-03-19T13:00:00Z"
            }
        };

        yield return new object[]
        {
            "/api/availability/slots?fromDate=2026-03-10&toDate=2026-03-10&resourceIds=5,4",
            Array.Empty<string>()
        };

        yield return new object[]
        {
            "/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=5,4",
            new[]
            {
                "2026-03-09T09:00:00Z-2026-03-09T10:00:00Z",
                "2026-03-09T11:00:00Z-2026-03-09T13:00:00Z"
            }
        };
    }

    private static void AssertSlots(JsonElement slots, IReadOnlyList<string> expectedRanges)
    {
        var actual = GetRanges(slots);

        Assert.Equal(expectedRanges.Count, actual.Length);
        for (var i = 0; i < expectedRanges.Count; i++)
        {
            Assert.Equal(expectedRanges[i], actual[i]);
        }
    }

    private static string[] GetRanges(JsonElement slots)
    {
        return slots.EnumerateArray()
            .Select(item => $"{item.GetProperty("startUtc").GetString()}-{item.GetProperty("endUtc").GetString()}")
            .ToArray();
    }
}
