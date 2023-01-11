using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Logging;
using Emby.Plugins.Bangumi.Model;
using System;
using System.Net;

namespace Emby.Plugins.Bangumi.Providers;

public class SeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
{
    private readonly BangumiApiClient ApiClient;

    private readonly ILogger Logger;


    public SeriesProvider(IHttpClient httpClientFactory, ILogger logger)
    {
        ApiClient = new BangumiApiClient(httpClientFactory, logger);
        Logger = logger;
    }

    public int Order => 1;

    public string Name => Constants.ProviderName;

    // 刷新
    public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
    {
        var name = info.Name;
        var bangumiId = info.GetProviderId(Constants.ProviderName);
        var solvedName = Anitomy.ExtractAnimeTitle(name);
        Logger.Debug("|节目|查询|查询Series开始");
        Logger.Debug("|节目|查询|info.Name=" + name);
        Logger.Debug("|节目|查询|solvedName=" + solvedName ?? "空");
        Logger.Debug("|节目|查询|bangumiId=" + bangumiId);

        var result = new MetadataResult<Series> { ResultLanguage = Constants.Language };
        // 通过关键字来搜索, 查找条目的bangumiid
        if (string.IsNullOrEmpty(bangumiId))
        {
            Logger.Debug("|节目|查询|通过关键字\"{0}\" 搜索, 查找条目的bangumiid", solvedName);
            var searchResult = await ApiClient.SearchSubject(solvedName, cancellationToken);
            if (searchResult.Count > 0)
            {
                bangumiId = $"{searchResult[0].Id}";
            }
        }

        // 如果没有id 返回空
        if (string.IsNullOrEmpty(bangumiId))
            return result;

        Subject m = await ApiClient.GetSubject(bangumiId, cancellationToken);
        if (m == null)
        {
            return result;
        }
        result.HasMetadata = true;
        result.Item = new Series();
        if (m.Rating != null) result.Item.CommunityRating = m.Rating.Score;
        result.Item.ProviderIds.Add(Constants.ProviderName, bangumiId);
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
    // 搜索
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var results = new List<RemoteSearchResult>();
        Logger.Debug("|节目|搜索|搜索Series开始");
        var bangumiId = searchInfo.GetProviderId(Constants.ProviderName);
        var name = searchInfo.Name;
        var Year = searchInfo.Year;
        Logger.Debug("|节目|搜索|搜索开始, bangumiId为" + bangumiId);
        Logger.Debug("|节目|搜索|搜索开始, 关键字为" + name);
        Logger.Debug("|节目|搜索|搜索开始, searchInfo.Year={0}", Year);

        if (!string.IsNullOrEmpty(bangumiId))
        {

            Logger.Debug("|节目|搜索|通过bangumiId搜索, bangumiId=" + bangumiId);
            var subject = await ApiClient.GetSubject(bangumiId, token);
            if (subject == null)
                return results;
            var result = new RemoteSearchResult
            {
                Name = subject.ShowName,
                SearchProviderName = subject.OriginalName,
                ImageUrl = subject.DefaultImage,
                Overview = subject.Summary
            };
            if (DateTime.TryParse(subject.AirDate, out var airDate))
                result.PremiereDate = airDate;
            if (subject.ProductionYear?.Length == 4)
                result.ProductionYear = int.Parse(subject.ProductionYear);
            result.SetProviderId(Constants.ProviderName, bangumiId);
            results.Add(result);
        }
        else if (!string.IsNullOrEmpty(name))
        {
            Logger.Debug("|节目|搜索|通过关键字搜索, name=" + name);
            var series = await ApiClient.SearchSubject(name, token);
            Logger.Debug("|节目|搜索|返回结果长度为{0}", series.Count);
            foreach (var item in series)
            {
                var itemId = $"{item.Id}";
                Logger.Debug("|节目|搜索|返回结果item.Id={0}", itemId);
                Logger.Debug("|节目|搜索|返回结果item.OriginalName={0}", item.OriginalName);
                Logger.Debug("|节目|搜索|返回结果item.ChineseName={0}", item.ChineseName);
                var result = new RemoteSearchResult
                {
                    Name = item.ShowName,
                    SearchProviderName = item.OriginalName,
                    ImageUrl = item.DefaultImage,
                    Overview = item.Summary
                };
                Logger.Debug("|节目|搜索|返回结果item.AirDate={0}", item.AirDate);
                if (DateTime.TryParse(item.AirDate, out var airDate))
                {
                    Logger.Debug("|节目|搜索|返回结果airDate={0}", item.AirDate);
                    result.PremiereDate = airDate;
                }
                Logger.Info("|节目|搜索|item.ProductionYear={0}", item.ProductionYear);
                if (item.ProductionYear?.Length == 4)
                    result.ProductionYear = int.Parse(item.ProductionYear);
                if (result.ProductionYear != null && Year != null)
                {
                    Logger.Debug("|节目|搜索|返回结果item.ProductionYear={0}", item.ProductionYear);
                    Logger.Debug("|节目|搜索|返回结果searchInfo.Year={0}", Year);
                    if (result.ProductionYear != Year)
                    {
                        continue;
                    }
                }
                result.SetProviderId(Constants.ProviderName, itemId);
                results.Add(result);
            }
        }
        Logger.Debug("======搜索Series结束");
        return results;
    }

    public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken token)
    {
        Logger.Debug("Bangumi_获取图片从url: {0}", url);
        return ApiClient.GetAsync(url, token);
    }
}