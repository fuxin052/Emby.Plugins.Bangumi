using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using System.Linq;

namespace Emby.Plugins.Bangumi.Providers;

public class ImageProvider: IRemoteImageProvider, IHasOrder
{
    private readonly BangumiApiClient ApiClient;
    private readonly ILogger Logger;

    public ImageProvider(IHttpClient httpClientFactory,
        ILogger logger)
    {
        Logger = logger;
        ApiClient = new BangumiApiClient(httpClientFactory, logger);
    }

    public int Order => 1;
    public string Name => Constants.ProviderName;


    // 根据id获取图片
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bangumiId = item.GetProviderId(Name); // 获取bangumi_id
        var fileName = item.FileNameWithoutExtension; // 获取文件名
        var title = item.Name; // 获取已存在的标题

        Logger.Debug("|图片|查询|查询图片url");
        Logger.Debug("|图片|查询|bangumiId= {0}", bangumiId);
        Logger.Debug("|图片|查询|fileName= {0}", fileName);
        Logger.Debug("|图片|查询|title= {0}", title);

        if (string.IsNullOrEmpty(bangumiId)){
            return Enumerable.Empty<RemoteImageInfo>();
        }

        var m = await ApiClient.GetSubject(bangumiId, cancellationToken);
        Logger.Debug("|图片|查询|返回结果m.Id:{0}", m.Id);
        Logger.Debug("|图片|查询|返回结果m.ShowName:{0}", m.ShowName);
        Logger.Debug("|图片|查询|返回结果m.ChineseName:{0}", m.ChineseName);
        Logger.Debug("|图片|查询|返回结果m.OriginalName:{0}", m.OriginalName);
        if (!string.IsNullOrWhiteSpace(m.DefaultImage)){
            Logger.Info("|图片|查询|查询到图片地址为:{0}", m.DefaultImage);
            return new[]
            {
                new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = m.DefaultImage
                }
            };
        }

        Logger.Warn("|图片|查询|没有查询到图片: {0}", bangumiId);
        return Enumerable.Empty<RemoteImageInfo>();

    }

    public bool Supports(BaseItem item)
    {
        return item is Movie || item is Season || item is Series;
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType>
        {
            ImageType.Primary,
            ImageType.Thumb,
            ImageType.Backdrop
        };
    }

    public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        Logger.Info("下载封面图: {0}", url);

        return ApiClient.GetAsync(url, cancellationToken);
    }

}