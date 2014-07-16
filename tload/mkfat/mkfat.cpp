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

/* A simple utility for creating a FAT image from a directory */

#include <vector>
#include <iostream>
#include <fstream>
#include <boost/filesystem.hpp>
#include "file.h"
#include <stdio.h>
#include <ff.h>
#include <stdint.h>
#include "mkfat.h"


using namespace boost::filesystem;
using namespace std;

int mkfsfat(uint8_t *buf, size_t buf_size);
void usage(TCHAR *epath);

extern uint8_t *buf;
extern int sector_count;
int fat32 = 0;

int _tmain(int argc, TCHAR *argv[])
{
	path *src_dir, *dest_loc = NULL;

	if(argc < 2)
	{
		usage(argv[0]);
		return -1;
	}
	for(int i = 1; i < argc; i++)
	{
		if(!_tstrcmp(argv[i], OPT_O))
		{
			if(i == (argc - 1))
			{
				usage(argv[0]);
				return -1;
			}
			i++;
			dest_loc = new path(argv[i]);
		}
		else if(!_tstrcmp(argv[i], OPT_F32))
			fat32 = 1;
		else if(i == (argc - 1))
		{
			if(argv[i][0] == '-')
			{
				usage(argv[0]);
				return -1;
			}
			src_dir = new path(argv[i]);
		}
		else
		{
			usage(argv[0]);
			return -1;
		}
	}

	if(!exists(*src_dir))
	{
		cout << "directory " << *src_dir << " does not exist" << endl;
		return -1;
	}
	if(!is_directory(*src_dir))
	{
		cout << *src_dir << " is not a directory" << endl;
		return -2;
	}

	/* Build a tree of the directory */
	vector<base_entry *> *big_list = new vector<base_entry *>();
	dir_entry *root = new dir_entry(*src_dir, big_list);

	/* Calculate the total size of the data in sectors */
	int sec_count = 0;
	int sec_size = 512;
	for(int i = 0; i < big_list->size(); i++)
		sec_count += (*big_list)[i]->cluster_count(sec_size);
	if(sec_count < 128)
		sec_count = 128;	// f_mkfs fails otherwise

	int success = 0;

	while(!success)
	{
		int fat_type = 0;
		int au = 128;

		/* 1.5x the size of the image */
		sec_count *= 3;
		sec_count /= 2;

		/* Round image size up to a multiple of 4096 */
		if(sec_count % (4096 / sec_size))
		{
			sec_count -= (sec_count % (4096 / sec_size));
			sec_count += (4096 / sec_size);
		}
		int byte_size = sec_count * sec_size;
		buf = (uint8_t *)malloc(byte_size);
		sector_count = sec_count;

		/* Make a filesystem on it */
		FATFS fatfs;
		FRESULT res;

		if(fat32)
		{
			while((fat_type != FS_FAT32) && (au > 1))
			{
				res = f_mount(&fatfs, DRV_0, 0);
				if(res != FR_OK)
				{
					cout << "f_mount failed: " << res << endl;
					return -1;
				}

				res = f_mkfs(DRV_0, 1, au, &fat_type);
				if((res != FR_OK) && (res != FR_MKFS_ABORTED))
				{
					cout << "f_mkfs failed: " << res << endl;
					return -1;
				}

				if(res == FR_MKFS_ABORTED)
					fat_type = 0;

				au /= 2;
			}
			if(fat_type != FS_FAT32)
			{
				success = false;
				free(buf);
				continue;
			}
		}
		else
		{
			res = f_mount(&fatfs, DRV_0, 0);
			if(res != FR_OK)
			{
				cout << "f_mount failed: " << res << endl;
				return -1;
			}

			res = f_mkfs(DRV_0, 1, au, NULL);
			if((res != FR_OK) && (res != FR_MKFS_ABORTED))
			{
				cout << "f_mkfs failed: " << res << endl;
				return -1;
			}

			if(res == FR_MKFS_ABORTED)
			{
				success = false;
				free(buf);
				continue;
			}
		}

		/* Copy the directory to it */
		res = root->write(&fatfs, *src_dir);
		f_mount(NULL, DRV_0, 1);

		/* Write it out */
		if(dest_loc != NULL)
		{
			ofstream fd;
			fd.open(dest_loc->tstring().c_str(), ios_base::out | ios::binary);
			fd.write((char *)buf, byte_size);
			fd.close();
		}
		else
			cout.write((char *)buf, byte_size);

		free(buf);
		buf = NULL;

		if(res == FR_OK)
			success = 1;
		else if(res == FR_DISK_ERR)
			success = 0;	// try again with a bigger image
		else
		{
			cout << "Error creating image(" << res << ")" << endl;
			return -1;
		}
	}

	return 0;
}

void usage(TCHAR *epath)
{
	cout << "Usage: " << epath << " [-o output_file] [-F32] input_directory" << endl << endl;
}
