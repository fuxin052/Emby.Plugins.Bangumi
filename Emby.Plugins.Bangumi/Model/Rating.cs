using System.Collections.Generic;

namespace Emby.Plugins.Bangumi.Model;

public class Rating
{
    public int Rank { get; set; }

    public int Total { get; set; }

    public Dictionary<string, int> Count { get; set; } = new();

    public float Score { get; set; }
}