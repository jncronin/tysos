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
#include <stdlib.h>

struct cfg_module
{
	const char *name;
	const char *path;
	struct cfg_module *next;
};

static const char *kpath;
static const char *kcmdline;
static int mod_count = 0;

static struct cfg_module *first = NULL;
static struct cfg_module *cur = NULL;
static struct cfg_module *last = NULL;

int v_width = 1024;
int v_height = 768;
int v_bpp = 32;

char *splash;

struct cfg_module *cfg_iterate_modules()
{
	if(cur == NULL)
		cur = first;
	else
		cur = cur->next;

	return cur;
}

const char *cfg_get_kpath()
{
	return kpath;
}

const char *cfg_get_kcmdline()
{
	return kcmdline;
}

int cfg_get_modcount()
{
	return mod_count;
}

static void add(const char *name, const char *path)
{
	struct cfg_module *m = (struct cfg_module *)malloc(sizeof(struct cfg_module));
	m->name = name;
	m->path = path;
	m->next = NULL;

	if(first == NULL)
		first = last = m;
	else
	{
		last->next = m;
		last = m;
	}

	mod_count++;
}

static cfg_opt_t kernel_opts[] = {
	CFG_STR("path", "/boot/tysos.bin", CFGF_NONE),
	CFG_STR("cmdline", "", CFGF_NONE),
	CFG_END()
};

static cfg_opt_t module_opts[] = {
	CFG_STR("name", "", CFGF_NONE),
	CFG_STR("path", "", CFGF_NODEFAULT),
	CFG_END()
};

static cfg_opt_t video_opts[] = {
	CFG_STR("width", "1024", CFGF_NONE),
	CFG_STR("height", "768", CFGF_NONE),
	CFG_STR("bpp", "32", CFGF_NONE),
	CFG_STR("splash", "", CFGF_NONE),
	CFG_END()
};
	
static cfg_opt_t opts[] = {
	CFG_SEC("kernel", kernel_opts, CFGF_NONE),
	CFG_SEC("module", module_opts, CFGF_MULTI),
	CFG_SEC("video", video_opts, CFGF_NONE),
	CFG_END()
};

EFI_STATUS parse_cfg_file()
{
	/* Attempt to load the config file */
	cfg_t *cfg = NULL;
	cfg = cfg_init(opts, 0);

	switch(cfg_parse(cfg, "/boot/boot.mnu"))
	{
		case CFG_FILE_ERROR:
			fprintf(stderr, "error: configuration file could not be read (%s)\n", strerror(errno));
			fprintf(stdout, "error: configuration file could not be read (%s)\n", strerror(errno));
			return EFI_NOT_FOUND;
		case CFG_SUCCESS:
			break;
		case CFG_PARSE_ERROR:
			fprintf(stderr, "error: parse error on configuration file\n");
			return EFI_OUT_OF_RESOURCES;
		default:
			fprintf(stderr, "error: unknown error\n");
			return EFI_OUT_OF_RESOURCES;
	}

	cfg_t *kernel_cfg = cfg_getsec(cfg, "kernel");
	if(kernel_cfg == NULL)
	{
		printf("no kernel section\n");
		return EFI_NOT_FOUND;
	}
	else
	{
		kpath = cfg_getstr(kernel_cfg, "path");
		kcmdline = cfg_getstr(kernel_cfg, "cmdline");
		printf("kernel: %s, cmdline: %s\n", kpath, kcmdline);
	}

	for(int i = 0; i < (int)cfg_size(cfg, "module"); i++)
	{
		cfg_t *module_cfg = cfg_getnsec(cfg, "module", i);
		
		char *module_name = cfg_getstr(module_cfg, "name");
		char *module_path = cfg_getstr(module_cfg, "path");
		if(module_name == NULL)
			module_name = module_path;

		printf("module: %s: %s\n", module_name, module_path);

		add(module_name, module_path);
	}
	
	cfg_t *video_cfg = cfg_getsec(cfg, "video");
	if (video_cfg != NULL)
	{
		v_width = atoi(cfg_getstr(video_cfg, "width"));
		v_height = atoi(cfg_getstr(video_cfg, "height"));
		v_bpp = atoi(cfg_getstr(video_cfg, "bpp"));
		splash = cfg_getstr(video_cfg, "splash");
		printf("video: width: %d, height: %d\n",
			v_width, v_height);
	}

	printf("end of configuration\n");

	return EFI_SUCCESS;
}
