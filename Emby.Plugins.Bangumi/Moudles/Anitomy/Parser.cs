/*
 * Copyright (c) 2014-2017, Eren Okka
 * Copyright (c) 2016-2017, Paul Miller
 * Copyright (c) 2017-2018, Tyler Bratton
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Emby.Plugins.Bangumi.Modules.AnitomySharp
{
  /// <summary>
  /// Class to classify <see cref="Token"/>s
  /// </summary>
  public class Parser
  {
    public bool IsEpisodeKeywordsFound { get; private set; }
    public ParserHelper ParseHelper { get; }
    public ParserNumber ParseNumber { get; }
    public List<Element> Elements { get; }
    public List<Token> Tokens { get; }
    private Options Options { get; }

    /// <summary>
    /// Constructs a new token parser
    /// </summary>
    /// <param name="elements">the list where parsed elements will be added</param>
    /// <param name="options">the parser options</param>
    /// <param name="tokens">the list of tokens</param>
    public Parser(List<Element> elements, Options options, List<Token> tokens)
    {
      Elements = elements;
      Options = options;
      Tokens = tokens;
      ParseHelper = new ParserHelper(this);
      ParseNumber = new ParserNumber(this);
    }

    /** Begins the parsing process */
    public bool Parse()
    {
      SearchForKeywords();
      SearchForIsolatedNumbers();

      if (Options.ParseEpisodeNumber)
      {
        SearchForEpisodeNumber();
      }

      SearchForAnimeTitle();

      if (Options.ParseReleaseGroup && Empty(Element.ElementCategory.ElementReleaseGroup))
      {
        SearchForReleaseGroup();
      }

      if (Options.ParseEpisodeTitle && !Empty(Element.ElementCategory.ElementEpisodeNumber))
      {
        SearchForEpisodeTitle();
      }

      ValidateElements();
      return Empty(Element.ElementCategory.ElementAnimeTitle);
    }

    /** Search for anime keywords. */
    private void SearchForKeywords()
    {
      for (var i = 0; i < Tokens.Count; i++)
      {
        var token = Tokens[i];
        if (token.Category != Token.TokenCategory.Unknown) continue;

        var word = token.Content;
        word = word.Trim(" -".ToCharArray());
        if (string.IsNullOrEmpty(word)) continue;

        // Don't bother if the word is a number that cannot be CRC
        if (word.Length != 8 && StringHelper.IsNumericString(word)) continue;

        var keyword = KeywordManager.Normalize(word);
        var category = Element.ElementCategory.ElementUnknown;
        var options = new KeywordOptions();

        if (KeywordManager.FindAndSet(keyword, ref category, ref options))
        {
          if (!Options.ParseReleaseGroup && category == Element.ElementCategory.ElementReleaseGroup) continue;
          if (!ParseHelper.IsElementCategorySearchable(category) || !options.Searchable) continue;
          if (ParseHelper.IsElementCategorySingular(category) && !Empty(category)) continue;
          switch (category)
          {
            case Element.ElementCategory.ElementAnimeSeasonPrefix:
              ParseHelper.CheckAndSetAnimeSeasonKeyword(token, i);
              continue;
            case Element.ElementCategory.ElementEpisodePrefix when options.Valid:
              ParseHelper.CheckExtentKeyword(Element.ElementCategory.ElementEpisodeNumber, i, token);
              continue;
            case Element.ElementCategory.ElementReleaseVersion:
              word = word.Substring(1);
              break;
            case Element.ElementCategory.ElementVolumePrefix:
              ParseHelper.CheckExtentKeyword(Element.ElementCategory.ElementVolumeNumber, i, token);
              continue;
          }
        }
        else
        {
          if (Empty(Element.ElementCategory.ElementFileChecksum) && ParserHelper.IsCrc32(word))
          {
            category = Element.ElementCategory.ElementFileChecksum;
          } else if (Empty(Element.ElementCategory.ElementVideoResolution) && ParserHelper.IsResolution(word))
          {
            category = Element.ElementCategory.ElementVideoResolution;
          }
        }

        if (category == Element.ElementCategory.ElementUnknown) continue;
        Elements.Add(new Element(category, word));
        if (options.Identifiable)
        {
          token.Category = Token.TokenCategory.Identifier;
        }
      }
    }

    /** Search for episode number. */
    private void SearchForEpisodeNumber()
    {
      // List all unknown tokens that contain a number
      var tokens = new List<int>();
      for (var i = 0; i < Tokens.Count; i++)
      {
        var token = Tokens[i];
        if (token.Category == Token.TokenCategory.Unknown &&
            ParserHelper.IndexOfFirstDigit(token.Content) != -1)
        {
          tokens.Add(i);
        }
      }

      if (tokens.Count == 0) return;

      IsEpisodeKeywordsFound = !Empty(Element.ElementCategory.ElementEpisodeNumber);

      // If a token matches a known episode pattern, it has to be the episode number
      if (ParseNumber.SearchForEpisodePatterns(tokens)) return;

      // We have previously found an episode number via keywords
      if (!Empty(Element.ElementCategory.ElementEpisodeNumber)) return;

      // From now on, we're only interested in numeric tokens
      tokens.RemoveAll(r => !StringHelper.IsNumericString(Tokens[r].Content));

      // e.g. "01 (176)", "29 (04)"
      if (ParseNumber.SearchForEquivalentNumbers(tokens)) return;

      // e.g. " - 08"
      if (ParseNumber.SearchForSeparatedNumbers(tokens)) return;

      // "e.g. "[12]", "(2006)"
      if (ParseNumber.SearchForIsolatedNumbers(tokens)) return;

      // Consider using the last number as a last resort
      ParseNumber.SearchForLastNumber(tokens);
    }

    /// <summary>
    /// Search for anime title
    /// </summary>
    private void SearchForAnimeTitle()
    {
      var enclosedTitle = false;

      var tokenBegin = Token.FindToken(Tokens, 0, Tokens.Count, Token.TokenFlag.FlagNotEnclosed, Token.TokenFlag.FlagUnknown);

      // without ReleaseGroup, only anime title e.g. "[2005][Paniponi Dash!][BDRIP][1080P][1-26Fin+OVA+SP]"
      var tokenBeginWithNoReleaseGroup = Tokens.Count;

      // If that doesn't work, find the first unknown token in the second enclosed
      // group, assuming that the first one is the release group
      if (!Token.InListRange(tokenBegin, Tokens))
      {
        tokenBegin = 0;
        enclosedTitle = true;
        var skippedPreviousGroup = false;

        do
        {
          tokenBegin = Token.FindToken(Tokens, tokenBegin, Tokens.Count, Token.TokenFlag.FlagUnknown);
          tokenBeginWithNoReleaseGroup = tokenBegin;
          if (!Token.InListRange(tokenBegin, Tokens)) break;

          // Ignore groups that are composed of non-Latin characters or non-Chinese characters
          // 对于同时有中英文名称，并且两者分割开来，如：“[異域字幕組][漆黑的子彈][Black Bullet][11][1280x720][繁体].mp4”，则只会返回第一个匹配到的
          if ((StringHelper.IsMostlyLatinString(Tokens[tokenBegin].Content) || StringHelper.IsMostlyChineseString(Tokens[tokenBegin].Content) ) && skippedPreviousGroup)
          {
            break;
          }

          // if ReleaseGroup is empty
          if (Options.ParseReleaseGroup && Empty(Element.ElementCategory.ElementReleaseGroup))
          {
            // Get the first unknown token of the next group
            tokenBegin = Token.FindToken(Tokens, tokenBegin, Tokens.Count, Token.TokenFlag.FlagBracket);
            tokenBegin = Token.FindToken(Tokens, tokenBegin, Tokens.Count, Token.TokenFlag.FlagUnknown);
          }
          // make sure the new token don't in Element.ElementCategory
          // if in or outListRange
          // return pretoken
          // TODO match other ElementCategory
          if ((Token.InListRange(tokenBegin, Tokens) && KeywordManager.Contains(Element.ElementCategory.ElementAnimeType, Tokens[tokenBegin].Content.ToUpper()))
            || tokenBegin == Tokens.Count){
            tokenBegin = tokenBeginWithNoReleaseGroup;
          }
          skippedPreviousGroup = true;
        } while (Token.InListRange(tokenBegin, Tokens));
      }

      if (!Token.InListRange(tokenBegin, Tokens)) return;

      // Continue until an identifier (or a bracket, if the title is enclosed) is found
      var tokenEnd = Token.FindToken(
        Tokens,
        tokenBegin,
        Tokens.Count,
        Token.TokenFlag.FlagIdentifier,
        enclosedTitle ? Token.TokenFlag.FlagBracket : Token.TokenFlag.FlagNone);

      // If within the interval there's an open bracket without its matching pair,
      // move the upper endpoint back to the bracket
      if (!enclosedTitle)
      {
        var lastBracket = tokenEnd;
        var bracketOpen = false;
        for (var i = tokenBegin; i < tokenEnd; i++)
        {
          if (Tokens[i].Category != Token.TokenCategory.Bracket) continue;
          lastBracket = i;
          bracketOpen = !bracketOpen;
        }

        if (bracketOpen) tokenEnd = lastBracket;
      }

      // If the interval ends with an enclosed group (e.g. "Anime Title [Fansub]"),
      // move the upper endpoint back to the beginning of the group. We ignore
      // parentheses in order to keep certain groups (e.g. "(TV)") intact.
      if (!enclosedTitle)
      {
        var token = Token.FindPrevToken(Tokens, tokenEnd, Token.TokenFlag.FlagNotDelimiter);

        while (ParseHelper.IsTokenCategory(token, Token.TokenCategory.Bracket) && Tokens[token].Content[0] != ')')
        {
          token = Token.FindPrevToken(Tokens, token, Token.TokenFlag.FlagBracket);
          if (!Token.InListRange(token, Tokens)) continue;
          tokenEnd = token;
          token = Token.FindPrevToken(Tokens, tokenEnd, Token.TokenFlag.FlagNotDelimiter);
        }
      }

      ParseHelper.BuildElement(Element.ElementCategory.ElementAnimeTitle, false, Tokens.GetRange(tokenBegin, tokenEnd - tokenBegin));
    }

    /// <summary>
    /// Search for release group
    /// </summary>
    private void SearchForReleaseGroup()
    {
      for (int tokenBegin = 0, tokenEnd = tokenBegin; tokenBegin < Tokens.Count;)
      {
        // Find the first enclosed unknown token
        tokenBegin = Token.FindToken(Tokens, tokenEnd, Tokens.Count, Token.TokenFlag.FlagEnclosed, Token.TokenFlag.FlagUnknown);
        if (!Token.InListRange(tokenBegin, Tokens)) return;

        // Continue until a bracket or identifier is found
        tokenEnd = Token.FindToken(Tokens, tokenBegin, Tokens.Count, Token.TokenFlag.FlagBracket, Token.TokenFlag.FlagIdentifier);
        // 去除纯数字发布组
        if (Regex.Match(Tokens[tokenBegin].Content, ParserNumber.RegexMatchOnlyStart + @"^[0-9]+$" + ParserNumber.RegexMatchOnlyEnd).Success) continue;
        if (!Token.InListRange(tokenEnd, Tokens) || Tokens[tokenEnd].Category != Token.TokenCategory.Bracket) continue;

        // Ignore if it's not the first non-delimiter token in group
        var prevToken = Token.FindPrevToken(Tokens, tokenBegin, Token.TokenFlag.FlagNotDelimiter);
        if (Token.InListRange(prevToken, Tokens) && Tokens[prevToken].Category != Token.TokenCategory.Bracket) continue;

        ParseHelper.BuildElement(Element.ElementCategory.ElementReleaseGroup, true, Tokens.GetRange(tokenBegin, tokenEnd - tokenBegin));
        return;
      }
    }

    /// <summary>
    /// Search for episode title
    /// </summary>
    private void SearchForEpisodeTitle()
    {
      int tokenBegin;
      var tokenEnd = 0;

      do
      {
        // Find the first non-enclosed unknown token
        tokenBegin = Token.FindToken(Tokens, tokenEnd, Tokens.Count, Token.TokenFlag.FlagNotEnclosed, Token.TokenFlag.FlagUnknown);
        if (!Token.InListRange(tokenBegin, Tokens)) return;

        // Continue until a bracket or identifier is found
        tokenEnd = Token.FindToken(Tokens, tokenBegin, Tokens.Count, Token.TokenFlag.FlagBracket, Token.TokenFlag.FlagIdentifier);

        // Ignore if it's only a dash
        if (tokenEnd - tokenBegin <= 2 && ParserHelper.IsDashCharacter(Tokens[tokenBegin].Content[0])) continue;
        //if (tokenBegin.Pos == null || tokenEnd.Pos == null) continue;
        ParseHelper.BuildElement(Element.ElementCategory.ElementEpisodeTitle, false, Tokens.GetRange(tokenBegin, tokenEnd - tokenBegin));
        return;
      } while (Token.InListRange(tokenBegin, Tokens));
    }

    /// <summary>
    /// Search for isolated numbers
    /// </summary>
    private void SearchForIsolatedNumbers()
    {
      for (var i = 0; i < Tokens.Count; i++)
      {
        var token = Tokens[i];
        var tokenContent = token.Content;

        // e.g. "2016-17"
        const string regexPattern = ParserNumber.RegexMatchOnlyStart + @"(\d{1,4})([-~&+])(\d{2,4})" + ParserNumber.RegexMatchOnlyEnd;
        var match = Regex.Match(token.Content, regexPattern);
        if (match.Success) {
          tokenContent = tokenContent.Split(match.Groups[2].Value)[0];
        }
        // add newtype e.g. "2021 OVA"
        if (token.Category != Token.TokenCategory.Unknown || !StringHelper.IsNumericString(tokenContent) ||
            !(ParseHelper.IsTokenContainAnimeType(i) ^ ParseHelper.IsTokenIsolated(i)))
        {
          continue;
        }

        var number = StringHelper.StringToInt(tokenContent);

        // Anime year
        if (number >= ParserNumber.AnimeYearMin && number <= ParserNumber.AnimeYearMax)
        {
          if (Empty(Element.ElementCategory.ElementAnimeYear))
          {
            Elements.Add(new Element(Element.ElementCategory.ElementAnimeYear, token.Content));
            token.Category = Token.TokenCategory.Identifier;
            continue;
          }
        }

        // Video resolution
        if (number != 480 && number != 720 && number != 1080 && number != 2160) continue;
        // If these numbers are isolated, it's more likely for them to be the
        // video resolution rather than the episode number. Some fansub groups use these without the "p" suffix.
        // if (!Empty(Element.ElementCategory.ElementVideoResolution)) continue;
        Elements.Add(new Element(Element.ElementCategory.ElementVideoResolution, token.Content));
        token.Category = Token.TokenCategory.Identifier;
      }
    }

    /// <summary>
    /// Validate Elements
    /// </summary>
    private void ValidateElements()
    {
      if (!Empty(Element.ElementCategory.ElementAnimeType) && !Empty(Element.ElementCategory.ElementEpisodeTitle))
      {
        var episodeTitle = Get(Element.ElementCategory.ElementEpisodeTitle);

        for (var i = 0; i < Elements.Count;)
        {
          var el = Elements[i];

          if (el.Category == Element.ElementCategory.ElementAnimeType)
          {
            if (episodeTitle.Contains(el.Value))
            {
              if (episodeTitle.Length == el.Value.Length)
              {
                Elements.RemoveAll(element =>
                  element.Category == Element.ElementCategory.ElementEpisodeTitle); // invalid episode title
              }
              else
              {
                var keyword = KeywordManager.Normalize(el.Value);
                if (KeywordManager.Contains(Element.ElementCategory.ElementAnimeType, keyword))
                {
                  i = Erase(el); // invalid anime type
                  continue;
                }
              }
            }
          }

          ++i;
        }
      }
    }

    /// <summary>
    /// Returns whether or not the parser contains this category
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    private bool Empty(Element.ElementCategory category)
    {
      return Elements.All(element => element.Category != category);
    }

    /// <summary>
    /// Returns the value of a particular category
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    private string Get(Element.ElementCategory category)
    {
      var foundElement = Elements.Find(element => element.Category == category);

      if (foundElement != null) return foundElement.Value;
      Element e = new Element(category, "");
      Elements.Add(e);
      foundElement = e;

      return foundElement.Value;
    }

    /// <summary>
    /// Deletes the first element with the same <code>element.Category</code> and returns the deleted element's position.
    /// </summary>
    private int Erase(Element element)
    {
      var removedIdx = -1;
      for (var i = 0; i < Elements.Count; i++)
      {
        var currentElement = Elements[i];
        if (element.Category != currentElement.Category) continue;
        removedIdx = i;
        Elements.RemoveAt(i);
        break;
      }

      return removedIdx;
    }
  }
}