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
#include "part.h"
#include "fstypes.h"
#include <stddef.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <assert.h>
#include <errno.h>
#include <limits.h>

void dump_help(char *fname);
int check_parts();
int parse_opts(int argc, char **argv);
int parse_guid(char *str, GUID *guid);
void write_output();

int CalculateCrc32 (uint8_t *Data, size_t DataSize, uint32_t *CrcOut);

size_t sect_size = 512;
long image_sects = 0;
PART *first_part = NULL;
PART *last_part = NULL;
FILE *output = NULL;
GUID disk_guid;
int part_count;
int header_sectors;
int first_usable_sector;
int secondary_headers_sect;
int secondary_gpt_sect;

int main(int argc, char **argv)
{
	init_fstypes();
	random_guid(&disk_guid);

	if(parse_opts(argc, argv) != 0)
		return -1;
	
	if(output == NULL)
	{
		fprintf(stderr, "no output file specifed\n");
		dump_help(argv[0]);
		return -1;
	}
	if(first_part == NULL)
	{
		fprintf(stderr, "no partitions specified\n");
		dump_help(argv[0]);
		return -1;
	}

	if(check_parts() != 0)
		return -1;

	write_output();
	fclose(output);

	return 0;
}

int parse_opts(int argc, char **argv)
{
	int i = 1;
	int cur_part_id = 0;
	PART *cur_part = NULL;

	/* First, parse global options */
	while(i < argc)
	{
		if(!strcmp(argv[i], "--output") || !strcmp(argv[i], "-o"))
		{
			i++;
			if(i == argc || argv[i][0] == '-')
			{
				fprintf(stderr, "no output file specified\n");
				return -1;
			}

			output = fopen(argv[i], "w+");
			if(output == NULL)
			{
				fprintf(stderr, "unable to open %s for writing (%s)\n", argv[i],
					strerror(errno));
				return -1;
			}
			i++;
		}
		else if(!strcmp(argv[i], "--disk-guid"))
		{
			i++;
			if(i == argc || argv[i][0] == '-')
			{
				fprintf(stderr, "no disk guid file specified\n");
				return -1;
			}

			if(parse_guid(argv[i], &disk_guid) != 0)
			{
				fprintf(stderr, "invalid disk uuid (%s)\n", argv[i]);
				return -1;
			}
			
			i++;
		}
		else if(!strcmp(argv[i], "--help") || !strcmp(argv[i], "-h"))
		{
			dump_help(argv[0]);
			return -1;
		}
		else if(!strcmp(argv[i], "--sector-size"))
		{
			i++;
			if(i == argc || argv[i][0] == '-')
			{
				fprintf(stderr, "sector size not specified\n");
				return -1;
			}
		
			sect_size = atoi(argv[i]);

			if(sect_size < 512 || sect_size > 4096 || sect_size % 512)
			{
				fprintf(stderr, "invalid sector size (%zu) - must be >= 512 and <= 4096 and "
					"a multiple of 512", sect_size);
				return -1;
			}
			i++;
		}
		else if(!strcmp(argv[i], "--image-size"))
		{
			i++;
			if(i == argc || argv[i][0] == '-')
			{
				fprintf(stderr, "image size not specified\n");
				return -1;
			}
		
			image_sects = atoi(argv[i]);

			i++;		
		}
		else if(!strcmp(argv[i], "--part") || !strcmp(argv[i], "-p"))
			break;
		else
		{
			fprintf(stderr, "unknown argument - %s\n", argv[i]);
			dump_help(argv[0]);
			return i;
		}
	}

	/* Now parse partitions */
	while(i < argc)
	{
		if(!strcmp(argv[i], "--part") || !strcmp(argv[i], "-p"))
		{
			/* Store the current partition data if there is one */
			if(cur_part != NULL)
			{
				if(last_part == NULL)
				{
					first_part = last_part = cur_part;
					cur_part->next = NULL;
				}
				else
				{
					last_part->next = cur_part;
					last_part = cur_part;
					cur_part->next = NULL;
				}
			}

			/* Allocate a new partition structure */
			cur_part = (PART *)malloc(sizeof(PART));
			if(cur_part == NULL)
			{
				fprintf(stderr, "out of memory allocating partition structure\n");
				return -1;
			}
			memset(cur_part, 0, sizeof(PART));
			cur_part_id++;
			cur_part->id = cur_part_id;

			/* Get the filename of the partition image */
			i++;
			if(i == argc || argv[i][0] == '-')
			{
				fprintf(stderr, "no partition image specified for partition %i\n", cur_part_id);
				return -1;
			}
			cur_part->src = fopen(argv[i], "r");
			if(cur_part->src == NULL)
			{
				fprintf(stderr, "unable to open partition image (%s) for partition (%i) - %s\n", argv[i],
					cur_part_id, strerror(errno));
				return -1;
			}

			i++;
		}
		else if(!strcmp(argv[i], "--name") || !strcmp(argv[i], "-n"))
		{
			if(cur_part == NULL)
			{
				fprintf(stderr, "--part must be specified before --name argument\n");
				return -1;
			}

			i++;
			if(i == argc || argv[i][0] == '-')
			{
				fprintf(stderr, "partition name not specified %i\n", cur_part_id);
				return -1;
			}

			cur_part->name = argv[i];

			i++;
		}
		else if(!strcmp(argv[i], "--type") || (!strcmp(argv[i], "-t")))
		{
			if(cur_part == NULL)
			{
				fprintf(stderr, "--part must be specifed before --type argument\n");
				return -1;
			}

			i++;
			if(i == argc || argv[i][0] == '-')
			{
				fprintf(stderr, "partition type not specified %i\n", cur_part_id);
				return -1;
			}

			if(parse_guid(argv[i], &cur_part->type) != 0)
			{
				fprintf(stderr, "invalid partition type (%s) for partition %i\n", argv[i], cur_part_id);
				return -1;
			}
			
			i++;
		}
		else if(!strcmp(argv[i], "--uuid") || (!strcmp(argv[i], "-u")))
		{
			if(cur_part == NULL)
			{
				fprintf(stderr, "--part must be specifed before --uuid argument\n");
				return -1;
			}

			i++;
			if(i == argc || argv[i][0] == '-')
			{
				fprintf(stderr, "partition uuid not specified %i\n", cur_part_id);
				return -1;
			}

			if(parse_guid(argv[i], &cur_part->uuid) != 0)
			{
				fprintf(stderr, "invalid partition uuid (%s) for partition %i\n", argv[i], cur_part_id);
				return -1;
			}
			
			i++;
		}
		else
		{
			fprintf(stderr, "unknown argument - %s\n", argv[i]);
			dump_help(argv[0]);
			return i;
		}
	}

	if(cur_part != NULL)
	{
		if(last_part == NULL)
		{
			first_part = last_part = cur_part;
			cur_part->next = NULL;
		}
		else
		{
			last_part->next = cur_part;
			last_part = cur_part;
			cur_part->next = NULL;
		}
	}

	return 0;
}

