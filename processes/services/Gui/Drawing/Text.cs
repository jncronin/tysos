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

namespace Gui.Drawing
{
    public class Text
    {
        public static Rectangle GetTextExtents(Buffer buffer, Font font, string text)
        {
            if (buffer.PixelFormat == Buffer.PixelFormatType.PF_16_8CHAR_8IDX)
            {
                Rectangle ret = new Rectangle();
                ret.X = 0;
                ret.Y = 0;
                ret.Width = text.Length;
                ret.Height = 1;
                return ret;
            }
            throw new NotSupportedException();
        }

        public static Rectangle GetTextExtents(Buffer buffer, Font font, char ch)
        {
            if (buffer.PixelFormat == Buffer.PixelFormatType.PF_16_8CHAR_8IDX)
            {
                Rectangle ret = new Rectangle();
                ret.X = 0;
                ret.Y = 0;
                ret.Width = 1;
                ret.Height = 1;
                return ret;
            }
            throw new NotSupportedException();
        }

        public static void DrawText(Buffer buffer, Font font, Brush brush, int x, int y, char ch)
        {
            if (buffer.PixelFormat == Buffer.PixelFormatType.PF_16_8CHAR_8IDX)
            {
                int buf_idx = (x + y * buffer.Width) * 2;
                buffer.Data[buf_idx] = (byte)ch;
                buffer.Data[buf_idx + 1] = 0x07;
            }
        }

        public static void DrawText(Buffer buffer, Font font, Brush brush, int x, int y, string text)
        {
            if (buffer.PixelFormat == Buffer.PixelFormatType.PF_16_8CHAR_8IDX)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    int buf_idx = (x + y * buffer.Width) * 2;
                    buffer.Data[buf_idx] = (byte)(text[i]);
                    buffer.Data[buf_idx + 1] = 0x07;

                    x++;
                    if (x >= buffer.Width)
                    {
                        x = 0;
                        y++;
                    }
                }
            }
        }
    }
}
