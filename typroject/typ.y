%namespace typroject

%visibility internal
%start expression
%partial

%token	EQUALS COLON MUL LPAREN RPAREN AMP PLUS MINUS DOLLARS COMMA NEWLINE FUNC ASSIGN NOT NOTEQUAL LEQUAL GEQUAL LBRACE RBRACE AT PERCENT EXISTS HASTRAILINGSLASH
%token	LBRACK RBRACK DOT LT GT LSHIFT RSHIFT SEMICOLON LOR LAND OR AND APPEND ASSIGNIF DCOLON
%token	IF ELSE INCLUDE RULEFOR INPUTS DEPENDS ALWAYS SHELLCMD TYPROJECT SOURCES MKDIR FUNCTION RETURN EXPORT
%token	INTEGER STRING VOID ARRAY OBJECT FUNCREF ANY NULL
%token	FOR FOREACH IN WHILE DO

%left	DOT
%right  DCOLON
	
%union {
		public int intval;
		public string strval;
		public Expression exprval;
		public List<Expression> listval;
	}
	
%token <intval>	INT
%token <strval> STRING LABEL
%type <exprval> expression expr expr2 expr3 expr6 expr7 expr10 expr11 propexpr
%type <listval> arglist
%type <strval>	dottedlabel

%%

expression :		expr							{ $$ = $1; val = $1; }
			;

expr		:	expr2							{ $$ = $1; }
			;

expr2		:	expr3 LOR expr2					{ $$ = new Expression { a = $1, b = $3, op = Tokens.LOR }; }
			|	expr3							{ $$ = $1; }
			;

expr3		:	expr6 LAND expr3				{ $$ = new Expression { a = $1, b = $3, op = Tokens.LAND }; }
			|	expr6							{ $$ = $1; }
			;

expr6		:	expr7 EQUALS expr6				{ $$ = new Expression { a = $1, b = $3, op = Tokens.EQUALS }; }
			|	expr7 NOTEQUAL expr6	{ $$ = new Expression { a = $1, b = $3, op = Tokens.NOTEQUAL }; }
			|	expr7							{ $$ = $1; }
			;

expr7		:	expr10 LT expr7					{ $$ = new Expression { a = $1, b = $3, op = Tokens.LT }; }
			|	expr10 GT expr7					{ $$ = new Expression { a = $1, b = $3, op = Tokens.GT }; }
			|	expr10 LEQUAL expr7				{ $$ = new Expression { a = $1, b = $3, op = Tokens.LEQUAL }; }
			|	expr10 GEQUAL expr7				{ $$ = new Expression { a = $1, b = $3, op = Tokens.GEQUAL }; }
			|	expr10							{ $$ = $1; }
			;

expr10		:	NOT expr10						{ $$ = new Expression { a = $2, b = null, op = Tokens.NOT }; }
			|	expr11							{ $$ = $1; }
			;

expr11		:	STRING							{ $$ = new StringExpression { val = $1 }; }
			|	INT								{ $$ = new IntExpression { val = $1 }; }
			|	LPAREN expr RPAREN				{ $$ = $2; }
			|	DOLLARS LPAREN propexpr RPAREN	{ $$ = new PropertyExpression { val = $3 }; }
			|	AT LPAREN LABEL RPAREN			{ $$ = new ListExpression { val = new LabelExpression { val = $3 } }; }
			|	PERCENT LPAREN LABEL RPAREN		{ $$ = new MetadataExpression { val = new LabelExpression { val = $3 } }; }
			|	EXISTS LPAREN expr RPAREN		{ $$ = new ExistsExpression { val = $3 }; }
			|	HASTRAILINGSLASH LPAREN expr RPAREN	{ $$ = new HasTrailingSlashExpression { val = $3 }; }
			;

propexpr		:	LABEL							{ $$ = new LabelExpression { val = $1 }; }
			|	LABEL LPAREN arglist RPAREN		{ $$ = new LabelExpression { val = $1, arglist = $3 }; }
			|	propexpr DOT propexpr			{ $$ = new LabelDotExpression { val = $3, srcval = $1 }; }
			|	LBRACK dottedlabel RBRACK DCOLON propexpr	{ $$ = new StaticExpression { type = $2, val = $5 }; }
			;

arglist		:	expr								{ $$ = new List<Expression> { $1 }; }
			|	arglist COMMA expr				{ $$ = new List<Expression>($1); $$.Add($3); }
			|									{ $$ = new List<Expression>(); }
			;

dottedlabel	:	LABEL							{ $$ = $1; }
			|	dottedlabel DOT LABEL			{ $$ = $1 + "." + $3; }
			;

%%

