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
#include <confuse.h>
#include <string.h>
#include <errno.h>
#include <efilibc.h>
#include <stdlib.h>
#include <assert.h>
#include <efiwindow.h>
#include "elf.h"
#include "tloadkif.h"
#include <png.h>

EFI_SYSTEM_TABLE *ST;
EFI_BOOT_SERVICES *BS;
EFI_RUNTIME_SERVICES *RT;

Elf64_Ehdr *ehdr;
FILE *kfile;

void *gdtidtpage;

/* Load files (and thus report progress) in 512 kiB chunks */
#define EFI_LOAD_FILE_CHUNK_SIZE		0x80000

EFI_STATUS efi_load_file(EFI_FILE *root, CHAR16 *fname, void **file, size_t *fsize, void (*report_progress)(int percent));
extern EFI_FILE *fopen_root;

char *empty_string = "";
char *loader_name = "tloadefi";

struct mod_info {
	char *name;
	uintptr_t addr;
	uintptr_t size;
};

void press_key()
{
	printf("Press any key to continue...");
	getchar();
	printf("\n");
}

static EFI_STATUS load_driver(char *fname, EFI_HANDLE ParentHandle);

EFI_STATUS efi_main(EFI_HANDLE ImageHandle, EFI_SYSTEM_TABLE *SystemTable)
{
	EFI_STATUS s;
	unsigned int i;

	/* Store the EFI tables */
	ST = SystemTable;
	BS = ST->BootServices;
	RT = ST->RuntimeServices;

	/* Initialise efilibc */
	efilibc_init(ImageHandle);

	/* Report where we are loaded */
	EFI_LOADED_IMAGE *li;
	BS->HandleProtocol(ImageHandle, &LoadedImageProtocol, (void **)&li);
	printf("Image base: %#llx\n", li->ImageBase);

	printf("efi_main @ %#llx\n", (uint64_t)efi_main);

	/* Disable watchdog timer */
	s = BS->SetWatchdogTimer(0, 0, 0, NULL);
	if(EFI_ERROR(s))
		fprintf(stderr, "could not disable watchdog timer\n");

	/* Load PS/2 driver */
	load_driver("/EFI/modules/Ps2MouseDxe.efi", ImageHandle);

	/* Initialise efiwindow */
	s = ew_init(ImageHandle);
	if(EFI_ERROR(s))
	{
		printf("Could not initialise efiwindow library\n");
		fprintf(stderr, "ew_init returned %i\n", s);
		press_key();
		return s;
	}

	/* Load font */
	FONT *font;
	s = ew_load_psf_font(&font, "boot/unifont.psf");
	if(EFI_ERROR(s))
	{
		printf("Could not load boot/unifont.psf\n");
		fprintf(stderr, "ew_load_psf_font returned %i\n", s);
		press_key();
		return s;
	}

	/* Attempt to set 800x600x32 */
	s = ew_set_mode(800, 600, 32);
	if(EFI_ERROR(s))
	{
		printf("Could not change graphics mode: %i\n");
		fprintf(stderr, "could not set mode: %i\n", s);
		press_key();
		return s;
	}

	fprintf(stderr, "set mode\n");

	/* Enable blending */
	ew_set_blend_mode(1, EW_BLEND_SRCALPHA, EW_BLEND_INVSRCALPHA);

	/* Load a PNG file */
	RECT png_r = { 0, 0, 800, 600 };
	WINDOW *bmp_window;

	EW_IMAGE img;
	memset(&img, 0, sizeof(EW_IMAGE));
	img.fname = "boot/tysos2.png";
	img.image_type = EW_IMAGE_TYPE_GUESS;
	s = ew_create_image(&img);
	fprintf(stderr, "ew_create_image: %i\n", s);

	s = ew_create_bitmap(&bmp_window, &png_r, EW_DESKTOP, &img, EW_BITMAP_STRETCH_NONE);
	if(EFI_ERROR(s))
	{
		printf("ew_create_bitmap failed: %i\n", s);
		press_key();
		return s;
	}
	ew_show(bmp_window);

	/* Handle messages */
	ew_message_wait(NULL);

	/* Create a progress bar */
	RECT pb_r = { 80, 460, 640, 60 };
	WINDOW *pb;
	EW_PROGRESS pi = { EW_DARKBLUE, EW_CLEAR, EW_BLACK, 2, 0, EW_PROGRESS_LEFTRIGHT, font, EW_WHITE, 16, 32 };
	s = ew_create_progress(&pb, &pb_r, EW_DESKTOP, &pi);
	if(EFI_ERROR(s))
	{
		printf("ew_create_progress failed: %i\n", s);
		press_key();
		return s;
	}
	ew_show(pb);

	/* Create a console window */
	RECT r;
	r.x = 80;
	r.w = 640;
	r.y = 80;
	r.h = 272;

	WINDOW *console;
	EW_TEXTBOX console_info = { font, 80, 17, EW_WHITE, EW_MAKECOLOR(EW_BLUE, EW_ALPHA50), EW_TEXTBOX_ALIGN_TOP | EW_TEXTBOX_ALIGN_LEFT, 1 };
	s = ew_create_textbox(&console, &r, EW_DESKTOP, &console_info);
	if(EFI_ERROR(s))
	{
		printf("Could not create console window\n");
		fprintf(stderr, "ew_create_textbox returned %i\n", s);
		press_key();
		return s;
	}
	size_t data_size;
	void *console_data;
	fprintf(stderr, "before get_data 1\n");
	ew_get_data(console, NULL, &data_size);
	console_data = malloc(data_size);
	assert(console_data);
	fprintf(stderr, "before get_data 2\n");
	ew_get_data(console, console_data, &data_size);
	fprintf(stderr, "after get_data\n");
	efilibc_register_stdout_fwrite(ew_textbox_fwrite, console_data);

	ew_show(console);

	/* Create a info bar */
	RECT info_r = { 80, 376, 640, 60 };
	EW_TEXTBOX info_tb = { font, 80, 1, EW_WHITE, EW_MAKECOLOR(EW_BLUE, EW_ALPHA50), EW_TEXTBOX_ALIGN_CENTER, 1 };
	WINDOW *info_w;
	s = ew_create_textbox(&info_w, &info_r, EW_DESKTOP, &info_tb);
	if(EFI_ERROR(s))
	{
		printf("ew_create_textbox failed: %i\n", s);
		press_key();
		return s;
	}
	ew_show(info_w);
	fprintf(stderr, "info box: %#016llx\n", info_w);

	fprintf(stderr, "console initialized\n");

	/* Animate the progress bar a bit */
	int pb_v = 0;
	for(pb_v = 0; pb_v <= 100; pb_v += 5)
	{
		pi.progress = pb_v;
		ew_set_progress_info(pb, &pi);
		char progress_t[80];
		sprintf(progress_t, "Progress: %i%%", pb_v);
		ew_set_textbox_text(info_w, progress_t, EW_TEXTBOX_WHOLE_STRING);
		BS->Stall(500000);
	}

	/* Attempt to load the config file */
	cfg_opt_t kernel_opts[] = {
		CFG_STR("path", "/boot/tysos.bin", CFGF_NONE),
		CFG_STR("cmdline", "", CFGF_NONE),
		CFG_END()
	};

	cfg_opt_t module_opts[] = {
		CFG_STR("name", "", CFGF_NONE),
		CFG_STR("path", "", CFGF_NODEFAULT),
		CFG_END()
	};
	
	cfg_opt_t opts[] = {
		CFG_SEC("kernel", kernel_opts, CFGF_NONE),
		CFG_SEC("module", module_opts, CFGF_MULTI),
		CFG_END()
	};
	cfg_t *cfg = NULL;
	cfg = cfg_init(opts, 0);

	switch(cfg_parse(cfg, "boot/boot.mnu"))
	{
		case CFG_FILE_ERROR:
			fprintf(stderr, "error: configuration file could not be read (%s)\n", strerror(errno));
			fprintf(stdout, "error: configuration file could not be read (%s)\n", strerror(errno));
			press_key();
			return EFI_NOT_FOUND;
		case CFG_SUCCESS:
			break;
		case CFG_PARSE_ERROR:
			fprintf(stderr, "error: parse error on configuration file\n");
			press_key();
			return EFI_OUT_OF_RESOURCES;
		default:
			fprintf(stderr, "error: unknown error\n");
			press_key();
			return EFI_OUT_OF_RESOURCES;
	}

	cfg_t *kernel_cfg = cfg_getsec(cfg, "kernel");
	char *kpath;
	if(kernel_cfg == NULL)
	{
		printf("no kernel section\n");
		press_key();
		return EFI_NOT_FOUND;
	}
	else
	{
		kpath = cfg_getstr(kernel_cfg, "path");
		printf("kernel: %s, cmdline: %s\n", cfg_getstr(kernel_cfg, "path"),
			cfg_getstr(kernel_cfg, "cmdline"));
	}

	for(i = 0; i < cfg_size(cfg, "module"); i++)
	{
		cfg_t *module_cfg = cfg_getnsec(cfg, "module", i);
		
		char *module_name = cfg_getstr(module_cfg, "name");
		char *module_path = cfg_getstr(module_cfg, "path");
		if(module_name == NULL)
			module_name = module_path;

		printf("module: %s: %s\n", module_name, module_path);
	}
	printf("end of configuration\n");

	if(kpath != NULL)
	{
		kfile = fopen(kpath, "r");
		if(kfile == NULL)
		{
			fprintf(stderr, "fopen(%s) failed: %s\n", kpath, strerror(errno));
			return EFI_NOT_FOUND;
		}

		int eret = elf_read_ehdr(kfile, (void **)&ehdr, NULL, EM_X86_64, ELFDATA2LSB);
		if(eret != ELF_OK)
		{
			fprintf(stderr, "elf_read_ehdr() failed: %i\n", eret);
			return EFI_NOT_FOUND;
		}

		printf("kernel ELF headers parsed\n");

		uint8_t *phdrs;
		eret = elf64_read_phdrs(kfile, ehdr, &phdrs);
		if(eret != ELF_OK)
		{
			fprintf(stderr, "elf_read_phdrs() failed: %i\n", eret);
			return EFI_NOT_FOUND;
		}

		printf("kernel program headers loaded\n");

		for(i = 0; i < ehdr->e_phnum; i++)
		{
			Elf64_Phdr *phdr = (Elf64_Phdr *)&phdrs[i * ehdr->e_phentsize];

			if(phdr->p_flags & PT_LOAD)
			{
				eret = elf64_load_segment(kfile, phdr);
				if(eret != ELF_OK)
				{
					fprintf(stderr, "elf_load_segment(%i) failed: %i\n", i, eret);
					return EFI_NOT_FOUND;
				}
			}
		}

		printf("kernel segments loaded\n");

		Elf64_Sym putc_sym;
		eret = elf64_find_symbol(kfile, ehdr, &putc_sym, "putc");
		if(eret != ELF_OK)
			fprintf(stderr, "elf64_find_symbol(putc) failed: %i\n", eret);
		else
		{
			printf("putc @ %#016llx\n", putc_sym.st_value);
		}
	}

	/* Find the symbol table */
	uint8_t *shdrs;
	elf64_read_shdrs(kfile, ehdr, &shdrs);
	Elf64_Shdr **sym_tabs;
	int sym_tab_count = 0;
	elf64_find_section(kfile, ehdr, shdrs, NULL, MF_TYPE, NULL, SHT_SYMTAB, 0, &sym_tabs, &sym_tab_count);
	printf("found %i symbol tables\n", sym_tab_count);
	Elf64_Shdr *sym_tab, *symstr_tab;

	if(sym_tab_count < 1)
	{
		fprintf(stderr, "no symbol table found!\n");
		printf("no symbol table found!\n");
		press_key();
		return EFI_DEVICE_ERROR;
	}
	sym_tab = sym_tabs[0];
	/* Load up the associated string table */
	int eret = elf64_find_section_by_idx(kfile, ehdr, shdrs, sym_tab->sh_link, &symstr_tab);
	if(eret != ELF_OK)
	{
		fprintf(stderr, "error loading symbol string table header: %i\n", eret);
		printf("error loading symbol string table header\n");
		press_key();
		return EFI_DEVICE_ERROR;
	}

	/* Load the symbol and string table */
	eret = elf64_load_section(kfile, sym_tab);
	if(eret != ELF_OK)
	{
		fprintf(stderr, "error loading symbol table: %i\n", eret);
		printf("error loading symbol table\n");
		press_key();
		return EFI_DEVICE_ERROR;
	}
	eret = elf64_load_section(kfile, symstr_tab);
	if(eret != ELF_OK)
	{
		fprintf(stderr, "error loading symbol string table: %i\n", eret);
		printf("error loading symbol string table\n");
		press_key();
		return EFI_DEVICE_ERROR;
	}

	/* Load modules */
	unsigned int modules_count = cfg_size(cfg, "module");
	struct mod_info *mods = (struct mod_info *)malloc(modules_count * sizeof(struct mod_info));
	memset(mods, 0, modules_count * sizeof(struct mod_info));
	assert(mods);
	for(i = 0; i < modules_count; i++)
	{
		cfg_t *module_cfg = cfg_getnsec(cfg, "module", i);
		
		char *module_name = cfg_getstr(module_cfg, "name");
		char *module_path = cfg_getstr(module_cfg, "path");
		if(module_name == NULL)
			module_name = module_path;

		CHAR16 *lpath= (CHAR16 *)malloc((strlen(module_path) + 1) * sizeof(CHAR16));
		mbstowcs((wchar_t *)lpath, module_path, strlen(module_path));
		s = efi_load_file(fopen_root, lpath, (void **)&mods[i].addr, (size_t *)&mods[i].size, NULL);
		if(EFI_ERROR(s))
			fprintf(stderr, "efi_load_file(%s) failed: %i\n", module_path, s);
		else
			printf("Loaded module %s\n", module_name);
	}

	/* Generate the KIF */
	struct Multiboot_Header *kif = (struct Multiboot_Header *)malloc(sizeof(struct Multiboot_Header));
	memset(kif, 0, sizeof(struct Multiboot_Header));
	Init_Multiboot_Header(kif);
	
	/* cmdline */
	struct System_String *cmdline_str;
	char *cmdline = cfg_getstr(kernel_cfg, "cmdline");
	if(cmdline == NULL)
		cmdline = empty_string;
	CreateString(&cmdline_str, cmdline);
	kif->cmdline = (uint64_t)cmdline_str;

	/* debug */
	kif->debug = 0;

	/* Allocate space for our gdt and idt (will be set up properly after calling ExitBootServices */
	s = BS->AllocatePages(AllocateAnyPages, EfiLoaderData, 1, (EFI_PHYSICAL_ADDRESS *)&gdtidtpage);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "Error allocating page for GDT/IDT: %i\n", s);
		printf("Couldn't allocate GDT/IDT page\n");
		press_key();
		return s;
	}
	kif->gdt = (uint64_t)gdtidtpage;
	fprintf(stderr, "GDT/IDT page at %#016llx\n", gdtidtpage);

	/* Allocate basic heap for tysos (1MiB) */
	size_t heap_size = 0x100000;
	EFI_PHYSICAL_ADDRESS heap;
	s = BS->AllocatePages(AllocateAnyPages, EfiLoaderData, heap_size / 0x1000, &heap);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "Couldn't allocate %#0llx bytes for heap: %i\n", heap_size, s);
		printf("Couldn't allocate heap\n");
		press_key();
		return s;
	}
	kif->heap_start = (uint64_t)heap;
	kif->heap_end = kif->heap_start + heap_size;
	fprintf(stderr, "Initial heap %#016llx-%#016llx\n", kif->heap_start, kif->heap_end);

	/* Machine type */
	kif->machine_major_type = x86_64;

	/* Framebuffer info */
	kif->fb_base = (uint64_t)GOP->Mode->FrameBufferBase;
	kif->fb_w = GOP->Mode->Info->HorizontalResolution;
	kif->fb_h = GOP->Mode->Info->VerticalResolution;
	kif->fb_stride = GOP->Mode->Info->PixelsPerScanLine;
	kif->fb_bpp = 32;
	switch(GOP->Mode->Info->PixelFormat)
	{
		case PixelRedGreenBlueReserved8BitPerColor:
			kif->fb_pixelformat = RGB;
			break;
		case PixelBlueGreenRedReserved8BitPerColor:
			kif->fb_pixelformat = BGR;
			break;
		default:
			fprintf(stderr, "Warning: unknown pixel format: %i\n", GOP->Mode->Info->PixelFormat);
			break;
	}
	fprintf(stderr, "Framebuffer info: base: %#016llx, w: %i, h: %i, stride: %i, bpp: %i, pixel_format: %i\n",
		kif->fb_base, kif->fb_w, kif->fb_h, kif->fb_stride, kif->fb_bpp, kif->fb_pixelformat);

	/* ELF info */
	kif->tysos_sym_tab_entsize = sym_tab->sh_entsize;
	kif->tysos_sym_tab_paddr = sym_tab->sh_addr;
	kif->tysos_sym_tab_size = sym_tab->sh_size;
	kif->tysos_str_tab_paddr = symstr_tab->sh_addr;
	kif->tysos_str_tab_size = symstr_tab->sh_size;
	fprintf(stderr, "Symbol table: entsize: %i, paddr: %#016llx, size: %i,  String table: paddr: %#016llx, size: %i\n",
		kif->tysos_sym_tab_entsize, kif->tysos_sym_tab_paddr, kif->tysos_sym_tab_size,
		kif->tysos_str_tab_paddr, kif->tysos_str_tab_size);

	/* loader name */
	struct System_String *loader_name_str;
	CreateString(&loader_name_str, loader_name);
	kif->loader_name = (uint64_t)loader_name_str;

	void (*epoint)();
	elf64_get_entry_point(kfile, (Elf64_Ehdr *)ehdr, (void **)&epoint);

	printf("About to start kernel (%#016llx):\n", (uintptr_t)epoint);
	press_key();
	epoint();

	return EFI_SUCCESS;
}

