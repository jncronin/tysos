/* Copyright (C) 2008 - 2011 by John Cronin
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

/* A buffer used for graphics display */

namespace Gui
{
    public class Buffer
    {
        int _width;
        int _height;

        public enum PixelFormatType
        {
            PF_16_8CHAR_8IDX,
            PF_8_8IDX,
            PF_32_8A_8R_8G_8B
        }

        PixelFormatType pf;

        public virtual byte[] Data { get { return buf; } }

        byte[] buf;

        public virtual int Width { get { return _width; } }
        public virtual int Height { get { return _height; } }
        public virtual PixelFormatType PixelFormat { get { return pf; } }

        public Buffer(int width, int height, PixelFormatType pixel_format)
        {
            _width = width;
            _height = height;
            pf = pixel_format;

            init_buffer();
        }

        public Buffer(Buffer b)
        {
            _width = b.Width;
            _height = b.Height;
            pf = b.PixelFormat;

            init_buffer();
        }

        void init_buffer()
        {
            int byte_length = _width * _height * get_pf_size(pf);
            buf = new byte[byte_length];
        }

        static int get_pf_size(PixelFormatType pixel_format)
        {
            switch (pixel_format)
            {
                case PixelFormatType.PF_16_8CHAR_8IDX:
                    return 2;
                case PixelFormatType.PF_32_8A_8R_8G_8B:
                    return 4;
                case PixelFormatType.PF_8_8IDX:
                    return 1;
            }

            throw new NotSupportedException();
        }

        static public void Blit(Buffer dest, Buffer src, int src_x, int src_y, int dest_x, int dest_y, int width, int height)
        {
            if (dest.pf != src.pf)
                throw new Exception("Source and destination pixel formats do not match");
            int pf_size = get_pf_size(dest.pf);

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    int src_i = src_x + i;
                    int src_j = src_y + j;
                    int dest_i = dest_x + i;
                    int dest_j = dest_y + j;

                    if (src_i < 0)
                        continue;
                    if (src_j < 0)
                        continue;
                    if (src_i >= src._width)
                        continue;
                    if (src_j >= src._height)
                        continue;

                    if (dest_i < 0)
                        continue;
                    if (dest_j < 0)
                        continue;
                    if (dest_i >= dest._width)
                        continue;
                    if (dest_j >= dest._height)
                        continue;

                    int src_idx = (src_i + src_j * src._width) * pf_size;
                    int dest_idx = (dest_i + dest_j * dest._width) * pf_size;

                    for(int k = 0; k < pf_size; k++)
                        dest.buf[dest_idx + k] = src.buf[src_idx + k];
                }
            }
        }
    }
}
