%namespace tymakeParse

%visibility internal
%start file
%partial
%using tymake

%token	EQUALS COLON MUL LPAREN RPAREN AMP PLUS MINUS DOLLARS COMMA NEWLINE FUNC ASSIGN NOT NOTEQUAL LEQUAL GEQUAL LBRACE RBRACE
%token	LBRACK RBRACK DOT LT GT SEMICOLON LOR LAND OR AND APPEND ASSIGNIF
%token	IF ELSE INCLUDE RULEFOR INPUTS DEPENDS ALWAYS SHELLCMD TYPROJECT SOURCES MKDIR FUNCTION RETURN EXPORT
%token	ISDIR ISFILE DEFINED BUILD
%token	INTEGER STRING VOID
%token	FOR FOREACH IN WHILE DO

%left	DOT
	
%union {
		public int intval;
		public string strval;
		public Statement stmtval;
		public Expression exprval;
		public List<Expression> exprlist;
		public tymakeParse.Tokens tokval;
		public Expression.EvalResult.ResultType typeval;
		public FunctionStatement.FunctionArg argval;
		public List<FunctionStatement.FunctionArg> arglistval;
		public List<ObjDef> objdeflist;
		public ObjDef objdefval;
		public bool bval;
	}
	
%token <intval>	INT
%token <strval> STRING LABEL

%type <exprval> expr expr2 expr3 expr4 expr5 expr6 expr7 expr8 expr9 expr10 expr11 strlabelexpr depends funccall labelexpr labelexpr2
%type <stmtval> stmtblock stmtlist stmt stmt2 define ifblock makerule cmd include strlabel funcdef forblock foreachblock whileblock doblock
%type <exprlist> dependsblock dependslist exprlist dependsstmt inputsstmt arrayexpr
%type <tokval> assignop
%type <argval> arg
%type <typeval> argtype
%type <arglistval> arglist
%type <bval> export
%type <objdefval> objmember
%type <objdeflist> objlist objexpr

%%

file		:									{ output = new StatementList(); }			/* empty */
			|	stmtblock						{ output = $1; }
			|	stmtlist						{ output = $1; }
			;

strlabel	:	STRING							{ $$ = new StringStatement { val = $1 }; }
			|	LABEL							{ $$ = new LabelStatement { val = $1 }; }
			;

strlabelexpr:	STRING							{ $$ = new StringExpression { val = $1 }; }
			|	labelexpr						{ $$ = $1; }
			;

labelexpr	:	labelexpr2						{ $$ = $1; }
			|	strlabelexpr DOT labelexpr2		{ $$ = new LabelMemberExpression { label = $1, member = $3 }; }
			;

labelexpr2	:	LABEL							{ $$ = new LabelExpression { val = $1 }; }
			|	funccall						{ $$ = $1; }
			;

stmtblock	:	LBRACE stmtlist RBRACE			{ $$ = $2; }
			;

stmtlist	:	stmt							{ StatementList sl = new StatementList(); sl.list = new List<Statement>(); sl.list.Add($1); $$ = sl; }
			|	stmtlist stmt					{ ((StatementList)$1).list.Add($2); $$ = $1; }
			;

stmt		:	stmt2							{ $$ = $1; }
			;

stmt2		:	define SEMICOLON				{ $$ = $1; }
			|	ifblock							{ $$ = $1; }
			|	forblock						{ $$ = $1; }
			|	foreachblock					{ $$ = $1; }
			|	whileblock						{ $$ = $1; }
			|	doblock							{ $$ = $1; }
			|	export makerule					{ $$ = $2; $2.export = $1; }
			|	export funcdef					{ $$ = $2; $2.export = $1; }
			|	cmd	SEMICOLON					{ $$ = $1; }
			|	include	SEMICOLON				{ $$ = $1; }
			;

define		:	LABEL assignop expr				{ $$ = new DefineExprStatement { tok_name = $1, assignop = $2, val = $3 }; }
			;

assignop	:	ASSIGN							{ $$ = Tokens.ASSIGN; }
			|	ASSIGNIF						{ $$ = Tokens.ASSIGNIF; }
			|	APPEND							{ $$ = Tokens.APPEND; }
			;

