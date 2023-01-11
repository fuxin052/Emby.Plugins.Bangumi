/*
 * Copyright (c) 2014-2017, Eren Okka
 * Copyright (c) 2016-2017, Paul Miller
 * Copyright (c) 2017-2018, Tyler Bratton
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.Plugins.Bangumi.Modules.AnitomySharp
{

  /// <summary>
  /// A class to manager the list of known anime keywords. This class is analogous to <code>keyword.cpp</code> of Anitomy, and <code>KeywordManager.java</code> of AnitomyJ
  /// </summary>
  public static class KeywordManager
  {
    private static readonly Dictionary<string, Keyword> Keys = new Dictionary<string, Keyword>();
    private static readonly Dictionary<string, Keyword> Extensions = new Dictionary<string, Keyword>();
    private static readonly List<Tuple<Element.ElementCategory, List<string>>> PeekEntries;

    static KeywordManager()
    {
      var optionsDefault = new KeywordOptions();
      var optionsInvalid = new KeywordOptions(true, true, false);
      var optionsUnidentifiable = new KeywordOptions(false, true, true);
      var optionsUnidentifiableInvalid = new KeywordOptions(false, true, false);
      var optionsUnidentifiableUnsearchable = new KeywordOptions(false, false, true);

      Add(Element.ElementCategory.ElementAnimeSeasonPrefix,
        optionsUnidentifiable,
        new List<string> {"SAISON", "Saison", "saison", "SEASON", "Season", "season"});

      Add(Element.ElementCategory.ElementAnimeType,
        optionsUnidentifiable,
        new List<string> {"GEKIJOUBAN", "MOVIE", "OAD", "OAV", "ONA", "OVA", "SPECIAL", "SPECIALS", "TV", "SP", "番外編", "總集編"});

//       add "SP" to ElementAnimeType with optionsUnidentifiable
//       Add(Element.ElementCategory.ElementAnimeType,
//         optionsUnidentifiableUnsearchable,
//         new List<string> {"SP"}); // e.g. "Yumeiro Patissiere SP Professional"

      Add(Element.ElementCategory.ElementAnimeType,
        optionsUnidentifiableInvalid,
        new List<string> {"ED", "ENDING", "NCED", "NCOP", "OP", "OPENING", "PREVIEW", "PV"});

      Add(Element.ElementCategory.ElementAudioTerm,
        optionsDefault,
        new List<string> {
        // Audio channels
        "2.0CH", "2CH", "5.1", "5.1CH", "7.1", "7.1CH", "6CH",
        "DTS", "DTS-ES", "DTS-MA", "DTS5.1", "DTS-HD", "MA.5.1", "MA.2.0", "MA.7.1",
        "TRUEHD5.1", "TRUE-HD", "TRUEHD", "THD",
        // Audio codec
        "AAC", "AACX2", "AACX3", "AACX4", "AC3", "EAC3", "E-AC-3",
        "FLAC", "FLACX2", "FLACX3", "FLACX4", "LOSSLESS", "MP3", "OGG", "VORBIS",
        "ATOMS",
        // Audio language
        "DUALAUDIO", "DUAL AUDIO"
      });

      Add(Element.ElementCategory.ElementDeviceCompatibility,
        optionsDefault,
        new List<string> {"IPAD3", "IPHONE5", "IPOD", "PS3", "PS3アプコン", "XBOX", "XBOX360"});

      Add(Element.ElementCategory.ElementDeviceCompatibility,
        optionsUnidentifiable,
        new List<string> {"ANDROID"});

      Add(Element.ElementCategory.ElementEpisodePrefix,
        optionsDefault,
        new List<string> {"EP", "EP.", "EPS", "EPS.", "EPISODE", "EPISODE.", "EPISODES", "CAPITULO", "EPISODIO", "FOLGE"});

      Add(Element.ElementCategory.ElementEpisodePrefix,
        optionsInvalid,
        new List<string> {"E", "\\x7B2C"}); // single-letter episode keywords are not valid tokens

      Add(Element.ElementCategory.ElementFileExtension,
        optionsDefault,
        new List<string> {"3GP", "AVI", "DIVX", "FLV", "M2TS", "MKV", "MOV", "MP4", "MPG", "OGM", "RM", "RMVB", "TS", "WEBM", "WMV"});

      Add(Element.ElementCategory.ElementFileExtension,
        optionsInvalid,
        new List<string> {"AAC", "AIFF", "FLAC", "M4A", "MP3", "MKA", "OGG", "WAV", "WMA", "7Z", "RAR", "ZIP", "ASS", "SRT"});

      Add(Element.ElementCategory.ElementLanguage,
        optionsDefault,
        new List<string> {"ENG", "ENGLISH", "ESPANO", "JAP", "PT-BR", "SPANISH", "VOSTFR", "zh-Hans", "zh-Hant", "CHS", "CHT", "CHN", "JPN", "JPSC", "JPTC"});

      Add(Element.ElementCategory.ElementLanguage,
        optionsUnidentifiable,
        new List<string> {"ESP", "ITA"}); // e.g. "Tokyo ESP:, "Bokura ga Ita"

      Add(Element.ElementCategory.ElementOther,
        optionsDefault,
        new List<string> {"REMASTER", "REMASTERED", "UNCENSORED", "UNCUT", "TS", "VFR", "WIDESCREEN", "WS"});

      Add(Element.ElementCategory.ElementReleaseGroup,
        optionsDefault,
        new List<string> {
          // rip group
          "AI-Raws","A.I.R.nesSub","Airota","ANK-Raws","ANK","ANE","Akatomba-Raws","ATTKC","BeanSub","Beatrice-Raws",
          "CASO","CoolComic","Commie","DanNi","DMG","Dymy","Eupho","EMTP-Raws","EnkanRec","Exiled-Destiny","FLsnow",
          "FREEWIND","FUDAN_NRC","FZSD","GTX-Raws","GST","Hakugetsu","HQR","HKG","JYFanSub","Jsum","Kagura","Kametsu",
          "Kamigami-Raws","Kamigami","诸神字幕组","KNA-Subs","KoeiSub","KTXP","LowPower-Raws","LKSUB","Lilith-Raws",
          "Liuyun","LoliHouse","LittleBakas!","Mabors","mawen1250","MGRT","MMZY-Sub","MH","Moozzi2","Nekomoe kissaten",
          "Pussub","POPGO","philosophy-raws","PPP-Raw","QTS","RARBG","RATH","ReinForce","RUELL-Next","RUELL-Raws",
          "r1RAW","Snow-Raws","SFEO-Raws","Shinsen-Subs","Shirokoi","SweetSub","SumiSora","SOFCJ-Raws","T.H.X","TSDM",
          "THORA","TUcaptions","TxxZ","UCCUSS","UHA-WINGS","U2-RIP","VCB-Studio","VCB-S","x_x","xyx98","XKsub","Xrip",
          "异域-11番小队","YYDM","Yusyabu","YlbudSub","Yuuki","Zagzad",
          "HYSUB", "Sakurato", "Skymoon-Raws", "Comicat&KissSub",
          // bangumi
          "ANi", "NC-Raws", "Lilith-Raws", "NaN-Raws",
          // other
          "脸肿字幕组","魔穗字幕组","桜都字幕组","Maho.sub","MahoXOkazu","Okazu.Sub","Thunder.Sub"
        });

      // UPPER
      Add(Element.ElementCategory.ElementReleaseInformation,
        optionsDefault,
        new List<string> {"BATCH", "COMPLETE", "PATCH", "REMUX", "REV", "REPACK", "FIN",
        "生肉", "熟肉", "18禁", "18禁アニメ", "15禁", "無修正", "无修正", "无码", "無碼", "有码", "NO WATERMARK", "有码", "有码",
        "BILIBILI", "BAHA", "GYAO!"});

      Add(Element.ElementCategory.ElementReleaseInformation,
        optionsUnidentifiable,
        new List<string> {"END", "FINAL"}); // e.g. "The End of Evangelion", 'Final Approach"

      Add(Element.ElementCategory.ElementReleaseVersion,
        optionsDefault,
        new List<string> {"V0", "V1", "V2", "V3", "V4"});

      Add(Element.ElementCategory.ElementSource,
        optionsDefault,
        new List<string> {"BD", "BDRIP", "BD-BOX", "BDBOX", "UHD", "UHDRIP", "BLURAY", "BLU-RAY",
        "DVD", "DVD5", "DVD9", "DVD-R2J", "DVDRIP", "DVD-RIP",
        "R2DVD", "R2J", "R2JDVD", "R2JDVDRIP",
        "HDTV", "HDTVRIP", "TVRIP", "TV-RIP",
        "WEBCAST", "WEBRIP", "WEB-DL", "WEB",
        "DLRIP"});

      Add(Element.ElementCategory.ElementSubtitles,
        optionsDefault,
        new List<string> {"ASS", "GB", "BIG5", "DUB", "DUBBED", "HARDSUB", "HARDSUBS", "RAW", "SOFTSUB", "SOFTSUBS", "SUB", "SUBBED", "SUBTITLED"});

      Add(Element.ElementCategory.ElementVideoTerm,
        optionsDefault,
        new List<string> {
          // Frame rate
          "23.976FPS", "24FPS", "29.97FPS", "30FPS", "60FPS", "120FPS",
          "SVFI",
          // Video codec
          "8BIT", "8-BIT", "10BIT", "10BITS", "10-BIT", "10-BITS",
          "HI10", "HI10P", "MA10P", "HI444", "HI444P", "HI444PP",
          "H264", "H265", "H.264", "H.265", "X264", "X265", "X.264",
          "AVC", "HEVC", "HEVC2", "DIVX", "DIVX5", "DIVX6", "XVID",
          "YUV420", "YUV420P8", "YUV420P10", "YUV420P10LE", "YUV444", "YUV444P10", "YUV444P10LE",
          "Main10", "Main10p", "Main12", "Main12p",
          "HDR", "HDR10", "HMAX",
          // Video format
          "AVI", "RMVB", "WMV", "WMV3", "WMV9", "MKV", "MP4", "MPEG",
          // Video quality
          "HQ", "LQ",
          // Video resolution
          "UHD", "HD", "SD"});

      Add(Element.ElementCategory.ElementVolumePrefix,
        optionsDefault,
        new List<string> {"VOL", "VOL.", "VOLUME"});

      PeekEntries = new List<Tuple<Element.ElementCategory, List<string>>>
      {
        Tuple.Create(Element.ElementCategory.ElementVideoTerm, new List<string> { "HEVC-10bit", "HEVC-YUV420P10","X264-10bit","x264-10bit", "x264-Hi10P" }),
        Tuple.Create(Element.ElementCategory.ElementAudioTerm, new List<string> { "Dual Audio" }),
        Tuple.Create(Element.ElementCategory.ElementVideoTerm, new List<string> { "H264", "H.264", "h264", "h.264" }),
        Tuple.Create(Element.ElementCategory.ElementVideoResolution, new List<string> { "480p", "720p", "1080p", "2160p", "4k", "6k", "8k", "480P", "720P", "1080P", "2160P", "4K", "6K", "8K" }),
        Tuple.Create(Element.ElementCategory.ElementSource, new List<string> { "Blu-Ray" })
      };
    }

    public static string Normalize(string word)
    {
      return string.IsNullOrEmpty(word) ? word : word.ToUpperInvariant();
    }

    public static bool Contains(Element.ElementCategory category, string keyword)
    {
      var keys = GetKeywordContainer(category);
      if (keys.TryGetValue(keyword, out var foundEntry))
      {
        return foundEntry.Category == category;
      }

      return false;
    }

    /// <summary>
    /// Finds a particular <code>keyword</code>. If found sets <code>category</code> and <code>options</code> to the found search result.
    /// </summary>
    /// <param name="keyword">the keyword to search for</param>
    /// <param name="category">the reference that will be set/changed to the found keyword category</param>
    /// <param name="options">the reference that will be set/changed to the found keyword options</param>
    /// <returns>if the keyword was found</returns>
    public static bool FindAndSet(string keyword, ref Element.ElementCategory category, ref KeywordOptions options)
    {
      var keys = GetKeywordContainer(category);
      if (!keys.TryGetValue(keyword, out var foundEntry))
      {
        return false;
      }

      if (category == Element.ElementCategory.ElementUnknown)
      {
        category = foundEntry.Category;
      }
      else if (foundEntry.Category != category)
      {
        return false;
      }
      options = foundEntry.Options;
      return true;
    }

    /// <summary>
    /// Given a particular <code>filename</code> and <code>range</code> attempt to preidentify the token before we attempt the main parsing logic
    /// </summary>
    /// <param name="filename">the filename</param>
    /// <param name="range">the search range</param>
    /// <param name="elements">elements array that any pre-identified elements will be added to</param>
    /// <param name="preidentifiedTokens">elements array that any pre-identified token ranges will be added to</param>
    public static void PeekAndAdd(string filename, TokenRange range, List<Element> elements, List<TokenRange> preidentifiedTokens)
    {
      var endR = range.Offset + range.Size;
      var search = filename.Substring(range.Offset, endR > filename.Length ? filename.Length - range.Offset : endR - range.Offset);
      foreach (var entry in PeekEntries)
      {
        foreach (var keyword in entry.Item2)
        {
          var foundIdx = search.IndexOf(keyword, StringComparison.CurrentCulture);
          if (foundIdx == -1) continue;
          foundIdx += range.Offset;
          elements.Add(new Element(entry.Item1, keyword));
          preidentifiedTokens.Add(new TokenRange(foundIdx, keyword.Length));
        }
      }
    }

    // Private API

    /** Returns the appropriate keyword container. */
    private static Dictionary<string, Keyword> GetKeywordContainer(Element.ElementCategory category)
    {
      return category == Element.ElementCategory.ElementFileExtension ? Extensions : Keys;
    }

    /// Adds a <code>category</code>, <code>options</code>, and <code>keywords</code> to the internal keywords list.
    private static void Add(Element.ElementCategory category, KeywordOptions options, IEnumerable<string> keywords)
    {
      var keys = GetKeywordContainer(category);
      foreach (var key in keywords.Where(k => !string.IsNullOrEmpty(k) && !keys.ContainsKey(k)))
      {
        keys[key] = new Keyword(category, options);
      }
    }
  }

  /// <summary>
  /// Keyword options for a particular keyword.
  /// </summary>
  public class KeywordOptions
  {
    public bool Identifiable { get; }
    public bool Searchable { get; }
    public bool Valid { get; }

    public KeywordOptions() : this(true, true, true) {}

    /// <summary>
    /// Constructs a new keyword options
    /// </summary>
    /// <param name="identifiable">if the token is identifiable</param>
    /// <param name="searchable">if the token is searchable</param>
    /// <param name="valid">if the token is valid</param>
    public KeywordOptions(bool identifiable, bool searchable, bool valid)
    {
      Identifiable = identifiable;
      Searchable = searchable;
      Valid = valid;
    }

  }

  /// <summary>
  /// A Keyword
  /// </summary>
  public struct Keyword
  {
    public readonly Element.ElementCategory Category;
    public readonly KeywordOptions Options;

    /// <summary>
    /// Constructs a new Keyword
    /// </summary>
    /// <param name="category">the category of the keyword</param>
    /// <param name="options">the keyword's options</param>
    public Keyword(Element.ElementCategory category, KeywordOptions options)
    {
      Category = category;
      Options = options;
    }
  }
}
