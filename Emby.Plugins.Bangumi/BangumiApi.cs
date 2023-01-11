using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using Emby.Plugins.Bangumi.Model;
using System.IO;
using EmbyPersonType = MediaBrowser.Model.Entities.PersonType;
using MediaBrowser.Model.Entities;
using System.Linq;

namespace Emby.Plugins.Bangumi
{
    public class BangumiApiClient
    {

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public string bangumiToken {
            get {
                var config = Plugin.Instance.Configuration;
                var t = config.bangumiToken;
                if (string.IsNullOrEmpty(t)) {
                    return null;
                }
                return t;
            }
        }

        public BangumiApiClient(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Subject> GetSubject(string bangumiId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bangumiId))
            {
                return new Subject();
            }

            try
            {
                var contentStream = await GetStream(GetSubjectUrl(bangumiId), cancellationToken);
                var ret = JsonSerializer.Deserialize<Subject>(contentStream, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                if (ret == null) return new Subject();
                return ret;
            }
            catch (Exception e)
            {
                _logger.Warn(e.ToString());
                return new Subject();
            }
        }

        public async Task<Episode> GetEpisode(string bangumiId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bangumiId))
            {
                return new Episode();
            }

            try
            {
                var contentStream = await GetStream(GetEpisodeUrl(bangumiId), cancellationToken);
                var ret = JsonSerializer.Deserialize<Episode>(contentStream, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                if (ret == null) return new Episode();
                return ret;
            }
            catch (Exception e)
            {
                _logger.Warn(e.ToString());
                return new Episode();
            }
        }
        public async Task<Episode> GetEpisodeFromList(string bangumiId, int indexNumber , CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bangumiId))
            {
                return new Episode();
            }

            try
            {
                var contentStream = await GetStream(GetEpisodesUrl(bangumiId, indexNumber), cancellationToken);
                var ret = JsonSerializer.Deserialize<DataList<Episode>>(contentStream, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                if (ret == null) return new Episode();
                if (ret.Data.Count > 0){
                    Episode a1 = null;
                    foreach (var item in ret.Data){
                        if (item.Index == indexNumber && item.Type == EpisodeType.Normal)
                        {
                            return item;
                        }
                        if (item.Order == indexNumber && item.Type == EpisodeType.Special) {
                            a1 = item;
                        }
                    }
                    if (a1 != null) {
                        return a1;
                    }
                }
                return new Episode();
            }
            catch (Exception e)
            {
                _logger.Warn(e.ToString());
                return new Episode();
            }
        }




        public async Task<List<Subject>> SearchSubject(string keyword, CancellationToken cancellationToken)
        {
            var contentStream = await GetStream(GetSearchSubjectUrl(keyword), cancellationToken);
            var json = JsonSerializer.Deserialize<SearchResult<Subject>>(contentStream, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var ss = JsonSerializer.Serialize(json);
            _logger.Info("返回json为", ss);
            _logger.Info("返回结果长度ResultCount={0}", json == null ? "" : json.ResultCount);
            var list = json?.List ?? new List<Subject>();
            return Subject.SortBySimilarity(list, keyword);
        }




        public async Task<List<PersonInfo>> GetSubjectPersonInfos(string bangumiid, CancellationToken cancellationToken)
        {
            try
            {
                var result = new List<PersonInfo>();
                var contentStream = await GetStream(GetSubjectPersonsUrl(bangumiid), cancellationToken);
                var persons = JsonSerializer.Deserialize<List<RelatedPerson>>(contentStream, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                persons?.ForEach(person =>
                {
                    var item = new PersonInfo
                    {
                        Name = person.Name,
                        ImageUrl = person.DefaultImage,
                        Type = person.Relation switch
                        {
                            "导演" => EmbyPersonType.Director,
                            "制片人" => EmbyPersonType.Producer,
                            "系列构成" => EmbyPersonType.Composer,
                            "脚本" => EmbyPersonType.Writer,
                            _ => EmbyPersonType.Lyricist
                        },
                        ProviderIds = new ProviderIdDictionary(
                            new Dictionary<string, string> {
                            { Constants.ProviderName, $"{person.Id}" }
                            }
                        )
                    };
                    if (EmbyPersonType.Lyricist != item.Type)
                        result.Add(item);
                });
                return result;
            }
            catch (Exception)
            {
                _logger.Info("分析人员错误-GetSubjectPersonInfos");
                return new List<PersonInfo>();
            }
        }
        public async Task<List<PersonInfo>> GetSubjectCharacters(string bangumiid, CancellationToken cancellationToken)
        {
            try
            {
                var result = new List<PersonInfo>();

                var contentStream = await GetStream(GetSubjectCharactersUrl(bangumiid), cancellationToken);
                var characters = JsonSerializer.Deserialize<List<RelatedCharacter>>(contentStream, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                characters?.ForEach(character =>
                {
                    if (character.Actors == null)
                        return;
                    result.AddRange(character.Actors.Select(actor => new PersonInfo
                    {
                        Name = actor.Name,
                        Role = character.Name,
                        ImageUrl = actor.DefaultImage,
                        Type = EmbyPersonType.Actor,
                        ProviderIds = new ProviderIdDictionary(new Dictionary<string, string> { { Constants.ProviderName, $"{actor.Id}" } })
                    }));
                });
                return result;
            }
            catch (Exception)
            {
                _logger.Info("分析人员错误-GetSubjectCharacters");
                return new List<PersonInfo>();
            }
        }

        // 发起请求 获取流
        public async Task<HttpResponseInfo> GetAsync(string url, CancellationToken cancellationToken)
        {
            var options = new MediaBrowser.Common.Net.HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                EnableDefaultUserAgent = true,
                TimeoutMs = 60000
            };
            if(!string.IsNullOrWhiteSpace(bangumiToken)){
                options.RequestHeaders.Add("authorization", $"Bearer {bangumiToken.Trim()}");
            }
            options.RequestHeaders.Add("User-Agent", "Jellyfin.Plugin.Bangumi");

            HttpResponseInfo  response = await _httpClient.GetResponse(options).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException($"Bad Status Code: {response.StatusCode}");
            return response;
        }

        // 发起请求
        private async Task<Stream> GetStream(string url, CancellationToken cancellationToken)
        {
            var response = await GetAsync(url, cancellationToken);
            return response.Content;
        }
        // subjects类型
        private static string GetSubjectUrl(string bangumiid)
        {
            return $"{Constants.Server}/v0/subjects/{bangumiid}";
        }
        private static string GetSearchSubjectUrl(string keyword)
        {
            return $"{Constants.Server}/search/subject/{Uri.EscapeDataString(keyword)}?responseGroup=large&type=2";
        }
        private static string GetSubjectPersonsUrl(string bangumiid)
        {
            return $"{Constants.Server}/v0/subjects/{bangumiid}/persons";
        }
        private static string GetSubjectCharactersUrl(string bangumiid)
        {
            return $"{Constants.Server}/v0/subjects/{bangumiid}/characters";
        }
        private static string GetEpisodeUrl(string bangumiid)
        {
            return $"{Constants.Server}/v0/episodes/{bangumiid}";
        }

        private static string GetEpisodesUrl(string bangumiid, int indexNumber)
        {
            int offset = indexNumber - 50;
            offset = offset <=0 ? 0 : offset;
            return $"{Constants.Server}/v0/episodes?subject_id={bangumiid}&limit=100&offset={offset}";
        }


    }


}
