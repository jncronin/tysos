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

#include <efi.h>
#include <efilib.h>
#include <stdio.h>
#include <errno.h>

SERIAL_IO_INTERFACE *efilibc_serial_iface;

int efilibc_setup_serial(int port_no)
{
	EFI_STATUS s;
	UINTN nohandles;
	EFI_HANDLE *buffer;

	s = BS->LocateHandleBuffer(ByProtocol, &SerialIoProtocol, NULL, &nohandles, &buffer);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efilibc: efilibc_setup_serial(%i): error: LocateHandleBuffer failed: %i\n", port_no, s);
		return -1;
	}

	if((UINTN)port_no < nohandles)
	{
		s = BS->HandleProtocol(buffer[port_no], &SerialIoProtocol, (void **)&efilibc_serial_iface);
		if(EFI_ERROR(s))
		{
			fprintf(stderr, "efilibc: efilibc_setup_serial(%i): error: HandleProtocol failed: %i\n", port_no, s);
			return -1;
		}
		efilibc_serial_iface->SetAttributes(efilibc_serial_iface, 0, 0, 0, NoParity, 8, OneStopBit);
		BS->FreePool(buffer);
		return 0;
	}
	else
	{
		BS->FreePool(buffer);
		return -1;
	}
}

size_t efilibc_serial_fwrite(const void *ptr, size_t size, size_t nmemb, void *data)
{
	EFI_STATUS s;
	(void)data;

	if(efilibc_serial_iface == NULL)
	{
		/* Try and setup the serial port */
		efilibc_setup_serial(0);

		if(efilibc_serial_iface == NULL)
			return EOF;
	}

	/* Write data */
	UINTN buf_size = size * nmemb;
	s = efilibc_serial_iface->Write(efilibc_serial_iface, &buf_size, (void *)ptr);
	if(s == EFI_TIMEOUT)
	{
		errno = ETIMEDOUT;
		return 0;
	}
	else if(EFI_ERROR(s))
	{
		errno = EFAULT;
		return 0;
	}
	else
		return buf_size / size;
}
