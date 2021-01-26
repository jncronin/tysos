/* Stub functions to allow confuse/zlib et al to use grub functions */
#include <stdarg.h>
#include <stddef.h>
#include <stdlib.h>
#include <stdio.h>
#include <sys/types.h>

#include <grub/mm.h>
#include <grub/misc.h>
#include <grub/file.h>

int errno;

struct glc_file
{
	grub_file_t fp;
	int pos;
	int size;
	int eof;
	int error;
	int fileno;
	int offset_offset;
};

#define MAX_FILENO		1024
static FILE* fileno_map[MAX_FILENO] = { stdin, stdout, stderr };
static int next_fileno = 3;

static void update_fpos(struct glc_file *fp)
{
	fp->pos = *(int *)(((uintptr_t)fp->fp) + fp->offset_offset);
}

static void update_feof(struct glc_file *fp)
{
	update_fpos(fp);
	if (fp->pos >= fp->size)
		fp->eof = 1;
	else
		fp->eof = 0;
}

int printf(const char *fmt, ...)
{
	va_list argp;
	va_start(argp, fmt);
	int ret = grub_vprintf(fmt, argp);
	va_end(argp);
	return ret;
}

void *malloc(size_t size)
{
	void* ret = grub_malloc(size);
#ifdef DEBUG
	grub_printf("malloc: %p\n", ret);
#endif
	return ret;
}

void free(void *ptr)
{
#ifdef DEBUG
	grub_printf("free: %p\n", ptr);
#endif
	return grub_free(ptr);
}

void *calloc(size_t nmemb, size_t size)
{
	void* ret = grub_malloc(nmemb * size);
	grub_memset(ret, 0, nmemb * size);
#ifdef DEBUG
	grub_printf("calloc: %p\n", ret);
#endif
	return ret;
}

void *realloc(void *ptr, size_t size)
{
	void *ret = grub_realloc(ptr, size);
#ifdef DEBUG
	grub_printf("realloc: %p\n", ret);
#endif
	return ret;
}

int isatty(int fd)
{
	if (fd == (int)stdin || fd == (int)stdout || fd == (int)stderr)
		return 1;
	return 0;
}

long int strtol(const char *nptr, char **endptr, int base)
{
	return grub_strtol(nptr, endptr, base);
}

int fprintf(FILE *stream, const char *fmt, ...)
{
	char *str = (char *)grub_malloc(512);
	int ret = 0;

	va_list argp;
	va_start(argp, fmt);
	grub_vsnprintf(str, 512, fmt, argp);
	va_end(argp);

	if (stream == stdout || stream == stderr)
	{
		ret = grub_printf(str);
	}

	grub_free(str);
	return ret;
}

int vfprintf(FILE *stream, const char *fmt, va_list argp)
{
	char *str = (char *)grub_malloc(512);
	int ret = 0;

	grub_vsnprintf(str, 512, fmt, argp);

	if (stream == stdout || stream == stderr)
	{
		ret = grub_printf(str);
	}

	grub_free(str);
	return ret;
}

int ferror(FILE *stream)
{
	if (stream != stdin && stream != stdout && stream != stderr && stream)
	{
		struct glc_file *fp = (struct glc_file *)fp;
		update_feof(fp);
		return fp->error;
	}
	return 0;
}

int feof(FILE *stream)
{
	if (stream != stdin && stream != stdout && stream != stderr && stream)
	{
		struct glc_file *fp = (struct glc_file *)fp;
		update_feof(fp);
		return fp->eof;
	}
	return 0;
}

size_t fread(void *ptr, size_t size, size_t nmemb, FILE *stream)
{
	if (!stream || stream == stdin || stream == stdout || stream == stderr)
	{
		return -1;
	}

	if (size == 0)
		return 0;

	struct glc_file *fp = (struct glc_file *)stream;
	grub_ssize_t read = grub_file_read(fp->fp, ptr, size * nmemb);
	update_fpos(fp);
	return (size_t)read / size;
}

ssize_t read(int fd, void *buf, size_t count)
{
	if (fd < 0 || fd >= next_fileno)
		return -1;
	return (ssize_t)fread(buf, 1, count, fileno_map[fd]);
}

long ftell(FILE *stream)
{
	if (!stream || stream == stdin || stream == stdout || stream == stderr)
	{
		return -1;
	}

	struct glc_file *fp = (struct glc_file *)stream;
	update_fpos(fp);
	return (long)fp->pos;
}

void rewind(FILE *stream)
{
	if (!stream || stream == stdin || stream == stdout || stream == stderr)
	{
		return;
	}

	fseek(stream, 0, SEEK_SET);
	clearerr(stream);
}