int elf_request_memory(uintptr_t base, uintptr_t size)
{
	uintptr_t end = base + size;
	base &= ~0xfff;
	if(end & 0xfff)
	{
		end += 0x1000;
		end &= ~0xfff;
	}

	EFI_PHYSICAL_ADDRESS addr = (EFI_PHYSICAL_ADDRESS)base;
	EFI_STATUS s = BS->AllocatePages(AllocateAddress, EfiLoaderData, (end - base) / 0x1000, &addr);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "elf_request_memory: request from %#016llx - %#016llx failed: %i\n",
			base, end, s);
	}
	return s;
}

uint64_t Get_Symbol_Addr(const char *name)
{
	Elf64_Sym s;
	if(elf64_find_symbol(kfile, (Elf64_Ehdr *)ehdr, &s, name) == ELF_OK)
		return s.st_value;
	return 0;
}

EFI_STATUS efi_load_file(EFI_FILE *root, CHAR16 *fname, void **file, size_t *fsize, void (*report_progress)(int percent))
{
	EFI_STATUS s;

	/* Attempt to open the file */
	EFI_FILE *f;
	s = root->Open(root, &f, fname, EFI_FILE_MODE_READ, 0);
	if(EFI_ERROR(s))
		return s;

	/* Get its info to determine its size */
	EFI_FILE_INFO *fi = NULL;
	UINTN fi_size = 0;
	s = f->GetInfo(f, &GenericFileInfo, &fi_size, fi);
	if(s != EFI_BUFFER_TOO_SMALL)
		return s;

	s = BS->AllocatePool(EfiLoaderData, fi_size, (void **)&fi);
	if(EFI_ERROR(s))
		return s;
	s = f->GetInfo(f, &GenericFileInfo, &fi_size, fi);
	if(EFI_ERROR(s))
	{
		BS->FreePool(fi);
		return s;
	}

	/* Get the file size, aligned up */
	unsigned long flen = (unsigned long)fi->FileSize;
	UINTN flen_unalign = flen;
	BS->FreePool(fi);
	if(flen % 4096)
	{
		flen -= (flen % 4096);
		flen += 4096;
	}

	/* Get memory for the file */
	uint8_t *buf = NULL;
	s = BS->AllocatePages(AllocateAnyPages, EfiLoaderData, flen / 4096, (EFI_PHYSICAL_ADDRESS *)&buf);
	if(EFI_ERROR(s))
		return s;

	/* Load the file in chunks, reporting progress as we go */
	UINTN cur_file_pointer = 0;
	while (cur_file_pointer < flen_unalign)
	{
		UINTN bytes_to_read = EFI_LOAD_FILE_CHUNK_SIZE;
		if((cur_file_pointer + bytes_to_read) > flen_unalign)
			bytes_to_read = flen_unalign - cur_file_pointer;
		UINTN bytes_read = bytes_to_read;

		if(report_progress != NULL)
			report_progress((int)((double)cur_file_pointer / flen_unalign * 100.0));

		s = f->Read(f, &bytes_to_read, &(((uint8_t *)buf)[cur_file_pointer]));
		if(EFI_ERROR(s))
			return s;
		if(bytes_to_read != bytes_read)
			return EFI_END_OF_FILE;

		cur_file_pointer += bytes_read;
	}

	/* Close and return */
	*fsize = (size_t)flen_unalign;
	*file = buf;
	return f->Close(f);
}

