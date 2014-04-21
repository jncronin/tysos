/*-----------------------------------------------------------------------*/
/* Low level disk I/O module skeleton for FatFs     (C)ChaN, 2013        */
/*-----------------------------------------------------------------------*/
/* If a working storage control module is available, it should be        */
/* attached to the FatFs via a glue function rather than modifying it.   */
/* This is an example of glue functions to attach various exsisting      */
/* storage control module to the FatFs module with a defined API.        */
/*-----------------------------------------------------------------------*/

#include <iostream>
#include "diskio.h"		/* FatFs lower layer API */
#include "ff.h"
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

using namespace std;

#define SECTOR_SIZE 512

/*-----------------------------------------------------------------------*/
/* Inidialize a Drive                                                    */
/*-----------------------------------------------------------------------*/

uint8_t *buf = NULL;
int sector_count = 0;

DSTATUS disk_initialize (
	BYTE pdrv				/* Physical drive nmuber (0..) */
)
{
	if((buf == NULL) || (pdrv != 0))
		return STA_NOINIT | STA_NODISK;
	else
		return 0;
}



/*-----------------------------------------------------------------------*/
/* Get Disk Status                                                       */
/*-----------------------------------------------------------------------*/

DSTATUS disk_status (
	BYTE pdrv		/* Physical drive nmuber (0..) */
)
{
	if((buf == NULL) || (pdrv != 0))
		return STA_NOINIT | STA_NODISK;
	else
		return 0;
}



/*-----------------------------------------------------------------------*/
/* Read Sector(s)                                                        */
/*-----------------------------------------------------------------------*/

DRESULT disk_read (
	BYTE pdrv,		/* Physical drive nmuber (0..) */
	BYTE *buff,		/* Data buffer to store read data */
	DWORD sector,	/* Sector address (LBA) */
	UINT count		/* Number of sectors to read (1..128) */
)
{
	if((buf == NULL) || (pdrv != 0))
		return RES_NOTRDY;

	if((sector + count) > sector_count)
		return RES_PARERR;

	memcpy(buff, &buf[sector * SECTOR_SIZE], count * SECTOR_SIZE);

	return RES_OK;
}



/*-----------------------------------------------------------------------*/
/* Write Sector(s)                                                       */
/*-----------------------------------------------------------------------*/

#if _USE_WRITE
DRESULT disk_write (
	BYTE pdrv,			/* Physical drive nmuber (0..) */
	const BYTE *buff,	/* Data to be written */
	DWORD sector,		/* Sector address (LBA) */
	UINT count			/* Number of sectors to write (1..128) */
)
{
	if((buf == NULL) || (pdrv != 0))
		return RES_NOTRDY;

	if((sector + count) > sector_count)
		return RES_PARERR;

	memcpy(&buf[sector * SECTOR_SIZE], buff, count * SECTOR_SIZE);

	return RES_OK;
}
#endif


/*-----------------------------------------------------------------------*/
/* Miscellaneous Functions                                               */
/*-----------------------------------------------------------------------*/

#if _USE_IOCTL
DRESULT disk_ioctl (
	BYTE pdrv,		/* Physical drive nmuber (0..) */
	BYTE cmd,		/* Control code */
	void *buff		/* Buffer to send/receive control data */
)
{
	if((buf == NULL) || (pdrv != 0))
		return RES_NOTRDY;

	switch(cmd)
	{
		case CTRL_SYNC:
			return RES_OK;
		case GET_SECTOR_COUNT:
			*(uint32_t *)buff = sector_count;
			return RES_OK;
		case GET_SECTOR_SIZE:
			*(uint32_t *)buff = SECTOR_SIZE;
			return RES_OK;
		case GET_BLOCK_SIZE:
			*(uint32_t *)buff = 1;
			return RES_OK;
		default:
			return RES_PARERR;
	}		
}
#endif

/* get_datetime implementation */
#include <boost/date_time/posix_time/posix_time.hpp>

DWORD get_fattime()
{
	/* Return time packed in DWORD value:

	bits 31:25		year from 1980
	bits 24:21		month
	bits 20:16		day in month
	bits 15:11		hour
	bits 10:5		minute
	bits 4:0		seconds / 2
	*/

	boost::posix_time::ptime p(boost::posix_time::microsec_clock::local_time());
	DWORD ret = 0;

	ret |= (p.time_of_day().seconds() / 2);
	ret |= (p.time_of_day().minutes() << 5);
	ret |= (p.time_of_day().hours() << 11);
	ret |= (p.date().day() << 16);
	ret |= (p.date().month() << 21);
	ret |= ((p.date().year() - 1980) << 25);

	return ret;
}