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

namespace Emby.Plugins.Bangumi.Modules.AnitomySharp
{

  /// <summary>
  /// A library capable of parsing Anime filenames
  /// This code is a  C++ to C# port of <see href="https://github.com/erengy/anitomy">Anitomy</see>>,
  /// using the already existing Java port <see href="https://github.com/Vorror/anitomyJ">AnitomyJ</see> as a reference.
  /// </summary>
  public class AnitomySharp
  {
    private AnitomySharp() {}

    /// <summary>
    /// Parses an anime <see cref="filename"/> into its constituent elements.
    /// </summary>
    /// <param name="filename">the anime file name</param>
    /// <param name="options">the options to parse with, use Parse(filename) to use default options</param>
    /// <returns>the list of parsed elements</returns>
    public static IEnumerable<Element> Parse(string filename, Options options)
    {
      var elements = new List<Element>(32);
      var tokens = new List<Token>();

      /** remove/parse extension */
      var fname = filename;
      if (options.ParseFileExtension)
      {
        var extension = "";
        if (RemoveExtensionFromFilename(ref fname, ref extension))
        {
          elements.Add(new Element(Element.ElementCategory.ElementFileExtension, extension));
        }
      }

      /** set filename */
      if (string.IsNullOrEmpty(filename))
      {
        return elements;
      }
      elements.Add(new Element(Element.ElementCategory.ElementFileName, fname));

      /** tokenize */
      var isTokenized = new Tokenizer(fname, elements, options, tokens).Tokenize();
      if (!isTokenized)
      {
        return elements;
      }
      new Parser(elements, options, tokens).Parse();
      return elements;
    }

    /// <summary>
    /// Parses an anime <see cref="filename"/> into its consituent elements.
    /// </summary>
    /// <param name="filename">the anime file name</param>
    /// <returns>the list of parsed elements</returns>
    public static IEnumerable<Element> Parse(string filename)
    {
      return Parse(filename, new Options());
    }

    /// <summary>
    /// Removes the extension from the <see cref="filename"/>
    /// </summary>
    /// <param name="filename">the ref that will be updated with the new filename</param>
    /// <param name="extension">the ref that will be updated with the file extension</param>
    /// <returns>if the extension was successfully separated from the filename</returns>
    private static bool RemoveExtensionFromFilename(ref string filename, ref string extension)
    {
      int position;
      if (string.IsNullOrEmpty(filename) || (position = filename.LastIndexOf('.')) == -1)
      {
        return false;
      }

      /** remove file extension */
      extension = filename.Substring(position + 1);
      if (extension.Length > 4 || !extension.All(char.IsLetterOrDigit))
      {
        return false;
      }

      /** check if valid anime extension */
      var keyword = KeywordManager.Normalize(extension);
      if (!KeywordManager.Contains(Element.ElementCategory.ElementFileExtension, keyword))
      {
        return false;
      }

      filename = filename.Substring(0, position);
      return true;
    }
  }
}