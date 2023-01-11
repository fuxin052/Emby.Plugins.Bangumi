using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugins.Bangumi.Extension;

public class ExternalEpisodeId : IExternalId
{
    public bool Supports(IHasProviderIds item)
    {
        return item is Episode;
    }

    public string Name => Constants.ProviderName;

    public string Key => Constants.PluginName;


    public string UrlFormatString => "https://bgm.tv/ep/{0}";
}