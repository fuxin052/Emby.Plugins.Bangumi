using System.Collections.Generic;
using System.Linq;
using AnitomySharp1 = Emby.Plugins.Bangumi.Modules.AnitomySharp;
using Element2 = Emby.Plugins.Bangumi.Modules.AnitomySharp.Element;

namespace Emby.Plugins.Bangumi;

public class Anitomy
{
    public static List<Element2> ElementsOutput(string path)
    {
        return new List<Element2>(AnitomySharp1.AnitomySharp.Parse(path));
    }

    public static string? ExtractAnimeTitle(string path)
    {
        var elements = AnitomySharp1.AnitomySharp.Parse(path);
        return elements.FirstOrDefault(p => p.Category == Element2.ElementCategory.ElementAnimeTitle)?.Value;
    }

    public static string? ExtractEpisodeTitle(string path)
    {
        var elements = AnitomySharp1.AnitomySharp.Parse(path);
        return elements.FirstOrDefault(p => p.Category == Element2.ElementCategory.ElementEpisodeTitle)?.Value;
    }

    public static string? ExtractEpisodeNumber(string path)
    {
        var elements = AnitomySharp1.AnitomySharp.Parse(path);
        return elements.FirstOrDefault(p => p.Category == Element2.ElementCategory.ElementEpisodeNumber)?.Value;
    }

    public static string? ExtractSeasonNumber(string path)
    {
        var elements = AnitomySharp1.AnitomySharp.Parse(path);
        return elements.FirstOrDefault(p => p.Category == Element2.ElementCategory.ElementAnimeSeason)?.Value;
    }
}