void dump_help(char *fname)
{
	printf("Usage: %s -o <output_file> [-h] [--sector-size sect_size] [partition def 0] [part def 1] ... [part def n]\n",
		fname);

}

int parse_guid(char *str, GUID *guid)
{
	long mbr_id = -1;
	int i;

	/* detect request for random uuid */
	if(!strcmp(str, "random") || !strcmp(str, "rnd"))
		return random_guid(guid);

	/* detect mbr partition id by number */
	mbr_id = strtol(str, NULL, 0);
	if(mbr_id == LONG_MIN || mbr_id == LONG_MAX)
		mbr_id = -1;
	
	/* detect by name */
	for(i = 0; i < 512; i++)
	{
		if(fsnames[i] == NULL)
			continue;
		if(!strcmp(fsnames[i], str))
		{
			mbr_id = i;
			break;
		}
	}

	if(mbr_id >= 0 && mbr_id <= 511)
	{
		if(guid_is_zero(&fstypes[mbr_id]))
			return -1;
		memcpy(guid, &fstypes[mbr_id], sizeof(GUID));
		return 0;
	}

	/* try and parse as guid */
	return string_to_guid(guid, str);
}

int check_parts()
{
	/* Iterate through the partitions, checking validity */
	int cur_part_id = 0;
	int cur_sect;
	PART *cur_part;
	int header_length;
	int needed_file_length;

	/* Count partitions */
	cur_part = first_part;
	part_count = 0;
	while(cur_part)
	{
		part_count++;
		cur_part = cur_part->next;
	}

	/* Determine the sectors needed for MBR, GPT header and partition entries */
	cur_sect = 2;	/* MBR + GPT header */
	header_length = part_count * 128;
	header_sectors = header_length / sect_size;
	if(header_length % sect_size)
		header_sectors++;
	cur_sect += header_sectors;
	first_usable_sector = cur_sect;

	cur_part = first_part;
	while(cur_part)
	{
		long cur_part_file_len;

		cur_part_id++;

		if(guid_is_zero(&cur_part->type))
		{
			fprintf(stderr, "partition type not specified for partition %i\n", cur_part_id);
			return -1;
		}

		if(guid_is_zero(&cur_part->uuid))
			random_guid(&cur_part->uuid);

		if(cur_part->sect_start == 0)
			cur_part->sect_start = cur_sect;
		else if(cur_part->sect_start < cur_sect)
		{
			fprintf(stderr, "unable to start partition %i at sector %i (would conflict with other data)\n",
				cur_part_id, cur_part->sect_start);
			return -1;
		}

		if(cur_part->name == NULL)
		{
			cur_part->name = (char *)malloc(128);
			sprintf(cur_part->name, "part%i", cur_part_id);
		}

		fseek(cur_part->src, 0, SEEK_END);
		cur_part_file_len = ftell(cur_part->src);
		fseek(cur_part->src, 0, SEEK_SET);

		if(cur_part->sect_length == 0)
		{
			cur_part->sect_length = cur_part_file_len / sect_size;
			if(cur_part_file_len % sect_size)
				cur_part->sect_length++;
		}
		cur_sect = cur_part->sect_start + cur_part->sect_length;

		cur_part = cur_part->next;
	}

	/* Add space for the secondary GPT */
	needed_file_length = cur_sect + 1 + header_sectors;

	if(image_sects == 0)
		image_sects = needed_file_length;
	else if(image_sects < needed_file_length)
	{
		fprintf(stderr, "requested image size (%lu) is too small to hold the partitions\n", image_sects * sect_size);
		return -1;
	}

	secondary_headers_sect = image_sects - 1 - header_sectors;
	secondary_gpt_sect = image_sects - 1;

	return 0;
}

