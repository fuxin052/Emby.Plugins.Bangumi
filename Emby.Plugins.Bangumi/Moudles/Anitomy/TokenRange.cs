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
    public struct TokenRange
    {
	    public int Offset;
	    public int Size;

      public TokenRange(int offset, int size)
      {
        Offset = offset;
        Size = size;
      }
    }
}
