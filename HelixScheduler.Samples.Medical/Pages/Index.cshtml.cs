using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelixScheduler.Samples.Medical.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IHttpClientFactory clientFactory, ILogger<IndexModel> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [BindProperty]
    public DateOnly FromDate { get; set; }

    [BindProperty]
    public DateOnly ToDate { get; set; }

    [BindProperty]
    public List<ResourceGroupModel> ResourceGroups { get; set; } = new();

    [BindProperty]
    public bool UseDemo { get; set; }

    public IReadOnlyList<SlotView> Slots { get; private set; } = Array.Empty<SlotView>();

    public string? ApiRequestUrl { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? InfoMessage { get; private set; }

    public async Task OnGetAsync()
    {
        FromDate = new DateOnly(2026, 3, 9);
        ToDate = new DateOnly(2026, 3, 12);
        var resources = await LoadResourcesAsync();
        if (resources == null)
        {
            return;
        }

        ResourceGroups = BuildGroupsFromResources(resources, postedGroups: null, preferDemo: true);
        SetApiRequestUrl(FromDate, ToDate, GetSelectedResourceIds(ResourceGroups));
    }

    public async Task OnPostAsync()
    {
        var resources = await LoadResourcesAsync();
        if (resources == null)
        {
            return;
        }

        if (UseDemo)
        {
            FromDate = new DateOnly(2026, 3, 9);
            ToDate = new DateOnly(2026, 3, 12);
            ResourceGroups = BuildGroupsFromResources(resources, postedGroups: null, preferDemo: true);
        }
        else
        {
            ResourceGroups = BuildGroupsFromResources(resources, ResourceGroups, preferDemo: false);
        }

        if (FromDate == default || ToDate == default)
        {
            ErrorMessage = "From/To dates are required.";
            return;
        }

        if (FromDate > ToDate)
        {
            ErrorMessage = "From date must be before or equal to To date.";
            return;
        }

        var inclusiveDays = (ToDate.DayNumber - FromDate.DayNumber) + 1;
        if (inclusiveDays > 31)
        {
            ErrorMessage = "Date range must be 31 days or less.";
            return;
        }

        var selectedResourceIds = GetSelectedResourceIds(ResourceGroups);
        if (selectedResourceIds.Count == 0)
        {
            ErrorMessage = "Select at least one resource filter and resource.";
            return;
        }

        var from = FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var resourceIdsCsv = string.Join(",", selectedResourceIds);
        var client = _clientFactory.CreateClient("AvailabilityApi");
        var requestUri = $"/api/availability/slots?fromDate={from}&toDate={to}&resourceIds={resourceIdsCsv}";
        ApiRequestUrl = client.BaseAddress == null ? requestUri : new Uri(client.BaseAddress, requestUri).ToString();

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(requestUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Availability API call failed.");
            ErrorMessage = "Availability API is not reachable.";
            return;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            ErrorMessage = await response.Content.ReadAsStringAsync();
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = $"Request failed ({(int)response.StatusCode}).";
            return;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var slots = await JsonSerializer.DeserializeAsync<List<SlotDto>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (slots == null || slots.Count == 0)
        {
            InfoMessage = "Nessuna disponibilita.";
            Slots = Array.Empty<SlotView>();
            return;
        }

        Slots = slots
            .OrderBy(slot => slot.StartUtc)
            .Select(slot => new SlotView(slot.StartUtc, slot.EndUtc))
            .ToList();
    }

    private void SetApiRequestUrl(DateOnly from, DateOnly to, IReadOnlyList<int> resourceIds)
    {
        var fromValue = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toValue = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var resources = string.Join(",", resourceIds);
        var requestUri = $"/api/availability/slots?fromDate={fromValue}&toDate={toValue}&resourceIds={resources}";
        var client = _clientFactory.CreateClient("AvailabilityApi");
        ApiRequestUrl = client.BaseAddress == null ? requestUri : new Uri(client.BaseAddress, requestUri).ToString();
    }

    public sealed record ResourceOption(int Id, string Label);

    public sealed class ResourceGroupModel
    {
        public ResourceGroupModel()
        {
            Name = string.Empty;
            Options = Array.Empty<ResourceOption>();
            SelectedIds = new List<int>();
        }

        public ResourceGroupModel(string name, bool isActive, IReadOnlyList<ResourceOption> options, List<int> selectedIds)
        {
            Name = name;
            IsActive = isActive;
            Options = options;
            SelectedIds = selectedIds;
        }

        public string Name { get; set; }
        public bool IsActive { get; set; }
        public IReadOnlyList<ResourceOption> Options { get; set; }
        public List<int> SelectedIds { get; set; }
    }

    public sealed record SlotView(DateTime StartUtc, DateTime EndUtc)
    {
        public string RangeLabel => $"{StartUtc:HH:mm}-{EndUtc:HH:mm} UTC";
    }

    public sealed class SlotDto
    {
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public int[] ResourceIds { get; set; } = Array.Empty<int>();
    }

    private sealed class ResourceInfo
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSchedulable { get; set; }
    }

    private async Task<List<ResourceInfo>?> LoadResourcesAsync()
    {
        var client = _clientFactory.CreateClient("AvailabilityApi");
        var requestUri = "/api/resources?onlySchedulable=true";
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(requestUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resource API call failed.");
            ErrorMessage = "Resource API is not reachable.";
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = $"Resource request failed ({(int)response.StatusCode}).";
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var resources = await JsonSerializer.DeserializeAsync<List<ResourceInfo>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (resources == null || resources.Count == 0)
        {
            ErrorMessage = "No schedulable resources returned by API.";
            return null;
        }

        return resources;
    }

    private static List<ResourceGroupModel> BuildGroupsFromResources(
        IReadOnlyList<ResourceInfo> resources,
        IReadOnlyList<ResourceGroupModel>? postedGroups,
        bool preferDemo)
    {
        var grouped = resources
            .GroupBy(resource => GetGroupName(resource))
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                group.Key,
                Options = group
                    .OrderBy(resource => resource.Name)
                    .Select(resource => new ResourceOption(resource.Id, resource.Name))
                    .ToList()
            })
            .ToList();

        var postedMap = new Dictionary<string, ResourceGroupModel>(StringComparer.OrdinalIgnoreCase);
        if (postedGroups != null)
        {
            foreach (var posted in postedGroups)
            {
                postedMap[posted.Name] = posted;
            }
        }

        var result = new List<ResourceGroupModel>();
        foreach (var group in grouped)
        {
            postedMap.TryGetValue(group.Key, out var posted);
            var selectedIds = posted?.SelectedIds ?? new List<int>();
            var allowedIds = group.Options.Select(option => option.Id).ToHashSet();
            selectedIds = selectedIds.Where(id => allowedIds.Contains(id)).ToList();

            if (preferDemo)
            {
                selectedIds = SelectDemoIds(group.Key, group.Options);
            }
            else if (selectedIds.Count == 0)
            {
                selectedIds = new List<int> { group.Options[0].Id };
            }

            result.Add(new ResourceGroupModel(
                group.Key,
                posted?.IsActive ?? true,
                group.Options,
                selectedIds));
        }

        return result;
    }

    private static List<int> SelectDemoIds(string groupName, IReadOnlyList<ResourceOption> options)
    {
        var demoName = groupName.Equals("Doctor", StringComparison.OrdinalIgnoreCase)
            ? "Doctor 8"
            : groupName.Equals("Room", StringComparison.OrdinalIgnoreCase)
                ? "Room 2"
                : options[0].Label;

        var selected = options.FirstOrDefault(option => string.Equals(option.Label, demoName, StringComparison.OrdinalIgnoreCase));
        return selected == null ? new List<int> { options[0].Id } : new List<int> { selected.Id };
    }

    private static string GetGroupName(ResourceInfo resource)
    {
        if (!string.IsNullOrWhiteSpace(resource.Code))
        {
            if (resource.Code.StartsWith("DOC", StringComparison.OrdinalIgnoreCase))
            {
                return "Doctor";
            }

            if (resource.Code.StartsWith("ROOM", StringComparison.OrdinalIgnoreCase))
            {
                return "Room";
            }
        }

        if (resource.Name.StartsWith("Doctor", StringComparison.OrdinalIgnoreCase))
        {
            return "Doctor";
        }

        if (resource.Name.StartsWith("Room", StringComparison.OrdinalIgnoreCase))
        {
            return "Room";
        }

        return "Other";
    }

    private static List<int> GetSelectedResourceIds(IEnumerable<ResourceGroupModel> groups)
    {
        return groups
            .Where(group => group.IsActive)
            .SelectMany(group => group.SelectedIds)
            .Distinct()
            .ToList();
    }
}
