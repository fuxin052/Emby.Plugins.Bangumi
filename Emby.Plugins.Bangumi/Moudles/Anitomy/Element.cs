/*
 * Copyright (c) 2014-2017, Eren Okka
 * Copyright (c) 2016-2017, Paul Miller
 * Copyright (c) 2017-2018, Tyler Bratton
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Emby.Plugins.Bangumi.Modules.AnitomySharp
{
 /// <summary>
 /// An <see cref="Element"/> represents an identified Anime <see cref="Token"/>.
 /// A single filename may contain multiple of the same
 /// token(e.g <see cref="ElementCategory.ElementEpisodeNumber"/>).
 /// </summary>
  public class Element
  {
    /** Element Categories */
    public enum ElementCategory
    {
      ElementAnimeSeason,
      ElementAnimeSeasonPrefix,
      ElementAnimeTitle,
      ElementAnimeType,
      ElementAnimeYear,
      ElementAudioTerm,
      ElementDeviceCompatibility,
      ElementEpisodeNumber,
      ElementEpisodeNumberAlt,
      ElementEpisodePrefix,
      ElementEpisodeTitle,
      ElementFileChecksum,
      ElementFileExtension,
      ElementFileName,
      ElementLanguage,
      ElementOther,
      ElementReleaseGroup,
      ElementReleaseInformation,
      ElementReleaseVersion,
      ElementSource,
      ElementSubtitles,
      ElementVideoResolution,
      ElementVideoTerm,
      ElementVolumeNumber,
      ElementVolumePrefix,
      ElementUnknown
    }

    public ElementCategory Category { get; set; }
    public string Value { get; }

    /// <summary>
    ///  Constructs a new Element
    /// </summary>
    /// <param name="category">the category of the element</param>
    /// <param name="value">the element's value</param>
    public Element(ElementCategory category, string value)
    {
      Category = category;
      Value = value;
    }

    public override int GetHashCode()
    {
      return -1926371015 + Value.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (this == obj)
      {
        return true;
      }

      if (obj == null || GetType() != obj.GetType())
      {
        return false;
      }

      var other = (Element) obj;
      return Category.Equals(other.Category);
    }

    public override string ToString()
    {
      return $"Element{{category={Category}, value='{Value}'}}";
    }
  }
}