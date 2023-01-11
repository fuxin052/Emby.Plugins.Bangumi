using MediaBrowser.Model.Plugins;

namespace Emby.Plugins.Bangumi.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool TranslationPreference { get; set; }
        public string bangumiToken { get; set; }
        public PluginConfiguration()
        {
            TranslationPreference = true;
            bangumiToken = "";
        }
    }
}