#include <efi.h>
#include <efilib.h>
#include <zlib.h>

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
		fread((void*)(paddr + cur_offset), to_read, 1, f);
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
					   int (**fclose_func)(void *))
{
	EFI_STATUS Status;

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

	int type = -1;
	if(magic[0] == 0x7f && magic[1] == 'E' && magic[2] == 'L' && magic[3] == 'F')
	{
		/* Its an uncompressed ELF kernel */
#ifdef _DEBUG
		printf("Uncompressed ELF kernel detected\n");
#endif
		type = 0;
	}
	else if(magic[0] == 0x1f && magic[1] == 0x8b)
	{
		/* Its a gzip file */
#ifdef _DEBUG
		printf("GZip kernel detected\n");
#endif
		type = 1;
	}
	else
	{
		printf("Unknown kernel type\n");
		return EFI_ABORTED;
	}

	if(type == 0)
	{
		long flen_align = flen;
		if(flen & 0xfff)
			flen_align = (flen + 0x1000) & ~0xfff;
		EFI_PHYSICAL_ADDRESS kernel_paddr;
		Status = BS->AllocatePages(AllocateAnyPages, EfiLoaderCode, flen_align / 4096, &kernel_paddr);
		if(Status != EFI_SUCCESS)
		{
			printf("Error allocating kernel space: %i\n", Status);
			return Status;
		}
		printf("Loading kernel");
		load_file(fkernel, flen, kernel_paddr);
		printf(" done.\n");
		elf_kernel = kernel_paddr;
	}
	else if(type == 1)
	{
		/* Read the uncompressed size */
		fseek(fkernel, flen - 4, SEEK_SET);
		uint32_t decomp_size;
		fread(&decomp_size, 4, 1, fkernel);
		fseek(fkernel, 0, SEEK_SET);

#ifdef _DEBUG
		printf("Decompressed kernel size is %i\n", decomp_size);
#endif

		uint32_t decomp_size_align = decomp_size;
		if(decomp_size & 0xfff)
			decomp_size_align = (decomp_size + 0x1000) & ~0xfff;
		EFI_PHYSICAL_ADDRESS decomp_addr;
		Status = BS->AllocatePages(AllocateAnyPages, EfiLoaderCode, decomp_size_align / 4096, &decomp_addr);
		if(Status != EFI_SUCCESS)
		{
#ifdef _DEBUG
			printf("Failed to allocate memory for decompressed kernel: %i\n", Status);
#endif
			return Status;
		}

		gzFile gzkernel = gzdopen(fileno(fkernel), "r");
		if(gzkernel == NULL)
		{
			printf("gzdopen returned null\n");
			return EFI_ABORTED;
		}
		gzbuffer(gzkernel, 128 * 1024);

		printf("Loading compressed kernel...");
		int read = gzread(gzkernel, (voidp)decomp_addr, decomp_size);

#ifdef _DEBUG
		printf("uncompressed %i bytes\n", read);
#endif

		if((uint32_t)read != decomp_size)
		{
			printf("Error uncompressing kernel\n");
			return EFI_ABORTED;
		}

		uint8_t *buf = (uint8_t *)decomp_addr;
		if(buf[0] == 0x7f && buf[1] == 'E' && buf[2] == 'L' && buf[3] == 'F')
			printf(" done\n");
		else
		{
			printf("Unknown kernel type\n");
			return EFI_ABORTED;
		}
		elf_kernel = decomp_addr;
		elf_kernel_len = decomp_size;
		gzclose(gzkernel);
	}

	return EFI_SUCCESS;
}