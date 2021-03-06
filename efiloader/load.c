#include <efi.h>
#include <efilib.h>
#include <zlib.h>
#include <stdint.h>

extern EFI_PHYSICAL_ADDRESS elf_kernel;
extern int elf_kernel_len;

EFI_STATUS load_file(FILE *f, long flen, EFI_PHYSICAL_ADDRESS paddr)
{
	long cur_offset = 0;
	while(cur_offset < flen)
	{
		long to_read = 0x100000;
		if(cur_offset + to_read > flen)
			to_read = flen - cur_offset;
		fread((void*)(intptr_t)(paddr + cur_offset), to_read, 1, f);
		printf(".");
		cur_offset += 0x100000;
	}

	return EFI_SUCCESS;
}

static size_t libc_fread(void *fobj, void *buf, size_t len)
{
	return fread(buf, 1, len, (FILE *)fobj);
}

EFI_STATUS load_kernel(const char *fname, void **fobj, size_t (**fread_func)(void *, void *, size_t),
					   int (**fclose_func)(void *), off_t (**fseek_func)(void *, off_t offset, int whence))
{
	/* Load up the kernel */
	FILE *fkernel = fopen(fname, "r");
	if(fkernel == NULL)
	{
		printf("Error: kernel not found\n");
		return EFI_NOT_FOUND;
	}
	fseek(fkernel, 0, SEEK_END);
	long flen = ftell(fkernel);
	elf_kernel_len = (int)flen;
	fseek(fkernel, 0, SEEK_SET);
#ifdef _DEBUG
	printf("Found kernel of length %i\n", flen);
#endif

	/* Load up the first 4 bytes to determine kernel type */
	uint8_t magic[4];
	fread(magic, 1, 4, fkernel);
	fseek(fkernel, 0, SEEK_SET);

	if(magic[0] == 0x7f && magic[1] == 'E' && magic[2] == 'L' && magic[3] == 'F')
	{
		/* Its an uncompressed ELF kernel */
#ifdef _DEBUG
		printf("Uncompressed ELF kernel detected\n");
#endif
		if(fobj)
			*fobj = (void *)fkernel;
		if(fread_func)
			*fread_func = libc_fread;
		if(fclose_func)
			*fclose_func = fclose;
		if(fseek_func)
			*fseek_func = (off_t (*)(void *, off_t offset, int whence))fseek;
	}
	else if(magic[0] == 0x1f && magic[1] == 0x8b)
	{
		/* Its a gzip file */
#ifdef _DEBUG
		printf("GZip kernel detected\n");
#endif
		gzFile gzkernel = gzdopen(fileno(fkernel), "r");
		if(fobj)
			*fobj = (void *)gzkernel;
		if(fread_func)
			*fread_func = (size_t (*)(void *, void *, size_t))gzread;
		if(fclose_func)
			*fclose_func = (int (*)(void *))gzclose_r;
		if(fseek_func)
			*fseek_func = (off_t (*)(void *, off_t offset, int whence))gzseek;
	}
	else
	{
		printf("Unknown kernel type: length: %i, magic[0]: %x, magic[1]: %x, "
			"magic[2]: %x, magic[3]: %x\n", flen, magic[0], magic[1], magic[2],
			magic[3]);
		return EFI_ABORTED;
	}

	return EFI_SUCCESS;
}