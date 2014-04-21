/* Copyright (C) 2011 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Gui
{
    class KeyMap
    {
        /* Defined keymaps */
        static readonly char[] gb_keymap = new char[]
        {
            (char)0, (char)0, '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', '=', (char)ConsoleKey.Backspace, '\t',
            'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p', '[', ']', '\n', (char)0, 'a', 's',
            'd', 'f', 'g', 'h', 'j', 'k', 'l', ';', '\'', '`', (char)0, '#', 'z', 'x', 'c', 'v',
            'b', 'n', 'm', ',', '.', '/', (char)0, '*', (char)0, ' ', (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
            (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, '7', '8', '9', '-', '4', '5', '6', '+', '1',
            '2', '3', '0', '.', (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
            (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
            (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
            'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P', '{', '}', (char)0, (char)0, (char)0, (char)0,
            'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', ':', '@', '¬', (char)0, (char)0, (char)0, (char)0,
            'Z', 'X', 'C', 'V', 'B', 'N', 'M', '<', '>', '?', (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
            '!', '"', '£', '$', '%', '^', '&', '*', '(', ')', '_', '+', (char)0, (char)0, (char)0, (char)0,
            '~', (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
            (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
            (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0,
            (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0
        };

        static char[] cur_keymap = gb_keymap;

        internal static char DecodeKey(ushort tysos_scancode)
        {
            return cur_keymap[tysos_scancode];
        }
    }
}
