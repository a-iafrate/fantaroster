using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Roster.DomainPacks;

public interface IDomainPackLoader
{
    IReadOnlyList<DomainPack> GetAllPacks();
    DomainPack? GetPack(string id);
}

public sealed class DomainPackLoader : IDomainPackLoader
{
    private readonly Dictionary<string, DomainPack> _packs = new(StringComparer.OrdinalIgnoreCase);

    public DomainPackLoader()
    {
        var assembly = typeof(DomainPackLoader).Assembly;
        var resourceNames = assembly.GetManifestResourceNames().Where(n => n.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                var pack = JsonSerializer.Deserialize<DomainPack>(stream, options);
                if (pack != null)
                {
                    _packs[pack.Id] = pack;
                }
            }
        }
    }

    public IReadOnlyList<DomainPack> GetAllPacks() => _packs.Values.ToList();

    public DomainPack? GetPack(string id) => _packs.TryGetValue(id, out var pack) ? pack : null;
}
