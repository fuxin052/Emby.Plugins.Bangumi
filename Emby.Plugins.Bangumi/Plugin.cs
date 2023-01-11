using System;
using System.Collections.Generic;
using System.IO;
using Emby.Plugins.Bangumi.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Common.Configuration;

namespace Emby.Plugins.Bangumi
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => Constants.PluginName;
        public override Guid Id => Guid.Parse(Constants.PluginGuid);
        public override string Description => "Improved Metadata Provider, specifically designed for Bangumi.";

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            var type = GetType();
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "Plugin.Bangumi.Configuration",
                    EmbeddedResourcePath = (type.Namespace ?? "") + ".Configuration.configPage.html",
                    EnableInMainMenu = true,
                    MenuSection = "server",
                    MenuIcon = "theaters",
                    DisplayName = "Bangumi 设置",
                }
            };
        }
        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream($"{type.Namespace}.thumb.png");
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

    }
}
