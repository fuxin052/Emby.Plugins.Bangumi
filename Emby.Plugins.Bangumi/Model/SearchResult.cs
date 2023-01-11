using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Emby.Plugins.Bangumi.Model;

internal class SearchResult<T>
{
    [JsonPropertyName("results")]
    public int ResultCount { get; set; }

    public List<T> List { get; set; } = new();
}