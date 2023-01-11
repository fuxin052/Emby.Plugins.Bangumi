using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.Bangumi.Model;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.Bangumi.Providers;

public class SeasonProvider : IRemoteMetadataProvider<Season, SeasonInfo>, IHasOrder
{
    private readonly BangumiApiClient ApiClient;
    private readonly ILogger Logger;
    private readonly IJsonSerializer JsonSerializer;
    public SeasonProvider( IHttpClient httpClientFactory, ILogger logger, IJsonSerializer jsonSerializer)
    {
        ApiClient = new BangumiApiClient(httpClientFactory, logger);
        Logger = logger;
        JsonSerializer = jsonSerializer;
    }

    public int Order => -5;
    public string Name => Constants.ProviderName;

    public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
    {

        var name = info.Name;
        var seriesName = info.SeriesName;
        var bangumiId = info.SeriesProviderIds.GetValueOrDefault(Constants.ProviderName);
        var solvedName = Anitomy.ExtractAnimeTitle(seriesName);
        Logger.Debug("|季度|查询|Season开始");
        Logger.Debug($"|章节|查询|SeasonInfo:{JsonSerializer.SerializeToString(info)}");

        var result = new MetadataResult<Season> { ResultLanguage = Constants.Language };

        // 如果没有id 返回空
        if (string.IsNullOrEmpty(bangumiId))
            return result;

        Subject m = await ApiClient.GetSubject(bangumiId, cancellationToken);
        if (m == null)
        {
            return result;
        }
        result.HasMetadata = true;
        result.Item = new Season();

        if (m.Rating != null) result.Item.CommunityRating = m.Rating.Score;
        result.Item.ProviderIds.Add(Constants.ProviderName, $"{m.Id}");
        if (m.Rating?.Score != null) result.Item.CommunityRating = m.Rating.Score;
        result.Item.Name = WebUtility.HtmlDecode(m.ShowName);
        result.Item.OriginalTitle = WebUtility.HtmlDecode(m.OriginalName);
        result.Item.Overview = (m.Summary ?? "").Trim();
        result.Item.Tags = m.PopularTags;
        if (DateTime.TryParse(m.AirDate, out var airDate))
            result.Item.PremiereDate = airDate;
        if (m.ProductionYear?.Length == 4)
            result.Item.ProductionYear = int.Parse(m.ProductionYear);
        (await ApiClient.GetSubjectPersonInfos(bangumiId, cancellationToken)).ForEach(result.AddPerson);
        (await ApiClient.GetSubjectCharacters(bangumiId, cancellationToken)).ForEach(result.AddPerson);
        return result;
    }
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo info, CancellationToken token)
    {
        return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
    }
    public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken token)
    {
        Logger.Debug("Bangumi_获取图片从url: {0}", url);
        return ApiClient.GetAsync(url, token);
    }
}