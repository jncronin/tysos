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

#define IN_FILE_C
typedef struct _FILE
{
	EFI_FILE *f;
	int eof;
	int error;
	int fileno;
	int istty;
	int ttyno;
} FILE;

typedef struct _DIR
{
	EFI_FILE *f;
} DIR;

#include <dirent.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <efilibc.h>

/* fileno -> FILE * mappings */
#define MAX_FILENO		1024
static FILE* fileno_map[MAX_FILENO] = { stdin, stdout, stderr };
static int next_fileno = 3;

static EFI_FILE_INFO *get_file_info(EFI_FILE *f);

size_t console_fread(void *ptr, size_t size, size_t nmemb, void *data);
size_t console_fwrite(const void *ptr, size_t size, size_t nmemb, void *data);

EFI_FILE *fopen_root = NULL;

static fwrite_func stdout_fwrite = NULL;
static fwrite_func stderr_fwrite = NULL;
static fread_func stdin_fread = NULL;
static void *stdout_data = NULL;
static void *stderr_data = NULL;
static void *stdin_data = NULL;

int efilibc_register_stdout_fwrite(fwrite_func new_stdout_fwrite, void *data)
{
	stdout_fwrite = new_stdout_fwrite;
	stdout_data = data;
	return 0;
}

int efilibc_register_stderr_fwrite(fwrite_func new_stderr_fwrite, void *data)
{
	stderr_fwrite = new_stderr_fwrite;
	stderr_data = data;
	return 0;
}

int efilibc_register_stdin_fread(fread_func new_stdin_fread, void *data)
{
	stdin_fread = new_stdin_fread;
	stdin_data = data;
	return 0;
}

void conv_backslashes(CHAR16 *s)
{
	while (*s != 0)
	{
		if(*s == '/')
			*s = '\\';
		s++;
	}
}

EFI_FILE_INFO *get_file_info(EFI_FILE *f)
{
	UINTN buf_size;

	EFI_STATUS s = f->GetInfo(f, &GenericFileInfo, &buf_size, NULL);
	assert(s == EFI_BUFFER_TOO_SMALL);
	EFI_FILE_INFO *ret = (EFI_FILE_INFO *)malloc(buf_size);
	assert(ret);
	s = f->GetInfo(f, &GenericFileInfo, &buf_size, ret);
	assert(!EFI_ERROR(s));
	return ret;
}

DIR *opendir(const char *name)
{
	if(name == NULL)
	{
		errno = EINVAL;
		return NULL;
	}

	/* Attempt to open the directory */
	EFI_FILE *f;
	CHAR16 *wfname = (CHAR16 *)malloc((strlen(name) + 1) * sizeof(CHAR16));
	if(wfname == NULL)
	{
		errno = ENOMEM;
		return NULL;
	}
	mbstowcs((wchar_t *)wfname, name, strlen(name) + 1);

	/* Convert backslashes to forward slashes */
	conv_backslashes(wfname);

	EFI_STATUS s = fopen_root->Open(fopen_root, &f, wfname, EFI_FILE_MODE_READ, 0);
	free(wfname);
	if(s == EFI_NOT_FOUND)
	{
		fprintf(stderr, "efilibc: opendir(%s): EFI_NOT_FOUND\n", name);
		errno = ENOENT;
		return NULL;
	}
	else if(EFI_ERROR(s))
	{
		fprintf(stderr, "efilibc: fopen(): error %i\n", s);
		errno = EFAULT;
		return NULL;
	}

	/* Ensure its a directory */
	EFI_FILE_INFO *dinfo = get_file_info(f);
	if(!(dinfo->Attribute & EFI_FILE_DIRECTORY))
	{
		free(dinfo);
		f->Close(f);
		errno = ENOTDIR;
		return NULL;
	}

	free(dinfo);
	struct _DIR *ret = (struct _DIR *)malloc(sizeof(struct _DIR));
	ret->f = f;
	return ret;
}

