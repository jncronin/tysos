/* Run the correct compiler depending on the current environment

	Environents are: 1) csc under windows command prompt
 					 2) csc under cygwin
 					 3) mono under sh
*/

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <errno.h>

const char csc[] = "\\Microsoft.NET\\Framework\\v2.0.50727\\csc.exe";
const char gmcs[] = "gmcs";
const char *opts[] = { "/optimize+",
	"/t:exe",
	"/out:bin\\Release\\tybuild.exe",
	"project.cs",
	"solution.cs",
	"tybuild.cs",
	(char *)0
};

char **newopts;

void copy_opts()
{
	/* copy the opts array to a new allocated piece of memory */
	
	int i;
	int opt_count = 1;
	const char **c = opts;
	while(*c != (char *)0)
	{
		opt_count++;
		c++;
	}

	newopts = (char **)malloc(sizeof(char *) * opt_count);
	for(i = 0; i < opt_count; i++)
	{
		int slen;

		if(opts[i] == (char *)0)
		{
			newopts[i] = (char *)0;
			continue;
		}
	   
		slen = strlen(opts[i]);
		newopts[i] = (char *)malloc(sizeof(char) * (slen + 1));
		strcpy(newopts[i], opts[i]);
	}
}

void convert_to_unix(char *s)
{
	char *c = s;
	while(*c != '\0')
	{
		if(*c == '\\')
			*c = '/';
		c++;
	}
}

void dump_cmd_line(const char *cmd, char **options)
{
	char **c = options;

	printf("%s", cmd);
	while(*c != (char *)0)
	{
		printf(" %s", *c);
		c++;
	}
	printf("\n");
}

void convert_all_to_unix(char **s)
{
	char **c = s;
	while(*c != (char *)0)
	{
		convert_to_unix(*c);
		c++;
	}
}

int main(int argc, char **argv)
{
	char *windir;
	windir = getenv("WINDIR");

	copy_opts();

	if(windir == NULL)
	{
		/* gmcs on unix */
		convert_all_to_unix(newopts);
		dump_cmd_line(gmcs, newopts);
		execvp(gmcs, newopts);
	}
	else
	{
		/* Windows */

		char *newwindir = (char *)malloc(sizeof(char) * (strlen(windir) +
					strlen(csc) + 1));
		strcpy(newwindir, windir);
		strcat(newwindir, csc);

		if(getenv("USER") == NULL)
		{
			/* cmd on Windows */
			dump_cmd_line(newwindir, newopts);
			execvp(newwindir, newopts);
		}
		else
		{
			/* cygwin */
			convert_to_unix(newwindir);
			convert_all_to_unix(newopts);
			dump_cmd_line(newwindir, newopts);
			execvp(newwindir, newopts);
		}
	}

	/* We should not reach here as exec replaces the current process
	 * with a new one.  i.e. this is an error condition */

	printf("Error executing build command: %s\n", strerror(errno));

	return -1;
}