void write_output()
{
	int i;
	uint8_t *mbr, *gpt, *gpt2, *parts, *image_buf;
	PART *cur_part;

	/* Write MBR */
	mbr = (uint8_t *)malloc(sect_size);
	memset(mbr, 0, sect_size);

	*(uint32_t *)&mbr[446] = 0x00020000;		/* boot indicator = 0, start CHS = 0x000200 */
	mbr[446 + 4] = 0xee;						/* OSType = GPT Protective */
	mbr[446 + 5] = 0xff;
	mbr[446 + 6] = 0xff;
	mbr[446 + 7] = 0xff;						/* EndingCHS = 0xffffff */
	*(uint32_t *)&mbr[446 + 8] = 0x1;			/* StartingLBA = 1 */
		
	if(image_sects > 0xffffffff)
		*(uint32_t *)&mbr[446 + 12] = 0xffffffff;
	else
		*(uint32_t *)&mbr[446 + 12] = (uint32_t)image_sects - 1;

	mbr[510] = 0x55; mbr[511] = 0xaa;			/* Signature */

	assert(fwrite(mbr, 1, sect_size, output) == sect_size);

	/* Define GPT headers */
	gpt = (uint8_t *)malloc(sect_size);
	assert(gpt);
	gpt2 = (uint8_t *)malloc(sect_size);
	assert(gpt2);

	memset(gpt, 0, sect_size);
	memset(gpt2, 0, sect_size);

	*(uint64_t *)&gpt[0] = 0x5452415020494645ULL;		/* Signature */
	*(uint32_t *)&gpt[8] = 0x00010000UL;				/* Revision */
	*(uint32_t *)&gpt[12] = 96;							/* HeaderSize */
	*(uint32_t *)&gpt[16] = 0;							/* HeaderCRC32 */
	*(uint32_t *)&gpt[20] = 0;							/* Reserved */
	*(uint64_t *)&gpt[24] = 0x1;						/* MyLBA */
	*(uint64_t *)&gpt[32] = secondary_gpt_sect;			/* AlternateLBA */
	*(uint64_t *)&gpt[40] = first_usable_sector;		/* FirstUsableLBA */
	*(uint64_t *)&gpt[48] = secondary_headers_sect - 1;	/* LastUsableLBA */
	guid_to_bytestring(&gpt[56], &disk_guid);			/* DiskGUID */
	*(uint64_t *)&gpt[72] = 0x2;						/* PartitionEntryLBA */
	*(uint32_t *)&gpt[80] = part_count;					/* NumberOfPartitionEntries */
	*(uint32_t *)&gpt[84] = 128;						/* SizeOfPartitionEntry */
	*(uint32_t *)&gpt[88] = 0;							/* PartitionEntryArrayCRC32 */

	/* Define GPT partition entries */
	parts = (uint8_t *)malloc(header_sectors * sect_size);
	assert(parts);
	memset(parts, 0, header_sectors * sect_size);

	cur_part = first_part;
	i = 0;
	while(cur_part)
	{
		int char_id;

		guid_to_bytestring(&parts[i * 128], &cur_part->type);			/* PartitionTypeGUID */
		guid_to_bytestring(&parts[i * 128 + 16], &cur_part->uuid);		/* UniquePartitionGUID */
		*(uint64_t *)&parts[i * 128 + 32] = cur_part->sect_start;		/* StartingLBA */
		*(uint64_t *)&parts[i * 128 + 40] = cur_part->sect_start + cur_part->sect_length - 1;	/* EndingLBA */
		*(uint64_t *)&parts[i * 128 + 48] = cur_part->attrs;			/* Attributes */

		for(char_id = 0; char_id < (int)strlen(cur_part->name) && char_id < 35; char_id++)
			*(uint16_t *)&parts[i * 128 + 56 + char_id * 2] = (uint16_t)cur_part->name[char_id];

		i++;
		cur_part = cur_part->next;
	}

	/* Do CRC calculations on the partition table entries and GPT headers */
	CalculateCrc32(parts, part_count * 128, (uint32_t *)&gpt[88]);
	CalculateCrc32(gpt, 96, (uint32_t *)&gpt[16]);

	memcpy(gpt2, gpt, 96);
	*(uint32_t *)&gpt2[16] = 0;							/* HeaderCRC32 */
	*(uint64_t *)&gpt2[24] = secondary_gpt_sect;		/* MyLBA */
	*(uint64_t *)&gpt2[32] = 0x1;						/* AlternateLBA */
	*(uint64_t *)&gpt2[72] = secondary_headers_sect;	/* PartitionEntryLBA */
	CalculateCrc32(gpt2, 96, (uint32_t *)&gpt2[16]);

	/* Write primary GPT and headers */
	assert(fwrite(gpt, 1, sect_size, output) == sect_size);
	assert(fwrite(parts, 1, header_sectors * sect_size, output) == header_sectors * sect_size);

	/* Write partitions */
	cur_part = first_part;
	image_buf = (uint8_t *)malloc(sect_size);
	while(cur_part)
	{
		size_t bytes_read;
		size_t bytes_written = 0;

		fseek(output, cur_part->sect_start * sect_size, SEEK_SET);
		while((bytes_read = fread(image_buf, 1, sect_size, cur_part->src)) > 0)
		{
			size_t bytes_to_write = bytes_read;

			/* Determine how much to write */
			if((bytes_written + bytes_to_write) > (size_t)(cur_part->sect_length * sect_size))
				bytes_to_write = cur_part->sect_length * sect_size - bytes_written;

			assert(fwrite(image_buf, 1, bytes_to_write, output) == bytes_to_write);

			bytes_written += bytes_to_write;
		}

		cur_part = cur_part->next;
	}

	/* Write secondary GPT partition headers and header */
	fseek(output, secondary_headers_sect * sect_size, SEEK_SET);
	assert(fwrite(parts, 1, header_sectors * sect_size, output) == header_sectors * sect_size);
	fseek(output, secondary_gpt_sect * sect_size, SEEK_SET);
	assert(fwrite(gpt2, 1, sect_size, output) == sect_size);
}
