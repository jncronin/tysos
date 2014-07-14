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

#include "guid.h"
#include <stddef.h>

GUID fstypes[512];
char *fsnames[512];

#include "fstypes.h"

void init_fstypes()
{
	int i;

	for(i = 0; i < 512; i++)
	{
		string_to_guid(&fstypes[i], "00000000-0000-0000-0000-000000000000");
		fsnames[i] = NULL;
	}

	string_to_guid(&fstypes[FSTYPE_EFI_SYSTEM], "C12A7328-F81F-11D2-BA4B-00A0C93EC93B");
	fsnames[FSTYPE_EFI_SYSTEM] = "system";
	string_to_guid(&fstypes[FSTYPE_BIOS_BOOT], "21686148-6449-6E6F-744E-656564454649");
	fsnames[FSTYPE_BIOS_BOOT] = "bios";
	string_to_guid(&fstypes[0x01], "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7");
	fsnames[0x01] = "fat12";
	string_to_guid(&fstypes[0x04], "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7");
	fsnames[0x04] = "fat16";
	string_to_guid(&fstypes[0x06], "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7");
	fsnames[0x06] = "fat16b";
	string_to_guid(&fstypes[0x07], "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7");
	fsnames[0x07] = "ntfs";
	string_to_guid(&fstypes[0x0b], "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7");
	fsnames[0x0b] = "fat32";
	string_to_guid(&fstypes[0x0c], "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7");
	fsnames[0x0c] = "fat32x";
	string_to_guid(&fstypes[0x0e], "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7");
	fsnames[0x0e] = "fat16x";
	string_to_guid(&fstypes[0x28], "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7");
	fsnames[0x28] = "fat16+";
	string_to_guid(&fstypes[0x29], "EBD0A0A2-B9E5-4433-87C0-68B6B72699C7");
	fsnames[0x29] = "fat32+";
	string_to_guid(&fstypes[0x83], "0FC63DAF-8483-4772-8E79-3D69D8477DE4");
	fsnames[0x83] = "linux";
}
