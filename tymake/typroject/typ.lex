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
"["				return (int)Tokens.LBRACK;
"]"				return (int)Tokens.RBRACK;
"::"				return (int)Tokens.DCOLON;
"<"				return (int)Tokens.LT;
">"				return (int)Tokens.GT;
[oO][rR]			return (int)Tokens.LOR;
[aA][nN][dD]		return (int)Tokens.LAND;
[tT][rR][uU][eE]		yylval.intval = 1; return (int)Tokens.INT;
[fF][aA][lL][sS][eE]		yylval.intval = 0; return (int)Tokens.INT;
[eE][xX][iI][sS][tT][sS]		return (int)Tokens.EXISTS;
[hH][aA][sS][tT][rR][aA][iI][lL][iI][nN][gG][sS][lL][aA][sS][hH]		return (int)Tokens.HASTRAILINGSLASH;
"$"			return (int)Tokens.DOLLARS;
"@"			return (int)Tokens.AT;
"%"			return (int)Tokens.PERCENT;
","			return (int)Tokens.COMMA;
"."			return (int)Tokens.DOT;

'      sb = new StringBuilder(); BEGIN(str);
     
<str>'        { /* saw closing quote - all done */
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
     
<str>%[0-9a-fA-F][0-9a-fA-F] {
		/* escape sequence */
		sb.Append((char)Convert.ToInt32(yytext.Substring(1, 2), 16));
		}
     
    
    
<str>\\(.|\n)  sb.Append(yytext[1]);
     
<str>[^\']+        {
		sb.Append(yytext);
        }

[a-zA-Z_][0-9a-zA-Z_]*		{ yylval.strval = yytext; return (int)Tokens.LABEL; }