using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugins.Bangumi
{

    public class ExternalPersonId : IExternalId
    {
        public bool Supports(IHasProviderIds item)
        {
            return item is Person;
        }

        public string Name => Constants.ProviderName;

        public string Key => Constants.PluginName;


        public string UrlFormatString => "https://bgm.tv/person/{0}";
    }
}