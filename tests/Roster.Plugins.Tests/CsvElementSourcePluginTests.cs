using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roster.Plugins.Abstractions;
using Roster.Plugins.Csv;
using Shouldly;
using Xunit;

namespace Roster.Plugins.Tests;

public class CsvElementSourcePluginTests
{
    private readonly CsvElementSourcePlugin _plugin;

    public CsvElementSourcePluginTests()
    {
        _plugin = new CsvElementSourcePlugin();
    }

    [Fact]
    public void GetConfigSchema_ShouldReturnExpectedFields()
    {
        var schema = _plugin.GetConfigSchema();
        schema.Fields.ShouldNotBeEmpty();
        schema.Fields.ShouldContain(f => f.Key == "CsvContent");
        schema.Fields.ShouldContain(f => f.Key == "ColumnName");
    }

    [Fact]
    public async Task ValidateConfigAsync_WithMissingCsvContent_ShouldFail()
    {
        var config = new PluginConfig(new Dictionary<string, string>
        {
            { "ColumnName", "Name" }
        });

        var result = await _plugin.ValidateConfigAsync(config, CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("CSV Content is required"));
    }

    [Fact]
    public async Task ImportAsync_WithValidSemicolonCsv_ShouldMapCorrectly()
    {
        var csvContent = await File.ReadAllTextAsync("Fixtures/sample.csv");
        
        var config = new PluginConfig(new Dictionary<string, string>
        {
            { "CsvContent", csvContent },
            { "HasHeader", "true" },
            { "ColumnName", "Name" },
            { "ColumnExternalId", "Id" },
            { "ColumnSubtitle", "Subtitle" },
            { "ColumnImageUrl", "ImageUrl" },
            { "ColumnGroup", "Group" }
        });

        var result = await _plugin.ImportAsync(config, CancellationToken.None);

        result.Warnings.ShouldBeEmpty();
        result.Elements.Count.ShouldBe(3);

        var first = result.Elements[0];
        first.ExternalId.ShouldBe("t1");
        first.Name.ShouldBe("Team Alpha");
        first.Subtitle.ShouldBe("New York");
        first.ImageUrl.ShouldBe("http://alpha.jpg");
        first.Group.ShouldBe("Group A");
        
        var third = result.Elements[2];
        third.ExternalId.ShouldBe("t3");
        third.Name.ShouldBe("Team Gamma");
        third.Subtitle.ShouldBeNullOrEmpty();
        third.ImageUrl.ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task ImportAsync_WithMissingNameColumn_ShouldProduceWarningsAndSkip()
    {
        var csvContent = "Id;Subtitle\n1;Sub1\n2;Sub2";
        
        var config = new PluginConfig(new Dictionary<string, string>
        {
            { "CsvContent", csvContent },
            { "HasHeader", "true" },
            { "ColumnName", "Name" } // This column does not exist in the CSV
        });

        var result = await _plugin.ImportAsync(config, CancellationToken.None);

        result.Elements.ShouldBeEmpty();
        result.Warnings.ShouldContain(w => w.Contains("Could not resolve Name column"));
    }
}