FILE *fopen(const char *path, const char *mode)
{
	if(fopen_root == NULL)
	{
		errno = EFAULT;
		fprintf(stderr, "efilibc: fopen(): error: please call efilibc_init() before fopen()\n");
		return NULL;
	}

	if(next_fileno >= MAX_FILENO)
	{
		errno = EMFILE;
		return NULL;
	}

	/* Interpret mode (simplistic) */
	if(mode == NULL)
	{
		errno = EINVAL;
		return NULL;
	}

	UINT64 openmode;
	if(mode[0] == 'r')
	{
		if(mode[1] == '+')
			openmode = EFI_FILE_MODE_READ | EFI_FILE_MODE_WRITE;
		else
			openmode = EFI_FILE_MODE_READ;
	} else
		openmode = EFI_FILE_MODE_READ | EFI_FILE_MODE_WRITE;

	/* Attempt to open the file */
	EFI_FILE *f;
	CHAR16 *wfname = (CHAR16 *)malloc((strlen(path) + 1) * sizeof(CHAR16));
	if(wfname == NULL)
	{
		errno = ENOMEM;
		return NULL;
	}
	mbstowcs((wchar_t *)wfname, path, strlen(path) + 1);

	/* Convert backslashes to forward slashes */
	conv_backslashes(wfname);

	EFI_STATUS s = fopen_root->Open(fopen_root, &f, wfname, openmode, 0);
	free(wfname);
	if(s == EFI_NOT_FOUND)
	{
		fprintf(stderr, "efilibc: fopen(%s): EFI_NOT_FOUND\n", path);
		errno = ENOENT;
		return NULL;
	}
	else if(EFI_ERROR(s))
	{
		fprintf(stderr, "efilibc: fopen(): error %i\n", s);
		errno = EFAULT;
		return NULL;
	}
	FILE *ret = (FILE *)malloc(sizeof(FILE));
	assert(ret);
	memset(ret, 0, sizeof(FILE));
	ret->f = f;
	ret->fileno = next_fileno++;

	fileno_map[ret->fileno] = ret;
	return ret;
}

size_t fread(void *ptr, size_t size, size_t nmemb, FILE *stream)
{
	if(stream == stdin)
		return stdin_fread(ptr, size, nmemb, stdin_data);
	else if((stream == stdout) || (stream == stderr))
		return 0;

	if((stream == NULL) || (ptr == NULL))
	{
		errno = EINVAL;
		return 0;
	}

	UINTN buf_size = size * nmemb;
	UINTN old_buf_size = buf_size;
	EFI_STATUS s = stream->f->Read(stream->f, &buf_size, ptr);
	if(EFI_ERROR(s))
		return 0;

	if(buf_size != old_buf_size)
		stream->eof = 1;

	return buf_size / size;
}

size_t fwrite(void *ptr, size_t size, size_t nmemb, FILE *stream)
{
	if(stream == stdin)
		return 0;
	else if(stream == stdout)
		return stdout_fwrite(ptr, size, nmemb, stdout_data);
	else if(stream == stderr)
		return stderr_fwrite(ptr, size, nmemb, stderr_data);

	if((stream == NULL) || (ptr == NULL))
	{
		errno = EINVAL;
		return 0;
	}

	UINTN buf_size = size * nmemb;
	UINTN old_buf_size = buf_size;
	EFI_STATUS s = stream->f->Write(stream->f, &buf_size, ptr);
	if(EFI_ERROR(s))
		return 0;

	if(buf_size != old_buf_size)
		stream->eof = 1;

	return buf_size / size;
}

int fgetc(FILE *stream)
{
	char ret;
	if(fread(&ret, 1, 1, stream) != 1)
		return EOF;
	return (int)ret;
}

int fputc(int c, FILE *stream)
{
	if(fwrite(&c, 1, 1, stream) != 1)
		return EOF;
	return c;
}

int fclose(FILE *stream)
{
	if((stream == NULL) || (stream == stdin) || (stream == stdout) || (stream == stderr))
	{
		errno = EBADF;
		return EOF;
	}

	stream->f->Close(stream->f);
	fileno_map[stream->fileno] = NULL;
	free(stream);

	return 0;
}

void clearerr(FILE *stream)
{
	if((stream == NULL) || (stream == stdin) || (stream == stdout) || (stream == stderr))
		return;
	stream->error = 0;
	stream->eof = 0;
}

int feof(FILE *stream)
{
	if((stream == NULL) || (stream == stdin))
		return 0;
	if((stream == stdout) || (stream == stderr))
		return 1;
	return stream->eof;
}

int ferror(FILE *stream)
{
	if(stream == NULL)
		return 1;
	if((stream == stdin) || (stream == stdout) || (stream == stderr))
		return 0;
	return stream->error;
}

