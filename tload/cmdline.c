/* Copyright (C) 2008 - 2011 by John Cronin
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

/* parse the command line */

/* command line arguments are in the form CMD_ARG = [' '*]NAME[' '*]['='[' '*]VALUE[' '*]]
The whole command line is CMD_LINE = [CMD_ARG][(','CMD_ARG)*] */

#include "cmdline.h"
#include "stdlib.h"
#include "console.h"
#include "malloc.h"

void interpret_cmd_arg(const char *name, const char *arg);
void _strip_whitespace(char **s);

char *kernel_name;
int tysos_debug = 0;

void interpret_cmd_arg(const char *name, const char *arg)
{
#ifdef _DEBUG
	_puts("    Interpreting command line arg: ", 0);
	_puts(name, 0);
	_puts(", val: ", 0);
	puts(arg);
#endif
	if(!strcmp("kernel", name))
		kernel_name = (char *)arg;
	else if(!strcmp("debug", name))
		tysos_debug = 1;
}

void parse_cmd_line(char *s)
{
	int i = 0;
	char *new_cmdline;

	new_cmdline = (char *)malloc(strlen(s) + 1);
	strncpy(new_cmdline, s, strlen(s));
	new_cmdline[strlen(s)] = '\0';
	s = new_cmdline;

#ifdef _DEBUG
	_puts("  Interpreting command line: ", 0);
	puts(s);
#endif

	while(*s == ' ') {
		s++;
	}
	if(*s == '\0')
		return;

	while(1) {
		char term_char;
		char *cur_str = &s[i];
		while((s[i] != '\0') && (s[i] != ',') && (s[i] != ' '))
			i++;
		term_char = s[i];
		if((term_char == ',') || (term_char == ' ')) {
			s[i] = '\0';
			i++;
		}
		parse_cmd_arg(cur_str);
		if(term_char == '\0')
			return;
	}
}

void parse_cmd_arg(char *cur_str)
{
	char *arg_name, *arg_val;

	arg_name = cur_str;
	
	while((*cur_str != '\0') && (*cur_str != '='))
		cur_str++;
	
	if(*cur_str == '\0')
		arg_val = NULL;
	else {
		*cur_str = '\0';
		arg_val = cur_str + 1;
	}

	_strip_whitespace(&arg_name);
	if(arg_val != NULL)
		_strip_whitespace(&arg_val);

	interpret_cmd_arg(arg_name, arg_val);
}

void _strip_whitespace(char **s)
{
	/* strip leading and trailing whitespace from a string */
	char *trailing;
	char *leading = *s;
	while(*leading == ' ')
		leading++;
	*s = leading;

	/* find the last character of the string */
	trailing = *s;
	while(*trailing != '\0')
		trailing++;
	trailing--;

	while((*trailing == ' ') && (trailing >= *s))
		trailing--;
	trailing++;
	*trailing = '\0';
}