FILE *fopen(const char *path, const char *mode)
{
	(void)mode;
	if (next_fileno >= MAX_FILENO)
	{
		return NULL;
	}

#ifdef HAS_ENUM_GRUB_FILE_TYPE
	grub_file_t ret = grub_file_open(path, GRUB_FILE_TYPE_MULTIBOOT_MODULE);
#else
	grub_file_t ret = grub_file_open(path);
#endif
	if (ret)
	{
#ifdef DEBUG
		grub_printf("fopen: %s\n", path);
#endif
		struct glc_file *fp = (struct glc_file *)grub_malloc(sizeof(struct glc_file));
		fp->fp = ret;
		fp->pos = 0;
		fp->eof = 0;
		fp->fileno = next_fileno++;

		int32_t *r = (int32_t *)ret;

		/* This is a total hack.  Because the alignment of the host grub may be 4 or 8 bytes
		    for int64_t's, the size member is either at offset 0x34 or 0x38.
			
			Thankfully, the previous member (estimated_speed) is not filled in upon opening,
			so we can inspect r[0x34] and use it if non-zero, else use r[0x38]*/
		if (r[0x34 / 4])
		{
			fp->size = (int)r[0x34 / 4];
			fp->offset_offset = 0xc;
		}
		else
		{
			fp->size = (int)r[0x38 / 4];
			fp->offset_offset = 0x10;
		}

		fileno_map[fp->fileno] = fp;
		return (FILE*)fp;
	}
	return NULL;
}

int open(const char *path, int flags)
{
	(void)flags;
	FILE *f = fopen(path, "");
	if (f == NULL)
		return -1;
	struct glc_file *fp = (struct glc_file *)fp;
	return fp->fileno;
}

int fseek(FILE *stream, long offset, int whence)
{
	if (!stream || stream == stdin || stream == stdout || stream == stderr)
	{
		return -1;
	}

	struct glc_file *fp = (struct glc_file *)stream;

	switch (whence)
	{
	case SEEK_SET:
		fp->pos = (int)offset;
		break;
	case SEEK_CUR:
		fp->pos = fp->pos + (int)offset;
		break;
	case SEEK_END:
		fp->pos = fp->size - (int)offset;
		break;
	default:
		return -1;
	}

	if (grub_file_seek(fp->fp, fp->pos) == (grub_off_t)-1)
	{
		printf("grub_file_seek failed\n");
		update_feof(fp);
		return -1;
	}
	update_feof(fp);
	return 0;
}

off_t lseek(int fd, off_t offset, int whence)
{
	if (fd < 0 || fd >= next_fileno)
		return (off_t)-1;
	return (off_t)fseek(fileno_map[fd], (long)offset, whence);
}


int fclose(FILE *stream)
{
	if (stream != stdin && stream != stdout && stream != stderr && stream)
	{
#ifdef DEBUG
		grub_printf("fclose: %p\n", stream);
#endif
		struct glc_file *fp = (struct glc_file *)stream;
		grub_file_close(fp->fp);
		grub_free(fp);
	}
	return 0;
}

int close(int fd)
{
	if (fd < 0 || fd >= next_fileno)
		return -1;
	return fclose(fileno_map[fd]);
}

void clearerr(FILE *stream)
{
	if (stream != stdin && stream != stdout && stream != stderr && stream)
	{
		struct glc_file *fp = (struct glc_file *)fp;
		fp->eof = 0;
		fp->error = 0;
	}
}

int fgetc(FILE *stream)
{
	if (!stream)
		return EOF;

	struct glc_file *fp = (struct glc_file *)stream;
	if (fp->pos >= fp->size)
	{
		fp->eof = 1;
		return EOF;
	}

	int ret = 0;
	grub_file_read(fp->fp, &ret, 1);
	fp->pos++;

	return ret;
}

int sprintf(char *str, const char *fmt, ...)
{
	va_list argp;
	va_start(argp, fmt);
	int ret = grub_vsnprintf(str, 1024, fmt, argp);
	va_end(argp);
	return ret;
}

int snprintf(char *str, size_t size, const char *fmt, ...)
{
	va_list argp;
	va_start(argp, fmt);
	int ret = grub_vsnprintf(str, size, fmt, argp);
	va_end(argp);
	return ret;
}

size_t fwrite(const void *ptr, size_t size, size_t nmemb, FILE *stream)
{
	if (stream == stdout || stream == stderr)
	{
		size_t sz = size * nmemb;

#ifdef DEBUG
		grub_printf("fwrite\n");
#endif

		/* Build a null-terminated version of ptr */
		char *str = (char *)grub_malloc(sz + 1);
		grub_memset(str, 0, sz + 1);
		grub_memcpy(str, ptr, sz);
		grub_puts(str);

		grub_free(str);

		return nmemb;
	}
	else
	{
		return 0;
	}
}

void exit(int status)
{
	(void)status;
	grub_exit();
}

int fileno(FILE *stream)
{
	if (stream == stdout || stream == stdin || stream == stderr)
		return (int)stream;
	struct glc_file *fp = (struct glc_file *)stream;
	return fp->fileno;
}
