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

#include "file.h"
#include <vector>
#include <boost/filesystem.hpp>
#include <ff.h>
#include <stdio.h>
#include <fstream>
#include "mkfat.h"

using namespace std;
using namespace boost::filesystem;

dir_entry::dir_entry(path p, vector<base_entry *> *big_list)
{
	vector<path> dir_entries;
	parent = NULL;
	name = p;

	directory_iterator dir_iter = directory_iterator(p);
	while(dir_iter != directory_iterator())
	{
		dir_entries.push_back(*dir_iter);
		dir_iter++;
	}

	for(unsigned int i = 0; i < dir_entries.size(); i++)
	{
		path cur_p = dir_entries[i];

		if(is_regular_file(cur_p))
		{
			file_entry *fe = new file_entry();
			fe->name = cur_p;
			fe->parent = this;
			fe->byte_size = file_size(cur_p);
			files.push_back(fe);
			if(big_list != NULL)
				big_list->push_back(fe);
		}
		else if(is_directory(cur_p))
		{
			dir_entry *de = new dir_entry(cur_p, big_list);
			de->name = cur_p;
			de->parent = this;
			subdirs.push_back(de);
		}
	}

	if(big_list != NULL)
		big_list->push_back(this);
}

int dir_entry::size()
{
	// directory entries are 32 bytes in length
	return (files.size() + subdirs.size()) * 32;
}

int file_entry::size()
{
	return byte_size;
}

int base_entry::cluster_count(int cluster_size)
{
	int ret = size() / cluster_size;
	if(size() % cluster_size)
		ret++;
	return ret;
}

FRESULT dir_entry::write(FATFS *fs, boost::filesystem::path root_path)
{
	std::tstring n_root_path = root_path.tstring();
	std::tstring my_root_path = this->name.tstring();

	for(int i = 0; i < files.size(); i++)
	{
		path p = files[i]->name;
		
		/* trim the root portion of the name */
		std::tstring n_p = p.tstring();
		std::tstring fname = n_p.substr(n_root_path.length());

		FIL dest;
		FRESULT res = f_open(&dest, fname.c_str(), FA_CREATE_ALWAYS | FA_WRITE);
		if(res != FR_OK)
		{
			cout << "f_open(" << fname << ") returned " << res << endl;
			return res;
		}
		
		/* Read the source file */
		void *buf = malloc(files[i]->byte_size);
		ifstream fd;
		fd.open(files[i]->name.tstring().c_str(), ios::binary | ios::in);
		fd.read((char *)buf, files[i]->byte_size);
		fd.close();

		/* Write out to the FAT filesystem */
		UINT written;
		res = f_write(&dest, buf, files[i]->byte_size, &written);
		if(res != FR_OK)
		{
			cout << "f_write failed: " << res << endl;
			return res;
		}
		if(written != files[i]->byte_size)
		{
			cout << "f_write - disk full" << res << endl;
			return FR_DISK_ERR;
		}
		f_close(&dest);

		free(buf);
	}

	for(int i = 0; i < subdirs.size(); i++)
	{
		path p = subdirs[i]->name;

		std::tstring n_p = p.tstring();
		std::tstring dname = n_p.substr(n_root_path.length());

		FRESULT res = f_mkdir(dname.c_str());
		if(res != FR_OK)
		{
			cout << "f_mkdir(" << dname << ") returned " << res << endl;
			return res;
		}

		res = subdirs[i]->write(fs, root_path);
		if(res != FR_OK)
			return res;
	}

	return FR_OK;
}
