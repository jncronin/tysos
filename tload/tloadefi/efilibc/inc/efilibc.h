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

#ifndef EFILIBC_H
#define EFILIBC_H

#include <efi.h>
#include <efilib.h>
#include <stddef.h>

typedef size_t (*fwrite_func)(const void *ptr, size_t size, size_t nmemb, void *data);
typedef size_t (*fread_func)(void *ptr, size_t size, size_t nmemb, void *data);

int efilibc_init(EFI_HANDLE ImageHandle);
int efilibc_register_stdout_fwrite(fwrite_func func, void *data);
int efilibc_register_stderr_fwrite(fwrite_func func, void *data);
int efilibc_register_stdin_fread(fread_func func, void *data);
size_t efilibc_console_fwrite(const void *ptr, size_t size, size_t nmemb, void *data);
size_t efilibc_serial_fwrite(const void *ptr, size_t size, size_t nmemb, void *data);
size_t efilibc_console_fread(void *ptr, size_t size, size_t nmemb, void *data);
int efilibc_setup_serial(int port_no);
void efilibc_register_external_malloc(void *(*my_ext_malloc)(size_t));

#endif
