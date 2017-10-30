%namespace typroject
%visibility internal

%x str

%{
	StringBuilder sb;
%}

%%

[0-9]+			yylval.intval = Int32.Parse(yytext); return (int)Tokens.INT;
"=="			return (int)Tokens.EQUALS;
"!="			return (int)Tokens.NOTEQUAL;
"!"				return (int)Tokens.NOT;
"<="			return (int)Tokens.LEQUAL;
">="			return (int)Tokens.GEQUAL;
"("				return (int)Tokens.LPAREN;
")"				return (int)Tokens.RPAREN;
"<"				return (int)Tokens.LT;
">"				return (int)Tokens.GT;
[oO][rR]			return (int)Tokens.LOR;
[aA][nN][dD]		return (int)Tokens.LAND;
[tT][rR][uU][eE]		yylval.intval = 1; return (int)Tokens.INT;
[fF][aA][lL][sS][eE]		yylval.intval = 0; return (int)Tokens.INT;

'      sb = new StringBuilder(); BEGIN(str);
     
<str>[\']        { /* saw closing quote - all done */
        BEGIN(INITIAL);
        /* return string constant token type and
        * value to parser
        */
		yylval.strval = sb.ToString();
		return (int)Tokens.STRING;
        }
     
<str>\n        {
        /* error - unterminated string constant */
        /* generate error message */
		throw new Exception("Unterminated string constant: " + sb.ToString());
        }
     
<str>\\[0-7]{1,3} {
        /* octal escape sequence */
        int result;
     
		result = Convert.ToInt32(yytext.Substring(1), 8);
     
        if ( result > 0xff )
                /* error, constant is out-of-bounds */
     
        sb.Append((char)result);
        }
     
<str>\\[0-9]+ {
        /* generate error - bad escape sequence; something
        * like '\48' or '\0777777'
        */
		throw new Exception("Bad escape sequence: " + yytext);
        }
     
<str>\\n  sb.Append('\n');
<str>\\t  sb.Append('\t');
<str>\\r  sb.Append('\r');
<str>\\b  sb.Append('\b');
<str>\\f  sb.Append('\f');
<str>\\\" sb.Append('\"');
     
<str>\\(.|\n)  sb.Append(yytext[1]);
     
<str>[^\\\n\']+        {
		sb.Append(yytext);
        }