ifblock		:	IF expr stmtblock					{ $$ = new IfBlockStatement { test = $2, if_block = $3, else_block = null }; }
			|	IF expr stmtblock ELSE stmtblock	{ $$ = new IfBlockStatement { test = $2, if_block = $3, else_block = $5 }; }
			|	IF expr stmtblock ELSE ifblock		{ $$ = new IfBlockStatement { test = $2, if_block = $3, else_block = $5 }; }
			;

forblock	:	FOR LPAREN define SEMICOLON expr SEMICOLON define RPAREN stmtblock	{ $$ = new ForBlockStatement { init = $3, test = $5, incr = $7, code = $9 }; }
			;

foreachblock:	FOREACH LPAREN LABEL IN expr RPAREN	stmtblock	{ $$ = new ForEachBlock { val = $3, enumeration = $5, code = $7 }; }
			;

whileblock	:	WHILE LPAREN expr RPAREN stmtblock	{ $$ = new WhileBlock { test = $3, code = $5 }; }
			;

doblock		:	DO stmtblock WHILE LPAREN expr RPAREN	{ $$ = new DoBlock { test = $5, code = $2 }; }
			;

makerule	:	RULEFOR strlabelexpr inputsstmt dependsstmt stmtblock	{ $$ = new MakeRuleStatement { output_file = $2, inputs_list = $3, depend_list = $4, rules = $5 }; }
			;

dependsstmt	:	DEPENDS dependsblock								{ $$ = $2; }
			|	ALWAYS												{ $$ = null; }
			|														{ $$ = new List<Expression>(); }
			;

inputsstmt	:	INPUTS dependsblock									{ $$ = $2; }
			|														{ $$ = new List<Expression>(); }
			;

cmd			:	SHELLCMD LPAREN strlabelexpr RPAREN	{ $$ = new ShellCommandStatement { shell_cmd = $3 }; }
			|	MKDIR LPAREN strlabelexpr RPAREN	{ $$ = new MkDirCommandStatement { dir = $3 }; }
			|	BUILD LPAREN strlabelexpr RPAREN	{ $$ = new BuildCommandStatement { fname = $3 }; }
			|	EXPORT LABEL						{ $$ = new ExportStatement { v = $2 }; }
			|	RETURN expr							{ $$ = new ReturnStatement { v = $2 }; }
			|	RETURN								{ $$ = new ReturnStatement { v = new ResultExpression { e = new Expression.EvalResult() } }; }
			|	funccall							{ $$ = new ExpressionStatement { expr = $1 }; }
			|	strlabelexpr DOT labelexpr2			{ $$ = new ExpressionStatement { expr = new LabelMemberExpression { label = $1, member = $3 } }; }
			;

dependsblock:	LBRACE dependslist RBRACE		{ $$ = $2; }
			;

dependslist	:	depends							{ $$ = new List<Expression> { $1 }; }
			|	dependslist COMMA depends		{ List<Expression> l = new List<Expression>($1); l.Add($3); $$ = l; }
			|									{ $$ = new List<Expression>(); }
			;

exprlist	:	exprlist COMMA expr2			{ $$ = new List<Expression>($1); $$.Add($3); }
			|	expr2							{ $$ = new List<Expression> { $1 }; }
			|									{ $$ = new List<Expression>(); }
			;

depends		:	expr							{ $$ = $1; }
			|	TYPROJECT strlabelexpr			{ $$ = new ProjectDepends { project = $2 }; }
			|	SHELLCMD strlabelexpr			{ $$ = new ShellCmdDepends { shellcmd = $2 }; }
			;


include		:	INCLUDE strlabel				{ $$ = new IncludeStatement { include_file = $2 }; }
			;

expr		:	LPAREN expr2 RPAREN				{ $$ = $2; }
			|	expr2							{ $$ = $1; }
			;

expr2		:	expr3 LOR expr2					{ $$ = new Expression { a = $1, b = $3, op = Tokens.LOR }; }
			|	expr3							{ $$ = $1; }
			;

expr3		:	expr4 LAND expr3				{ $$ = new Expression { a = $1, b = $3, op = Tokens.LAND }; }
			|	expr4							{ $$ = $1; }
			;

