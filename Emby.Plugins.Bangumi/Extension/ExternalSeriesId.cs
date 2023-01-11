using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugins.Bangumi{
    public class ExternalSeriesId : IExternalId
    {
        public bool Supports(IHasProviderIds item)
        {
            return item is Series || item is Season || item is Movie;
        }

        public string Name => Constants.ProviderName;

        public string Key => Constants.PluginName;

        public string UrlFormatString => "https://bgm.tv/subject/{0}";
    }
}
