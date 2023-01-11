using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.Bangumi.Providers;

public class EpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>, IHasOrder
{
    private readonly BangumiApiClient ApiClient;
    private readonly ILogger Logger;
    private readonly IJsonSerializer JsonSerializer;

    public EpisodeProvider(IHttpClient httpClientFactory, ILogger logger, IJsonSerializer jsonSerializer)
    {
        ApiClient = new BangumiApiClient(httpClientFactory, logger);
        Logger = logger;
        JsonSerializer = jsonSerializer;
    }

    public int Order => -5;
    public string Name => Constants.ProviderName;

    public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Logger.Debug("|章节|查询|Episode-GetMetadata");
        Logger.Debug($"|章节|查询|EpisodeInfo:{JsonSerializer.SerializeToString(info)}");
        var result = new MetadataResult<Episode> { ResultLanguage = Constants.Language };
        Logger.Debug("|章节|查询|Episode-GetMetadata=============");

        var episodeId = info.GetProviderId(Constants.ProviderName);
        if (episodeId == "0"){
            episodeId = "";
        }
        var IndexNumber = info.IndexNumber;

        Model.Episode epData;

        if(string.IsNullOrEmpty(episodeId)){
            if (info.ParentIndexNumber == 0) {
                return result;
            }
            if (IndexNumber.HasValue){
                var seriesId = info.SeriesProviderIds.GetValueOrDefault(Constants.ProviderName);
                if (string.IsNullOrEmpty(seriesId)){
                    return result;
                }
                epData = await ApiClient.GetEpisodeFromList(seriesId, IndexNumber.Value, token);
            }else{
                return result;
            }
        }else{
            epData = await ApiClient.GetEpisode(episodeId, token);
        }

        if (epData == null || epData.Id == 0){
            Logger.Info("|章节|查询|没有找到章节信息");
            return result;
        }

        Logger.Info("|章节|查询|result={0}_{1}", epData.Id, epData.OriginalName);

        result.Item = new Episode();
        result.HasMetadata = true;

        result.Item.SetProviderId(Constants.ProviderName, $"{epData.Id}");
        if (DateTime.TryParse(epData.AirDate, out var airDate))
            result.Item.PremiereDate = airDate;
        if (epData.AirDate.Length == 4)
            result.Item.ProductionYear = int.Parse(epData.AirDate);
        result.Item.Name = WebUtility.HtmlDecode(epData.ShowName);
        result.Item.OriginalTitle = WebUtility.HtmlDecode(epData.OriginalName);
        result.Item.Overview = (epData.Description ?? "").Trim();
        return result;
    }

    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken token)
    {
        return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
    }
    public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken token)
    {
        Logger.Info("Bangumi_获取图片从url: {0}", url);
        return ApiClient.GetAsync(url, token);
    }
}