int fileno(FILE *stream)
{
	return stream->fileno;
}

int isatty(int fd)
{
	if((fd < 0) || (fd > MAX_FILENO))
	{
		errno = EBADF;
		return 0;
	}

	if((fd == 0) || (fd == 1) || (fd == 2))
		return 1;

	if(fileno_map[fd] == NULL)
	{
		errno = EBADF;
		return 0;
	}

	int ret = fileno_map[fd]->istty;
	if(ret)
		return ret;
	else
	{
		errno = EINVAL;
		return ret;
	}
}

int fseek(FILE *stream, long pos, int whence)
{
	if((stream == stdin) || (stream == stdout) || (stream == stderr) || (stream == NULL))
	{
		errno = EBADF;
		return -1;
	}

	UINT64 new_pos;
	EFI_STATUS s;
	switch(whence)
	{
		case SEEK_SET:
			new_pos = (UINT64)pos;
			break;
		case SEEK_CUR:
			{
				UINT64 cur_pos;
				s = stream->f->GetPosition(stream->f, &cur_pos);
				if(EFI_ERROR(s))
				{
					errno = EFAULT;
					return -1;
				}
				if(pos < 0)
					new_pos = cur_pos - (UINT64)(-pos);
				else
					new_pos = cur_pos + (UINT64)pos;
			}
			break;
		case SEEK_END:
			new_pos = fsize(stream) + (UINT64)pos;
			break;
		default:
			errno = EINVAL;
			return -1;
	}

	s = stream->f->SetPosition(stream->f, new_pos);
	if(EFI_ERROR(s))
	{
		errno = EFAULT;
		return -1;
	}
	stream->eof = 0;
	return 0;
}

void rewind(FILE *stream)
{
	if(fseek(stream, 0, SEEK_SET) == 0)
		stream->error = 0;
}

long ftell(FILE *stream)
{
	if((stream == stdin) || (stream == stdout) || (stream == stderr) || (stream == NULL))
	{
		errno = EBADF;
		return -1;
	}

	UINT64 pos;
	EFI_STATUS s = stream->f->GetPosition(stream->f, &pos);
	if(EFI_ERROR(s))
	{
		errno = EFAULT;
		return -1;
	}
	return (long)pos;
}

int fgetpos(FILE *stream, fpos_t *pos)
{
	long p = ftell(stream);
	if(p == -1)
		return -1;
	
	*pos = p;
	return 0;
}

int fsetpos(FILE *stream, fpos_t *pos)
{
	return fseek(stream, *pos, SEEK_SET);
}

int fflush(FILE *stream)
{
	if((stream == stdin) || (stream == NULL))
	{
		errno = EBADF;
		return EOF;
	}

	if((stream == stderr) || (stream == stdout))
		return 0;

	EFI_STATUS s = stream->f->Flush(stream->f);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efilibc: fflush failed: %i\n", s);
		errno = EFAULT;
		return EOF;
	}

	return 0;
}

int remove(const char *pathname)
{
	if(pathname == NULL)
	{
		errno = EINVAL;
		return -1;
	}

	FILE *f = fopen(pathname, "r");
	if(f == NULL)
		return -1;

	EFI_STATUS s = f->f->Delete(f->f);

	fileno_map[f->fileno] = NULL;
	free(f);

	if(s == EFI_WARN_DELETE_FAILURE)
	{
		fprintf(stderr, "efilibc: remove: could not remove %s\n", pathname);
		errno = EFAULT;
		return -1;
	}

	return 0;
}

#ifndef POSIXLY_CORRECT
long fsize(FILE *stream)
{
	UINTN buf_size = 0;
	EFI_STATUS s = stream->f->GetInfo(stream->f, &GenericFileInfo, &buf_size, NULL);
	if(s != EFI_BUFFER_TOO_SMALL)
	{
		errno = EFAULT;
		return -1;
	}
	EFI_FILE_INFO *fi = (EFI_FILE_INFO *)malloc(buf_size);
	if(fi == NULL)
	{
		errno = ENOMEM;
		return -1;
	}
	s = stream->f->GetInfo(stream->f, &GenericFileInfo, &buf_size, fi);
	if(EFI_ERROR(s))
	{
		free(fi);
		errno = EFAULT;
		return -1;
	}
	long len = (long)fi->FileSize;
	free(fi);
	return len;
}
#endif