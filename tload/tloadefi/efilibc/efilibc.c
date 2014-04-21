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

#include <efilibc.h>
#include <stdio.h>

extern EFI_FILE *fopen_root;

int efilibc_init(EFI_HANDLE ImageHandle)
{
	/* Ensure the EFI_SYSTEM_TABLE *ST and EFI_BOOT_SERVICES *BS pointers are set */
	if(ST == NULL)
	{
		fprintf(stderr, "efilibc: efilibc_init(): error: please set EFI_SYSTEM_TABLE *ST prior to calling efilibc_init()\n");
		return -1;
	}
	if(BS == NULL)
	{
		fprintf(stderr, "efilibc: efilibc_init(): error: please set EFI_BOOT_SERVICES *BS prior to calling efilibc_init()\n");
		return -1;
	}

	efilibc_register_stderr_fwrite(efilibc_console_fwrite, NULL);

	/* Check arguments */
	if(ImageHandle == NULL)
	{
		fprintf(stderr, "efilibc: efilibc_init(): error: ImageHandle cannot be NULL\n");
		return -1;
	}

	/* Get the fopen_root pointer */
	EFI_FILE_IO_INTERFACE *sfs;
	EFI_LOADED_IMAGE *li;
	EFI_STATUS s;

	s = ST->BootServices->HandleProtocol(ImageHandle, &LoadedImageProtocol, (void **)&li);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efilibc: efilibc_init(): error: HandleProtocol(LoadedImageProtocol) failed: %i\n", s);
		return -1;
	}

	s = ST->BootServices->HandleProtocol(li->DeviceHandle, &FileSystemProtocol, (void **)&sfs);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efilibc: efilibc_init(): error: HandleProtocol(FileSystemProtocol) failed: %i\n", s);
		return -1;
	}

	/* Obtain root directory */
	EFI_FILE *root;
	s = sfs->OpenVolume(sfs, &root);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efilibc: efilibc_init(): error: OpenVolume() failed: %i\n", s);
		return -1;
	}

	/* store it as fopen_root */
	fopen_root = root;

	/* register default input/output streams */
	efilibc_register_stdout_fwrite(efilibc_console_fwrite, NULL);
	efilibc_register_stdin_fread(efilibc_console_fread, NULL);

	if(efilibc_setup_serial(0) == 0)
	{
		efilibc_register_stderr_fwrite(efilibc_serial_fwrite, NULL);
		fprintf(stderr, "stderr:\n");
	}

	return 0;
}