expr4		:	expr5 OR expr4					{ $$ = new Expression { a = $1, b = $3, op = Tokens.OR }; }
			|	expr5							{ $$ = $1; }
			;

expr5		:	expr6 AND expr5					{ $$ = new Expression { a = $1, b = $3, op = Tokens.AND }; }
			|	expr6							{ $$ = $1; }
			;

expr6		:	expr7 EQUALS expr6				{ $$ = new Expression { a = $1, b = $3, op = Tokens.EQUALS }; }
			|	expr7 NOTEQUAL expr6			{ $$ = new Expression { a = $1, b = $3, op = Tokens.NOTEQUAL }; }
			|	expr7							{ $$ = $1; }
			;

expr7		:	expr8 LT expr7					{ $$ = new Expression { a = $1, b = $3, op = Tokens.LT }; }
			|	expr8 GT expr7					{ $$ = new Expression { a = $1, b = $3, op = Tokens.GT }; }
			|	expr8 LEQUAL expr7				{ $$ = new Expression { a = $1, b = $3, op = Tokens.LEQUAL }; }
			|	expr8 GEQUAL expr7				{ $$ = new Expression { a = $1, b = $3, op = Tokens.GEQUAL }; }
			|	expr8							{ $$ = $1; }
			;

expr8		:	expr9 PLUS expr8				{ $$ = new Expression { a = $1, b = $3, op = Tokens.PLUS }; }
			|	expr9 MINUS expr8				{ $$ = new Expression { a = $1, b = $3, op = Tokens.MINUS }; }
			|	expr9							{ $$ = $1; }
			;

expr9		:	expr10 MUL expr9				{ $$ = new Expression { a = $1, b = $3, op = Tokens.MUL }; }
			|	expr10							{ $$ = $1; }
			;

expr10		:	NOT expr10						{ $$ = new Expression { a = $2, b = null, op = Tokens.NOT }; }
			|	MINUS expr10					{ $$ = new Expression { a = $2, b = null, op = Tokens.MINUS }; }
			|	expr11							{ $$ = $1; }
			;

expr11		:	strlabelexpr					{ $$ = $1; }
			|	INT								{ $$ = new IntExpression { val = $1 }; }
			|	arrayexpr						{ $$ = new ArrayExpression { val = $1 }; }
			|	objexpr							{ $$ = new ObjExpression { val = $1 }; }
			|	DEFINED LPAREN LABEL RPAREN		{ $$ = new Expression { a = new StringExpression { val = $3 }, op = Tokens.DEFINED }; }
			;

arrayexpr	:	LBRACK exprlist RBRACK			{ $$ = $2; }
			;

objexpr		:	LBRACK objlist RBRACK			{ $$ = $2; }
			;

objlist		:	objmember						{ $$ = new List<ObjDef> { $1 }; }
			|	objlist COMMA objmember			{ $1.Add($3); $$ = $1; }
			;

objmember	:	LABEL ASSIGN expr				{ $$ = new ObjDef { name = $1, val = $3 }; }
			;

funccall	:	LABEL LPAREN exprlist RPAREN	{ $$ = new FuncCall { target = $1, args = $3 }; }
			;

funcdef		:	FUNCTION LABEL LPAREN arglist RPAREN stmtblock	{ $$ = new FunctionStatement { name = $2, args = $4, code = $6 }; }
			;

arglist		:	arglist COMMA arg				{ $$ = new List<FunctionStatement.FunctionArg>($1); $$.Add($3); }
			|	arg								{ $$ = new List<FunctionStatement.FunctionArg>(); $$.Add($1); }
			|									{ $$ = new List<FunctionStatement.FunctionArg>(); }
			;

argtype		:	INTEGER							{ $$ = Expression.EvalResult.ResultType.Int; }
			|	STRING							{ $$ = Expression.EvalResult.ResultType.String; }
			|	VOID							{ $$ = Expression.EvalResult.ResultType.Void; }
			;

arg			:	argtype LABEL					{ $$ = new FunctionStatement.FunctionArg { name = $2, argtype = $1 }; }
			;

export		:	EXPORT							{ $$ = true; }
			|									{ $$ = false; }
			;
			
%%

internal Statement output;