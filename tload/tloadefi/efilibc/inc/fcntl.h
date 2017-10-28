/* Copyright (C) 2014 by John Cronin
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

#ifndef FCNTL_H
#define FCNTL_H

#define O_CLOEXEC		0x1
#define O_CREAT			0x2
#define O_DIRECTORY		0x4
#define O_EXCL			0x8
#define O_NOCTTY		0x10
#define O_NOFOLLOW		0x20
#define O_TRUNC			0x40
#define O_TTY_INIT		0x80
#define O_APPEND		0x100
#define O_DSYNC			0x200
#define O_NONBLOCK		0x400
#define O_RSYNC			0x800
#define O_SYNC			0x1000
#define O_ACCMODE		0xff0000
#define O_EXEC			0x10000
#define	O_RDONLY		0x20000
#define O_RDWR			0x40000
#define O_SEARCH		0x80000
#define O_WRONLY		0x100000

int open(const char *pathname, int flags, ...);
ssize_t read(int fildes, void *buf, size_t nbyte);
int close(int fildes);
ssize_t write(int fildes, const void *buf, size_t nbyte);

#endif
