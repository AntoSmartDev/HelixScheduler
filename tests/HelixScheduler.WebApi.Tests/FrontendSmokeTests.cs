using System.IO;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class FrontendSmokeTests
{
    [Fact]
    public void Explorer_Page_Contains_Ancestor_And_Slot_Controls()
    {
        var html = File.ReadAllText(Path.Combine(GetRepoRoot(), "samples", "HelixScheduler.DemoWeb", "wwwroot", "index.html"));

        Assert.Contains("slotDurationMinutes", html);
        Assert.Contains("includeRemainderSlot", html);
        Assert.Contains("ancestorFilterType", html);
        Assert.Contains("ancestorFilterDefinition", html);
        Assert.Contains("ancestorFilterProperty", html);
        Assert.Contains("ancestorFilterMatchMode", html);
        Assert.Contains("ancestorFilterScope", html);
        Assert.Contains("ancestorFilterMatchAll", html);
        Assert.Contains("addAncestorFilter", html);
        Assert.Contains("clearAncestorFilters", html);
        Assert.Contains("payloadPreview", html);
        Assert.Contains("copyPayload", html);
    }

    [Fact]
    public void Search_Page_Contains_Ancestor_And_Slot_Controls()
    {
        var html = File.ReadAllText(Path.Combine(GetRepoRoot(), "samples", "HelixScheduler.DemoWeb", "wwwroot", "search.html"));

        Assert.Contains("slotDurationMinutes", html);
        Assert.Contains("includeRemainderSlot", html);
        Assert.Contains("ancestorFilterType", html);
        Assert.Contains("ancestorFilterDefinition", html);
        Assert.Contains("ancestorFilterProperty", html);
        Assert.Contains("ancestorFilterMatchMode", html);
        Assert.Contains("ancestorFilterScope", html);
        Assert.Contains("ancestorFilterMatchAll", html);
        Assert.Contains("addAncestorFilter", html);
        Assert.Contains("clearAncestorFilters", html);
        Assert.Contains("payloadPreview", html);
        Assert.Contains("copyPayload", html);
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HelixScheduler.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new DirectoryNotFoundException("Repo root not found.");
        }

        return dir.FullName;
    }
}