EFI_STATUS load_driver(char *fname, EFI_HANDLE ParentHandle)
{
	FILE *fd = fopen(fname, "r");
	if(fd == NULL)
		return EFI_NOT_FOUND;
	size_t f_len = (size_t)fsize(fd);
	void *f_buf = malloc(f_len);
	if(f_buf == NULL)
		return EFI_OUT_OF_RESOURCES;
	fread(f_buf, 1, f_len, fd);
	fclose(fd);

	EFI_HANDLE h;
	EFI_STATUS s = BS->LoadImage(0, ParentHandle, NULL, f_buf, f_len, &h);
	free(f_buf);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "LoadImage returned: %i\n", s);
		return s;
	}

	s = BS->StartImage(h, NULL, NULL);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "StartImage returned: %i\n", s);
		return s;
	}

	// This portion of the function was from from the BdsLib implementation in
	// IntelFrameworkModulePkg\Library\GenericBdsLib\BdsConnect.c
	// function name: BdsLibConnectAllEfi
	UINTN       HandleCount;
	EFI_HANDLE  *HandleBuffer;
	UINTN       Index;

	/* Try and connect the driver to any device */
	s = BS->LocateHandleBuffer(AllHandles, NULL, NULL, &HandleCount, &HandleBuffer);
	if (EFI_ERROR (s)) {
		fprintf(stderr, "LocateHandleBuffer failed: %i\n", s);
		return s;
	}

	for (Index = 0; Index < HandleCount; Index++) {
		BS->ConnectController (HandleBuffer[Index], NULL, NULL, TRUE);
	}

	BS->FreePool (HandleBuffer);

	return EFI_SUCCESS;
}