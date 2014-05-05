%namespace TIRParse
%using libtysila
%using libtysila.timple
%visibility internal
%start list
%partial

%token	EQUALS COLON MUL LPAREN RPAREN AMP PLUS MINUS DOLLARS COMMA NEWLINE FUNC
%token	EXTERN LBRACK RBRACK DOT LT GT WITH DEF AS
	
%union {
		public int intval;
		public string strval;
		public libtysila.vara varval;
		public libtysila.timple.TreeNode tacval;
		public libtysila.ThreeAddressCode.Op opval;
		public Parser.partial_inst pival;
		public List<vara> arglist;
		public List<string> stringlist;
		public libtysila.Assembler.TypeToCompile ttc;
		public List<libtysila.Assembler.TypeToCompile> ttclist;
		public List<int> intlist;
	}
	
%token <intval>	INT INTLABEL
%token <strval> STRING

%type <varval>	var const
%type <intval>	offset
%type <tacval>	stat label funcdef
%type <opval>	op call_op br_op cmpbr_op
%type <pival>	inst call_inst br_inst carglist cmpbr_inst
%type <arglist>	arglist arglist2
%type <ttclist>	ttclist ttclist2 gen_args
%type <ttc>		type
%type <strval>	module
%type <intlist>	varlist

%token <opval> INVALID
%token <opval> LDCONST_I4
%token <opval> LDCONST_I8
%token <opval> LDCONST_R4
%token <opval> LDCONST_R8
%token <opval> LDCONST_I
%token <opval> ADD_I4
%token <opval> ADD_I8
%token <opval> ADD_R8
%token <opval> ADD_R4
%token <opval> ADD_I
%token <opval> ADD_OVF_I4
%token <opval> ADD_OVF_I8
%token <opval> ADD_OVF_I
%token <opval> ADD_OVF_UN_I4
%token <opval> ADD_OVF_UN_I8
%token <opval> ADD_OVF_UN_I
%token <opval> AND_I4
%token <opval> AND_I8
%token <opval> AND_I
%token <opval> ARGLIST
%token <opval> CMP_I4
%token <opval> CMP_I8
%token <opval> CMP_I
%token <opval> CMP_R8
%token <opval> CMP_R4
%token <opval> CMP_R8_UN
%token <opval> CMP_R4_UN
%token <opval> BR
%token <opval> BEQ
%token <opval> BNE
%token <opval> BG
%token <opval> BGE
%token <opval> BL
%token <opval> BLE
%token <opval> BA
%token <opval> BAE
%token <opval> BB
%token <opval> BBE
%token <opval> BR_EHCLAUSE
%token <opval> THROWEQ
%token <opval> THROWNE
%token <opval> THROW_OVF
%token <opval> THROW_OVF_UN
%token <opval> THROWGE_UN
%token <opval> THROWG_UN
%token <opval> BREAK_
%token <opval> THROW_
%token <opval> LDFTN
%token <opval> CALL_I4
%token <opval> CALL_I8
%token <opval> CALL_I
%token <opval> CALL_R4
%token <opval> CALL_R8
%token <opval> CALL_VOID
%token <opval> CALL_VT
%token <opval> SETEQ
%token <opval> SETNE
%token <opval> SETG
%token <opval> SETGE
%token <opval> SETL
%token <opval> SETLE
%token <opval> SETA
%token <opval> SETAE
%token <opval> SETB
%token <opval> SETBE
%token <opval> EXAMINEF
%token <opval> BRFINITE
%token <opval> CONV_I4_U1ZX
%token <opval> CONV_I4_I1SX
%token <opval> CONV_I4_U2ZX
%token <opval> CONV_I4_I2SX
%token <opval> CONV_I4_I8SX
%token <opval> CONV_I4_U8ZX
%token <opval> CONV_I4_ISX
%token <opval> CONV_I4_UZX
%token <opval> CONV_I8_U1ZX
%token <opval> CONV_I8_I1SX
%token <opval> CONV_I8_U2ZX
%token <opval> CONV_I8_I2SX
%token <opval> CONV_I8_I4SX
%token <opval> CONV_I8_U4ZX
%token <opval> CONV_I8_ISX
%token <opval> CONV_I8_UZX
%token <opval> CONV_I_U1ZX
%token <opval> CONV_I_I1SX
%token <opval> CONV_I_U2ZX
%token <opval> CONV_I_I2SX
%token <opval> CONV_I_I8SX
%token <opval> CONV_I_U8ZX
%token <opval> CONV_I_ISX
%token <opval> CONV_I_UZX
%token <opval> CONV_I_I4SX
%token <opval> CONV_I_U4ZX
%token <opval> CONV_R8_I8
%token <opval> CONV_R8_I4
%token <opval> CONV_R8_I
%token <opval> CONV_I4_R8
%token <opval> CONV_I8_R8
%token <opval> CONV_I_R8
%token <opval> CONV_R4_I8
%token <opval> CONV_R4_I4
%token <opval> CONV_R4_I
%token <opval> CONV_I4_R4
%token <opval> CONV_I8_R4
%token <opval> CONV_I_R4
%token <opval> CONV_R8_R4
%token <opval> CONV_R4_R8
%token <opval> CONV_U4_R8
%token <opval> CONV_U8_R8
%token <opval> CONV_U_R8
%token <opval> MOVSTRING
%token <opval> DIV_I4
%token <opval> DIV_I8
%token <opval> DIV_I
%token <opval> DIV_R8
%token <opval> DIV_R4
%token <opval> DIV_U4
%token <opval> DIV_U8
%token <opval> DIV_U
%token <opval> SETSTRING_VALUE
%token <opval> GETSTRING_VALUE
%token <opval> STORESTRING
%token <opval> JMPMETHOD
%token <opval> LDOBJ_I4
%token <opval> LDOBJ_I8
%token <opval> LDOBJ_R4
%token <opval> LDOBJ_R8
%token <opval> LDOBJ_I
%token <opval> LDOBJ_VT
%token <opval> LDOBJA_EX_I
%token <opval> STOBJ_I4
%token <opval> STOBJ_I8
%token <opval> STOBJ_R4
%token <opval> STOBJ_R8
%token <opval> STOBJ_I
%token <opval> STOBJ_VT
%token <opval> LDARGA
%token <opval> LDLOCA
%token <opval> LDSTRA
%token <opval> LDDATAA
%token <opval> MUL_I4
%token <opval> MUL_I8
%token <opval> MUL_I
%token <opval> MUL_R8
%token <opval> MUL_R4
%token <opval> MUL_OVF_I4
%token <opval> MUL_OVF_I8
%token <opval> MUL_OVF_I
%token <opval> MUL_OVF_UN_I4
%token <opval> MUL_OVF_UN_I8
%token <opval> MUL_OVF_UN_I
%token <opval> MUL_UN_I4
%token <opval> MUL_UN_I8
%token <opval> MUL_UN_I
%token <opval> NEG_I4
%token <opval> NEG_I8
%token <opval> NEG_I
%token <opval> NEG_R8
%token <opval> NEG_R4
%token <opval> NOT_I4
%token <opval> NOT_I8
%token <opval> NOT_I
%token <opval> OR_I4
%token <opval> OR_I8
%token <opval> OR_I
%token <opval> REM_I4
%token <opval> REM_I8
%token <opval> REM_I
%token <opval> REM_R8
%token <opval> REM_R4
%token <opval> REM_UN_I4
%token <opval> REM_UN_I8
%token <opval> REM_UN_I
%token <opval> RET_VOID
%token <opval> RET_I4
%token <opval> RET_I8
%token <opval> RET_I
%token <opval> RET_R8
%token <opval> RET_VT
%token <opval> SHL_I4
%token <opval> SHL_I8
%token <opval> SHL_I
%token <opval> SHR_I4
%token <opval> SHR_I8
%token <opval> SHR_I
%token <opval> SHR_UN_I4
%token <opval> SHR_UN_I8
%token <opval> SHR_UN_I
%token <opval> SUB_I4
%token <opval> SUB_I8
%token <opval> SUB_I
%token <opval> SUB_R8
%token <opval> SUB_R4
%token <opval> SUB_OVF_I
%token <opval> SUB_OVF_UN_I
%token <opval> SWITCH_
%token <opval> XOR_I4
%token <opval> XOR_I8
%token <opval> XOR_I
%token <opval> SIZEOF_
%token <opval> MALLOC
%token <opval> ASSIGN_I4
%token <opval> ASSIGN_I8
%token <opval> ASSIGN_R4
%token <opval> ASSIGN_R8
%token <opval> ASSIGN_I
%token <opval> ASSIGN_VT
%token <opval> ASSIGN_V_I4
%token <opval> ASSIGN_V_I8
%token <opval> ASSIGN_V_I
%token <opval> ASSIGN_TO_VIRTFTNPTR
%token <opval> ASSIGN_FROM_VIRTFTNPTR_PTR
%token <opval> ASSIGN_FROM_VIRTFTNPTR_THISADJUST
%token <opval> ASSIGN_VIRTFTNPTR
%token <opval> LDOBJ_VIRTFTNPTR
%token <opval> LABEL
%token <opval> LOC_LABEL
%token <opval> INSTRUCTION_LABEL
%token <opval> ENTER
%token <opval> NOP
%token <opval> PHI_I
%token <opval> PHI_I4
%token <opval> PHI_I8
%token <opval> PHI_R4
%token <opval> PHI_R8
%token <opval> PHI_VT
%token <opval> PEEK_U1
%token <opval> PEEK_U2
%token <opval> PEEK_U4
%token <opval> PEEK_U8
%token <opval> PEEK_U
%token <opval> PEEK_I1
%token <opval> PEEK_I2
%token <opval> PEEK_R4
%token <opval> PEEK_R8
%token <opval> POKE_U1
%token <opval> POKE_U2
%token <opval> POKE_U4
%token <opval> POKE_U8
%token <opval> POKE_U
%token <opval> POKE_R4
%token <opval> POKE_R8
%token <opval> PORTOUT_U2_U1
%token <opval> PORTOUT_U2_U2
%token <opval> PORTOUT_U2_U4
%token <opval> PORTOUT_U2_U8
%token <opval> PORTOUT_U2_U
%token <opval> PORTIN_U2_U1
%token <opval> PORTIN_U2_U2
%token <opval> PORTIN_U2_U4
%token <opval> PORTIN_U2_U8
%token <opval> PORTIN_U2_U
%token <opval> TRY_ACQUIRE_I8
%token <opval> RELEASE_I8
%token <opval> SQRT_R8
%token <opval> ALLOCA_I4
%token <opval> ALLOCA_I
%token <opval> ZEROMEM
%token <opval> LDCATCHOBJ
%token <opval> LDMETHINFO
%token <opval> ENDFINALLY
%token <opval> LOCALARG
%token <opval> BEQ_I4
%token <opval> BEQ_I8
%token <opval> BEQ_I
%token <opval> BEQ_R8
%token <opval> BEQ_R4
%token <opval> BEQ_R8_UN
%token <opval> BEQ_R4_UN
%token <opval> BNE_I4
%token <opval> BNE_I8
%token <opval> BNE_I
%token <opval> BNE_R8
%token <opval> BNE_R4
%token <opval> BNE_R8_UN
%token <opval> BNE_R4_UN
%token <opval> BG_I4
%token <opval> BG_I8
%token <opval> BG_I
%token <opval> BG_R8
%token <opval> BG_R4
%token <opval> BG_R8_UN
%token <opval> BG_R4_UN
%token <opval> BGE_I4
%token <opval> BGE_I8
%token <opval> BGE_I
%token <opval> BGE_R8
%token <opval> BGE_R4
%token <opval> BGE_R8_UN
%token <opval> BGE_R4_UN
%token <opval> BL_I4
%token <opval> BL_I8
%token <opval> BL_I
%token <opval> BL_R8
%token <opval> BL_R4
%token <opval> BL_R8_UN
%token <opval> BL_R4_UN
%token <opval> BLE_I4
%token <opval> BLE_I8
%token <opval> BLE_I
%token <opval> BLE_R8
%token <opval> BLE_R4
%token <opval> BLE_R8_UN
%token <opval> BLE_R4_UN
%token <opval> BA_I4
%token <opval> BA_I8
%token <opval> BA_I
%token <opval> BA_R8
%token <opval> BA_R4
%token <opval> BA_R8_UN
%token <opval> BA_R4_UN
%token <opval> BAE_I4
%token <opval> BAE_I8
%token <opval> BAE_I
%token <opval> BAE_R8
%token <opval> BAE_R4
%token <opval> BAE_R8_UN
%token <opval> BAE_R4_UN
%token <opval> BB_I4
%token <opval> BB_I8
%token <opval> BB_I
%token <opval> BB_R8
%token <opval> BB_R4
%token <opval> BB_R8_UN
%token <opval> BB_R4_UN
%token <opval> BBE_I4
%token <opval> BBE_I8
%token <opval> BBE_I
%token <opval> BBE_R8
%token <opval> BBE_R4
%token <opval> BBE_R8_UN
%token <opval> BBE_R4_UN
%token <opval> MISC

%%

list		:	/* empty */
			|	list NEWLINE
			|	list stat endline			{ if(!tacs.ContainsKey(cur_func)) tacs[cur_func] = new List<TreeNode>(); tacs[cur_func].Add($2); }
			|	list label stat endline		{ if(!tacs.ContainsKey(cur_func)) tacs[cur_func] = new List<TreeNode>(); tacs[cur_func].Add($2); tacs[cur_func].Add($3); }
			|	list externdef endline
			|	list vardef endline
			;
			
funcdef		:	FUNC STRING					{ cur_func = $2; $$ = new TimpleLabelNode($2); }
			;
			
externdef	:	EXTERN type STRING LPAREN ttclist RPAREN	{ AddExternDef($3, $2, $5, "default"); }
			|	EXTERN type STRING LPAREN ttclist RPAREN WITH STRING	{ AddExternDef($3, $2, $5, $8); }
			;

vardef		:	DEF varlist AS type						{ AddVarDef($2, $4); }
			;

varlist		:	INT									{ $$ = new List<int> { $1 }; }
			|	varlist COMMA INT					{ $$ = new List<int>($1); $$.Add($3); }
			;
			
type		:	module STRING DOT STRING gen_args	{ $$ = InterpretType($1, $2, $4, $5); }
			|	STRING DOT STRING gen_args			{ $$ = InterpretType(cur_module, $1, $3, $4); }
			|	STRING gen_args						{ $$ = InterpretSimpleType(cur_module, cur_nspace, $1, $2); }
			;
			
module		:	LBRACK STRING RBRACK		{ $$ = $2; }
			;
		
gen_args	:										{ $$ = new List<Assembler.TypeToCompile>(); }
			;
			
ttclist		: 								{ $$ = new List<Assembler.TypeToCompile>(); }
			|	ttclist2
			;

ttclist2	:	type						{ $$ = new List<Assembler.TypeToCompile>(); $$.Add($1); }
			|	ttclist2 COMMA type		{ $$ = new List<Assembler.TypeToCompile>($1); $$.Add($3); }
			;
			
endline		:	NEWLINE
			|	EOF
			;
			
stat		:	label				{ $$ = $1; }
			|	var EQUALS var		{ $$ = new TimpleNode(libtysila.Assembler.GetAssignTac($1.DataType), $1, $3, libtysila.vara.Void()); }
			|	var EQUALS inst		{ $$ = new TimpleNode($3.op, $1, var_if_exist(1, $3.args), var_if_exist(2, $3.args)); }
			|	var EQUALS call_inst { $$ = new TimpleCallNode($3.op, $1, $3.call_target, $3.args, GetMethSig($3), GetCallConv($3)); }
			|	inst				{ $$ = new TimpleNode($1.op, libtysila.vara.Void(), var_if_exist(1, $1.args), var_if_exist(2, $1.args)); }
			|	call_inst			{ $$ = new TimpleCallNode($1.op, libtysila.vara.Void(), $1.call_target, $1.args, GetMethSig($1), GetCallConv($1)); }
			|	br_inst				{ $$ = new TimpleBrNode($1.block_target); }
			|	cmpbr_inst			{ $$ = new TimpleBrNode($1.op, $1.block_target, -1, var_if_exist(1, $1.args), var_if_exist(2, $1.args)); }
			|	funcdef				{ $$ = $1; }
			;
			
label		:	INTLABEL			{ $$ = new TimpleLabelNode($1); }
			;
			
var			:	INT					{ $$ = libtysila.vara.Logical($1, GetDataTypeOf($1)); }	
			|	MUL INT				{ $$ = libtysila.vara.ContentsOf($2, libtysila.Assembler.CliType.void_); }
			|	MUL LPAREN INT offset RPAREN	{ $$ = libtysila.vara.ContentsOf($3, $4, libtysila.Assembler.CliType.void_); }
			|	const				{ $$ = $1; }
			|	STRING				{ $$ = libtysila.vara.Label($1); }
			|	AMP INT				{ $$ = libtysila.vara.AddrOf($2); }
			|	AMP STRING			{ $$ = libtysila.vara.Label($2); }
			;
			
offset		:	PLUS DOLLARS INT	{ $$ = $3; }
			|	MINUS DOLLARS INT	{ $$ = 0 - $3; }
			;
		
const		:	DOLLARS INT			{ $$ = libtysila.vara.Const($2); }
			|	DOLLARS MINUS INT	{ $$ = libtysila.vara.Const(- $3); }
			;
			
arglist		:	/* empty */			{ $$ = new List<libtysila.vara>(); }
			|	LPAREN RPAREN		{ $$ = new List<libtysila.vara>(); }
			|	LPAREN arglist2 RPAREN	{ $$ = $2; }
			;
			
arglist2	:	var					{ $$ = new List<libtysila.vara>(); $$.Add($1); }
			|	arglist2 COMMA var	{ $$ = new List<libtysila.vara>($1); $$.Add($3); }
			;
			
carglist	:	LPAREN var COLON RPAREN { $$ = new partial_inst { call_target = $2, args = new List<libtysila.vara>() }; }
			|	LPAREN var COLON arglist2 RPAREN { $$ = new partial_inst { call_target = $2, args = $4 }; }
			;
			
inst		:	op arglist			{ $$ = new partial_inst { op = $1, args = $2 }; }
			;
			
call_inst	:	call_op carglist	{ $$ = new partial_inst { op = $1, args = $2.args, call_target = $2.call_target }; }
			;

br_inst		:	br_op LPAREN STRING RPAREN { $$ = new partial_inst { op = $1, block_target = Int32.Parse($3.Substring(1)) }; }
			;

cmpbr_inst	:	cmpbr_op LPAREN INTLABEL arglist2 RPAREN { $$ = new partial_inst { op = $1, block_target = $3, args = $4 }; }
			;
			
op          :   INVALID { $$ = libtysila.ThreeAddressCode.Op.invalid; }
            |   LDCONST_I4 { $$ = libtysila.ThreeAddressCode.Op.ldconst_i4; }
            |   LDCONST_I8 { $$ = libtysila.ThreeAddressCode.Op.ldconst_i8; }
            |   LDCONST_R4 { $$ = libtysila.ThreeAddressCode.Op.ldconst_r4; }
            |   LDCONST_R8 { $$ = libtysila.ThreeAddressCode.Op.ldconst_r8; }
            |   LDCONST_I { $$ = libtysila.ThreeAddressCode.Op.ldconst_i; }
            |   ADD_I4 { $$ = libtysila.ThreeAddressCode.Op.add_i4; }
            |   ADD_I8 { $$ = libtysila.ThreeAddressCode.Op.add_i8; }
            |   ADD_R8 { $$ = libtysila.ThreeAddressCode.Op.add_r8; }
            |   ADD_R4 { $$ = libtysila.ThreeAddressCode.Op.add_r4; }
            |   ADD_I { $$ = libtysila.ThreeAddressCode.Op.add_i; }
            |   ADD_OVF_I4 { $$ = libtysila.ThreeAddressCode.Op.add_ovf_i4; }
            |   ADD_OVF_I8 { $$ = libtysila.ThreeAddressCode.Op.add_ovf_i8; }
            |   ADD_OVF_I { $$ = libtysila.ThreeAddressCode.Op.add_ovf_i; }
            |   ADD_OVF_UN_I4 { $$ = libtysila.ThreeAddressCode.Op.add_ovf_un_i4; }
            |   ADD_OVF_UN_I8 { $$ = libtysila.ThreeAddressCode.Op.add_ovf_un_i8; }
            |   ADD_OVF_UN_I { $$ = libtysila.ThreeAddressCode.Op.add_ovf_un_i; }
            |   AND_I4 { $$ = libtysila.ThreeAddressCode.Op.and_i4; }
            |   AND_I8 { $$ = libtysila.ThreeAddressCode.Op.and_i8; }
            |   AND_I { $$ = libtysila.ThreeAddressCode.Op.and_i; }
            |   ARGLIST { $$ = libtysila.ThreeAddressCode.Op.arglist; }
            |   CMP_I4 { $$ = libtysila.ThreeAddressCode.Op.cmp_i4; }
            |   CMP_I8 { $$ = libtysila.ThreeAddressCode.Op.cmp_i8; }
            |   CMP_I { $$ = libtysila.ThreeAddressCode.Op.cmp_i; }
            |   CMP_R8 { $$ = libtysila.ThreeAddressCode.Op.cmp_r8; }
            |   CMP_R4 { $$ = libtysila.ThreeAddressCode.Op.cmp_r4; }
            |   CMP_R8_UN { $$ = libtysila.ThreeAddressCode.Op.cmp_r8_un; }
            |   CMP_R4_UN { $$ = libtysila.ThreeAddressCode.Op.cmp_r4_un; }
            |   THROWEQ { $$ = libtysila.ThreeAddressCode.Op.throweq; }
            |   THROWNE { $$ = libtysila.ThreeAddressCode.Op.throwne; }
            |   THROW_OVF { $$ = libtysila.ThreeAddressCode.Op.throw_ovf; }
            |   THROW_OVF_UN { $$ = libtysila.ThreeAddressCode.Op.throw_ovf_un; }
            |   THROWGE_UN { $$ = libtysila.ThreeAddressCode.Op.throwge_un; }
            |   THROWG_UN { $$ = libtysila.ThreeAddressCode.Op.throwg_un; }
            |   BREAK_ { $$ = libtysila.ThreeAddressCode.Op.break_; }
            |   THROW_ { $$ = libtysila.ThreeAddressCode.Op.throw_; }
            |   LDFTN { $$ = libtysila.ThreeAddressCode.Op.ldftn; }
            |   SETEQ { $$ = libtysila.ThreeAddressCode.Op.seteq; }
            |   SETNE { $$ = libtysila.ThreeAddressCode.Op.setne; }
            |   SETG { $$ = libtysila.ThreeAddressCode.Op.setg; }
            |   SETGE { $$ = libtysila.ThreeAddressCode.Op.setge; }
            |   SETL { $$ = libtysila.ThreeAddressCode.Op.setl; }
            |   SETLE { $$ = libtysila.ThreeAddressCode.Op.setle; }
            |   SETA { $$ = libtysila.ThreeAddressCode.Op.seta; }
            |   SETAE { $$ = libtysila.ThreeAddressCode.Op.setae; }
            |   SETB { $$ = libtysila.ThreeAddressCode.Op.setb; }
            |   SETBE { $$ = libtysila.ThreeAddressCode.Op.setbe; }
            |   EXAMINEF { $$ = libtysila.ThreeAddressCode.Op.examinef; }
            |   CONV_I4_U1ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i4_u1zx; }
            |   CONV_I4_I1SX { $$ = libtysila.ThreeAddressCode.Op.conv_i4_i1sx; }
            |   CONV_I4_U2ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i4_u2zx; }
            |   CONV_I4_I2SX { $$ = libtysila.ThreeAddressCode.Op.conv_i4_i2sx; }
            |   CONV_I4_I8SX { $$ = libtysila.ThreeAddressCode.Op.conv_i4_i8sx; }
            |   CONV_I4_U8ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i4_u8zx; }
            |   CONV_I4_ISX { $$ = libtysila.ThreeAddressCode.Op.conv_i4_isx; }
            |   CONV_I4_UZX { $$ = libtysila.ThreeAddressCode.Op.conv_i4_uzx; }
            |   CONV_I8_U1ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i8_u1zx; }
            |   CONV_I8_I1SX { $$ = libtysila.ThreeAddressCode.Op.conv_i8_i1sx; }
            |   CONV_I8_U2ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i8_u2zx; }
            |   CONV_I8_I2SX { $$ = libtysila.ThreeAddressCode.Op.conv_i8_i2sx; }
            |   CONV_I8_I4SX { $$ = libtysila.ThreeAddressCode.Op.conv_i8_i4sx; }
            |   CONV_I8_U4ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i8_u4zx; }
            |   CONV_I8_ISX { $$ = libtysila.ThreeAddressCode.Op.conv_i8_isx; }
            |   CONV_I8_UZX { $$ = libtysila.ThreeAddressCode.Op.conv_i8_uzx; }
            |   CONV_I_U1ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i_u1zx; }
            |   CONV_I_I1SX { $$ = libtysila.ThreeAddressCode.Op.conv_i_i1sx; }
            |   CONV_I_U2ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i_u2zx; }
            |   CONV_I_I2SX { $$ = libtysila.ThreeAddressCode.Op.conv_i_i2sx; }
            |   CONV_I_I8SX { $$ = libtysila.ThreeAddressCode.Op.conv_i_i8sx; }
            |   CONV_I_U8ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i_u8zx; }
            |   CONV_I_ISX { $$ = libtysila.ThreeAddressCode.Op.conv_i_isx; }
            |   CONV_I_UZX { $$ = libtysila.ThreeAddressCode.Op.conv_i_uzx; }
            |   CONV_I_I4SX { $$ = libtysila.ThreeAddressCode.Op.conv_i_i4sx; }
            |   CONV_I_U4ZX { $$ = libtysila.ThreeAddressCode.Op.conv_i_u4zx; }
            |   CONV_R8_I8 { $$ = libtysila.ThreeAddressCode.Op.conv_r8_i8; }
            |   CONV_R8_I4 { $$ = libtysila.ThreeAddressCode.Op.conv_r8_i4; }
            |   CONV_R8_I { $$ = libtysila.ThreeAddressCode.Op.conv_r8_i; }
            |   CONV_I4_R8 { $$ = libtysila.ThreeAddressCode.Op.conv_i4_r8; }
            |   CONV_I8_R8 { $$ = libtysila.ThreeAddressCode.Op.conv_i8_r8; }
            |   CONV_I_R8 { $$ = libtysila.ThreeAddressCode.Op.conv_i_r8; }
            |   CONV_R4_I8 { $$ = libtysila.ThreeAddressCode.Op.conv_r4_i8; }
            |   CONV_R4_I4 { $$ = libtysila.ThreeAddressCode.Op.conv_r4_i4; }
            |   CONV_R4_I { $$ = libtysila.ThreeAddressCode.Op.conv_r4_i; }
            |   CONV_I4_R4 { $$ = libtysila.ThreeAddressCode.Op.conv_i4_r4; }
            |   CONV_I8_R4 { $$ = libtysila.ThreeAddressCode.Op.conv_i8_r4; }
            |   CONV_I_R4 { $$ = libtysila.ThreeAddressCode.Op.conv_i_r4; }
            |   CONV_R8_R4 { $$ = libtysila.ThreeAddressCode.Op.conv_r8_r4; }
            |   CONV_R4_R8 { $$ = libtysila.ThreeAddressCode.Op.conv_r4_r8; }
            |   CONV_U4_R8 { $$ = libtysila.ThreeAddressCode.Op.conv_u4_r8; }
            |   CONV_U8_R8 { $$ = libtysila.ThreeAddressCode.Op.conv_u8_r8; }
            |   CONV_U_R8 { $$ = libtysila.ThreeAddressCode.Op.conv_u_r8; }
            |   MOVSTRING { $$ = libtysila.ThreeAddressCode.Op.movstring; }
            |   DIV_I4 { $$ = libtysila.ThreeAddressCode.Op.div_i4; }
            |   DIV_I8 { $$ = libtysila.ThreeAddressCode.Op.div_i8; }
            |   DIV_I { $$ = libtysila.ThreeAddressCode.Op.div_i; }
            |   DIV_R8 { $$ = libtysila.ThreeAddressCode.Op.div_r8; }
            |   DIV_R4 { $$ = libtysila.ThreeAddressCode.Op.div_r4; }
            |   DIV_U4 { $$ = libtysila.ThreeAddressCode.Op.div_u4; }
            |   DIV_U8 { $$ = libtysila.ThreeAddressCode.Op.div_u8; }
            |   DIV_U { $$ = libtysila.ThreeAddressCode.Op.div_u; }
            |   SETSTRING_VALUE { $$ = libtysila.ThreeAddressCode.Op.setstring_value; }
            |   GETSTRING_VALUE { $$ = libtysila.ThreeAddressCode.Op.getstring_value; }
            |   STORESTRING { $$ = libtysila.ThreeAddressCode.Op.storestring; }
            |   JMPMETHOD { $$ = libtysila.ThreeAddressCode.Op.jmpmethod; }
            |   LDOBJ_I4 { $$ = libtysila.ThreeAddressCode.Op.ldobj_i4; }
            |   LDOBJ_I8 { $$ = libtysila.ThreeAddressCode.Op.ldobj_i8; }
            |   LDOBJ_R4 { $$ = libtysila.ThreeAddressCode.Op.ldobj_r4; }
            |   LDOBJ_R8 { $$ = libtysila.ThreeAddressCode.Op.ldobj_r8; }
            |   LDOBJ_I { $$ = libtysila.ThreeAddressCode.Op.ldobj_i; }
            |   LDOBJ_VT { $$ = libtysila.ThreeAddressCode.Op.ldobj_vt; }
            |   LDOBJA_EX_I { $$ = libtysila.ThreeAddressCode.Op.ldobja_ex_i; }
            |   STOBJ_I4 { $$ = libtysila.ThreeAddressCode.Op.stobj_i4; }
            |   STOBJ_I8 { $$ = libtysila.ThreeAddressCode.Op.stobj_i8; }
            |   STOBJ_R4 { $$ = libtysila.ThreeAddressCode.Op.stobj_r4; }
            |   STOBJ_R8 { $$ = libtysila.ThreeAddressCode.Op.stobj_r8; }
            |   STOBJ_I { $$ = libtysila.ThreeAddressCode.Op.stobj_i; }
            |   STOBJ_VT { $$ = libtysila.ThreeAddressCode.Op.stobj_vt; }
            |   LDARGA { $$ = libtysila.ThreeAddressCode.Op.ldarga; }
            |   LDLOCA { $$ = libtysila.ThreeAddressCode.Op.ldloca; }
            |   LDSTRA { $$ = libtysila.ThreeAddressCode.Op.ldstra; }
            |   LDDATAA { $$ = libtysila.ThreeAddressCode.Op.lddataa; }
            |   MUL_I4 { $$ = libtysila.ThreeAddressCode.Op.mul_i4; }
            |   MUL_I8 { $$ = libtysila.ThreeAddressCode.Op.mul_i8; }
            |   MUL_I { $$ = libtysila.ThreeAddressCode.Op.mul_i; }
            |   MUL_R8 { $$ = libtysila.ThreeAddressCode.Op.mul_r8; }
            |   MUL_R4 { $$ = libtysila.ThreeAddressCode.Op.mul_r4; }
            |   MUL_OVF_I4 { $$ = libtysila.ThreeAddressCode.Op.mul_ovf_i4; }
            |   MUL_OVF_I8 { $$ = libtysila.ThreeAddressCode.Op.mul_ovf_i8; }
            |   MUL_OVF_I { $$ = libtysila.ThreeAddressCode.Op.mul_ovf_i; }
            |   MUL_OVF_UN_I4 { $$ = libtysila.ThreeAddressCode.Op.mul_ovf_un_i4; }
            |   MUL_OVF_UN_I8 { $$ = libtysila.ThreeAddressCode.Op.mul_ovf_un_i8; }
            |   MUL_OVF_UN_I { $$ = libtysila.ThreeAddressCode.Op.mul_ovf_un_i; }
            |   MUL_UN_I4 { $$ = libtysila.ThreeAddressCode.Op.mul_un_i4; }
            |   MUL_UN_I8 { $$ = libtysila.ThreeAddressCode.Op.mul_un_i8; }
            |   MUL_UN_I { $$ = libtysila.ThreeAddressCode.Op.mul_un_i; }
            |   NEG_I4 { $$ = libtysila.ThreeAddressCode.Op.neg_i4; }
            |   NEG_I8 { $$ = libtysila.ThreeAddressCode.Op.neg_i8; }
            |   NEG_I { $$ = libtysila.ThreeAddressCode.Op.neg_i; }
            |   NEG_R8 { $$ = libtysila.ThreeAddressCode.Op.neg_r8; }
            |   NEG_R4 { $$ = libtysila.ThreeAddressCode.Op.neg_r4; }
            |   NOT_I4 { $$ = libtysila.ThreeAddressCode.Op.not_i4; }
            |   NOT_I8 { $$ = libtysila.ThreeAddressCode.Op.not_i8; }
            |   NOT_I { $$ = libtysila.ThreeAddressCode.Op.not_i; }
            |   OR_I4 { $$ = libtysila.ThreeAddressCode.Op.or_i4; }
            |   OR_I8 { $$ = libtysila.ThreeAddressCode.Op.or_i8; }
            |   OR_I { $$ = libtysila.ThreeAddressCode.Op.or_i; }
            |   REM_I4 { $$ = libtysila.ThreeAddressCode.Op.rem_i4; }
            |   REM_I8 { $$ = libtysila.ThreeAddressCode.Op.rem_i8; }
            |   REM_I { $$ = libtysila.ThreeAddressCode.Op.rem_i; }
            |   REM_R8 { $$ = libtysila.ThreeAddressCode.Op.rem_r8; }
            |   REM_R4 { $$ = libtysila.ThreeAddressCode.Op.rem_r4; }
            |   REM_UN_I4 { $$ = libtysila.ThreeAddressCode.Op.rem_un_i4; }
            |   REM_UN_I8 { $$ = libtysila.ThreeAddressCode.Op.rem_un_i8; }
            |   REM_UN_I { $$ = libtysila.ThreeAddressCode.Op.rem_un_i; }
            |   RET_VOID { $$ = libtysila.ThreeAddressCode.Op.ret_void; }
            |   RET_I4 { $$ = libtysila.ThreeAddressCode.Op.ret_i4; }
            |   RET_I8 { $$ = libtysila.ThreeAddressCode.Op.ret_i8; }
            |   RET_I { $$ = libtysila.ThreeAddressCode.Op.ret_i; }
            |   RET_R8 { $$ = libtysila.ThreeAddressCode.Op.ret_r8; }
            |   RET_VT { $$ = libtysila.ThreeAddressCode.Op.ret_vt; }
            |   SHL_I4 { $$ = libtysila.ThreeAddressCode.Op.shl_i4; }
            |   SHL_I8 { $$ = libtysila.ThreeAddressCode.Op.shl_i8; }
            |   SHL_I { $$ = libtysila.ThreeAddressCode.Op.shl_i; }
            |   SHR_I4 { $$ = libtysila.ThreeAddressCode.Op.shr_i4; }
            |   SHR_I8 { $$ = libtysila.ThreeAddressCode.Op.shr_i8; }
            |   SHR_I { $$ = libtysila.ThreeAddressCode.Op.shr_i; }
            |   SHR_UN_I4 { $$ = libtysila.ThreeAddressCode.Op.shr_un_i4; }
            |   SHR_UN_I8 { $$ = libtysila.ThreeAddressCode.Op.shr_un_i8; }
            |   SHR_UN_I { $$ = libtysila.ThreeAddressCode.Op.shr_un_i; }
            |   SUB_I4 { $$ = libtysila.ThreeAddressCode.Op.sub_i4; }
            |   SUB_I8 { $$ = libtysila.ThreeAddressCode.Op.sub_i8; }
            |   SUB_I { $$ = libtysila.ThreeAddressCode.Op.sub_i; }
            |   SUB_R8 { $$ = libtysila.ThreeAddressCode.Op.sub_r8; }
            |   SUB_R4 { $$ = libtysila.ThreeAddressCode.Op.sub_r4; }
            |   SUB_OVF_I { $$ = libtysila.ThreeAddressCode.Op.sub_ovf_i; }
            |   SUB_OVF_UN_I { $$ = libtysila.ThreeAddressCode.Op.sub_ovf_un_i; }
            |   SWITCH_ { $$ = libtysila.ThreeAddressCode.Op.switch_; }
            |   XOR_I4 { $$ = libtysila.ThreeAddressCode.Op.xor_i4; }
            |   XOR_I8 { $$ = libtysila.ThreeAddressCode.Op.xor_i8; }
            |   XOR_I { $$ = libtysila.ThreeAddressCode.Op.xor_i; }
            |   SIZEOF_ { $$ = libtysila.ThreeAddressCode.Op.sizeof_; }
            |   MALLOC { $$ = libtysila.ThreeAddressCode.Op.malloc; }
            |   ASSIGN_I4 { $$ = libtysila.ThreeAddressCode.Op.assign_i4; }
            |   ASSIGN_I8 { $$ = libtysila.ThreeAddressCode.Op.assign_i8; }
            |   ASSIGN_R4 { $$ = libtysila.ThreeAddressCode.Op.assign_r4; }
            |   ASSIGN_R8 { $$ = libtysila.ThreeAddressCode.Op.assign_r8; }
            |   ASSIGN_I { $$ = libtysila.ThreeAddressCode.Op.assign_i; }
            |   ASSIGN_VT { $$ = libtysila.ThreeAddressCode.Op.assign_vt; }
            |   ASSIGN_V_I4 { $$ = libtysila.ThreeAddressCode.Op.assign_v_i4; }
            |   ASSIGN_V_I8 { $$ = libtysila.ThreeAddressCode.Op.assign_v_i8; }
            |   ASSIGN_V_I { $$ = libtysila.ThreeAddressCode.Op.assign_v_i; }
            |   ASSIGN_TO_VIRTFTNPTR { $$ = libtysila.ThreeAddressCode.Op.assign_to_virtftnptr; }
            |   ASSIGN_FROM_VIRTFTNPTR_PTR { $$ = libtysila.ThreeAddressCode.Op.assign_from_virtftnptr_ptr; }
            |   ASSIGN_FROM_VIRTFTNPTR_THISADJUST { $$ = libtysila.ThreeAddressCode.Op.assign_from_virtftnptr_thisadjust; }
            |   ASSIGN_VIRTFTNPTR { $$ = libtysila.ThreeAddressCode.Op.assign_virtftnptr; }
            |   LDOBJ_VIRTFTNPTR { $$ = libtysila.ThreeAddressCode.Op.ldobj_virtftnptr; }
            |   LABEL { $$ = libtysila.ThreeAddressCode.Op.label; }
            |   LOC_LABEL { $$ = libtysila.ThreeAddressCode.Op.loc_label; }
            |   INSTRUCTION_LABEL { $$ = libtysila.ThreeAddressCode.Op.instruction_label; }
            |   ENTER { $$ = libtysila.ThreeAddressCode.Op.enter; }
            |   NOP { $$ = libtysila.ThreeAddressCode.Op.nop; }
            |   PHI_I { $$ = libtysila.ThreeAddressCode.Op.phi_i; }
            |   PHI_I4 { $$ = libtysila.ThreeAddressCode.Op.phi_i4; }
            |   PHI_I8 { $$ = libtysila.ThreeAddressCode.Op.phi_i8; }
            |   PHI_R4 { $$ = libtysila.ThreeAddressCode.Op.phi_r4; }
            |   PHI_R8 { $$ = libtysila.ThreeAddressCode.Op.phi_r8; }
            |   PHI_VT { $$ = libtysila.ThreeAddressCode.Op.phi_vt; }
            |   PEEK_U1 { $$ = libtysila.ThreeAddressCode.Op.peek_u1; }
            |   PEEK_U2 { $$ = libtysila.ThreeAddressCode.Op.peek_u2; }
            |   PEEK_U4 { $$ = libtysila.ThreeAddressCode.Op.peek_u4; }
            |   PEEK_U8 { $$ = libtysila.ThreeAddressCode.Op.peek_u8; }
            |   PEEK_U { $$ = libtysila.ThreeAddressCode.Op.peek_u; }
            |   PEEK_I1 { $$ = libtysila.ThreeAddressCode.Op.peek_i1; }
            |   PEEK_I2 { $$ = libtysila.ThreeAddressCode.Op.peek_i2; }
            |   PEEK_R4 { $$ = libtysila.ThreeAddressCode.Op.peek_r4; }
            |   PEEK_R8 { $$ = libtysila.ThreeAddressCode.Op.peek_r8; }
            |   POKE_U1 { $$ = libtysila.ThreeAddressCode.Op.poke_u1; }
            |   POKE_U2 { $$ = libtysila.ThreeAddressCode.Op.poke_u2; }
            |   POKE_U4 { $$ = libtysila.ThreeAddressCode.Op.poke_u4; }
            |   POKE_U8 { $$ = libtysila.ThreeAddressCode.Op.poke_u8; }
            |   POKE_U { $$ = libtysila.ThreeAddressCode.Op.poke_u; }
            |   POKE_R4 { $$ = libtysila.ThreeAddressCode.Op.poke_r4; }
            |   POKE_R8 { $$ = libtysila.ThreeAddressCode.Op.poke_r8; }
            |   PORTOUT_U2_U1 { $$ = libtysila.ThreeAddressCode.Op.portout_u2_u1; }
            |   PORTOUT_U2_U2 { $$ = libtysila.ThreeAddressCode.Op.portout_u2_u2; }
            |   PORTOUT_U2_U4 { $$ = libtysila.ThreeAddressCode.Op.portout_u2_u4; }
            |   PORTOUT_U2_U8 { $$ = libtysila.ThreeAddressCode.Op.portout_u2_u8; }
            |   PORTOUT_U2_U { $$ = libtysila.ThreeAddressCode.Op.portout_u2_u; }
            |   PORTIN_U2_U1 { $$ = libtysila.ThreeAddressCode.Op.portin_u2_u1; }
            |   PORTIN_U2_U2 { $$ = libtysila.ThreeAddressCode.Op.portin_u2_u2; }
            |   PORTIN_U2_U4 { $$ = libtysila.ThreeAddressCode.Op.portin_u2_u4; }
            |   PORTIN_U2_U8 { $$ = libtysila.ThreeAddressCode.Op.portin_u2_u8; }
            |   PORTIN_U2_U { $$ = libtysila.ThreeAddressCode.Op.portin_u2_u; }
            |   TRY_ACQUIRE_I8 { $$ = libtysila.ThreeAddressCode.Op.try_acquire_i8; }
            |   RELEASE_I8 { $$ = libtysila.ThreeAddressCode.Op.release_i8; }
            |   SQRT_R8 { $$ = libtysila.ThreeAddressCode.Op.sqrt_r8; }
            |   ALLOCA_I4 { $$ = libtysila.ThreeAddressCode.Op.alloca_i4; }
            |   ALLOCA_I { $$ = libtysila.ThreeAddressCode.Op.alloca_i; }
            |   ZEROMEM { $$ = libtysila.ThreeAddressCode.Op.zeromem; }
            |   LDCATCHOBJ { $$ = libtysila.ThreeAddressCode.Op.ldcatchobj; }
            |   LDMETHINFO { $$ = libtysila.ThreeAddressCode.Op.ldmethinfo; }
            |   ENDFINALLY { $$ = libtysila.ThreeAddressCode.Op.endfinally; }
            |   LOCALARG { $$ = libtysila.ThreeAddressCode.Op.localarg; }
            |   MISC { $$ = libtysila.ThreeAddressCode.Op.misc; }
            ;

call_op     :   CALL_I4 { $$ = libtysila.ThreeAddressCode.Op.call_i4; }
            |   CALL_I8 { $$ = libtysila.ThreeAddressCode.Op.call_i8; }
            |   CALL_I { $$ = libtysila.ThreeAddressCode.Op.call_i; }
            |   CALL_R4 { $$ = libtysila.ThreeAddressCode.Op.call_r4; }
            |   CALL_R8 { $$ = libtysila.ThreeAddressCode.Op.call_r8; }
            |   CALL_VOID { $$ = libtysila.ThreeAddressCode.Op.call_void; }
            |   CALL_VT { $$ = libtysila.ThreeAddressCode.Op.call_vt; }
            ;

br_op       :   BR { $$ = libtysila.ThreeAddressCode.Op.br; }
            |   BR_EHCLAUSE { $$ = libtysila.ThreeAddressCode.Op.br_ehclause; }
            |   BRFINITE { $$ = libtysila.ThreeAddressCode.Op.brfinite; }
            ;		

cmpbr_op     :   BEQ_I4 { $$ = libtysila.ThreeAddressCode.Op.beq_i4; }
            |   BEQ_I8 { $$ = libtysila.ThreeAddressCode.Op.beq_i8; }
            |   BEQ_I { $$ = libtysila.ThreeAddressCode.Op.beq_i; }
            |   BEQ_R8 { $$ = libtysila.ThreeAddressCode.Op.beq_r8; }
            |   BEQ_R4 { $$ = libtysila.ThreeAddressCode.Op.beq_r4; }
            |   BEQ_R8_UN { $$ = libtysila.ThreeAddressCode.Op.beq_r8_un; }
            |   BEQ_R4_UN { $$ = libtysila.ThreeAddressCode.Op.beq_r4_un; }
            |   BNE_I4 { $$ = libtysila.ThreeAddressCode.Op.bne_i4; }
            |   BNE_I8 { $$ = libtysila.ThreeAddressCode.Op.bne_i8; }
            |   BNE_I { $$ = libtysila.ThreeAddressCode.Op.bne_i; }
            |   BNE_R8 { $$ = libtysila.ThreeAddressCode.Op.bne_r8; }
            |   BNE_R4 { $$ = libtysila.ThreeAddressCode.Op.bne_r4; }
            |   BNE_R8_UN { $$ = libtysila.ThreeAddressCode.Op.bne_r8_un; }
            |   BNE_R4_UN { $$ = libtysila.ThreeAddressCode.Op.bne_r4_un; }
            |   BG_I4 { $$ = libtysila.ThreeAddressCode.Op.bg_i4; }
            |   BG_I8 { $$ = libtysila.ThreeAddressCode.Op.bg_i8; }
            |   BG_I { $$ = libtysila.ThreeAddressCode.Op.bg_i; }
            |   BG_R8 { $$ = libtysila.ThreeAddressCode.Op.bg_r8; }
            |   BG_R4 { $$ = libtysila.ThreeAddressCode.Op.bg_r4; }
            |   BG_R8_UN { $$ = libtysila.ThreeAddressCode.Op.bg_r8_un; }
            |   BG_R4_UN { $$ = libtysila.ThreeAddressCode.Op.bg_r4_un; }
            |   BGE_I4 { $$ = libtysila.ThreeAddressCode.Op.bge_i4; }
            |   BGE_I8 { $$ = libtysila.ThreeAddressCode.Op.bge_i8; }
            |   BGE_I { $$ = libtysila.ThreeAddressCode.Op.bge_i; }
            |   BGE_R8 { $$ = libtysila.ThreeAddressCode.Op.bge_r8; }
            |   BGE_R4 { $$ = libtysila.ThreeAddressCode.Op.bge_r4; }
            |   BGE_R8_UN { $$ = libtysila.ThreeAddressCode.Op.bge_r8_un; }
            |   BGE_R4_UN { $$ = libtysila.ThreeAddressCode.Op.bge_r4_un; }
            |   BL_I4 { $$ = libtysila.ThreeAddressCode.Op.bl_i4; }
            |   BL_I8 { $$ = libtysila.ThreeAddressCode.Op.bl_i8; }
            |   BL_I { $$ = libtysila.ThreeAddressCode.Op.bl_i; }
            |   BL_R8 { $$ = libtysila.ThreeAddressCode.Op.bl_r8; }
            |   BL_R4 { $$ = libtysila.ThreeAddressCode.Op.bl_r4; }
            |   BL_R8_UN { $$ = libtysila.ThreeAddressCode.Op.bl_r8_un; }
            |   BL_R4_UN { $$ = libtysila.ThreeAddressCode.Op.bl_r4_un; }
            |   BLE_I4 { $$ = libtysila.ThreeAddressCode.Op.ble_i4; }
            |   BLE_I8 { $$ = libtysila.ThreeAddressCode.Op.ble_i8; }
            |   BLE_I { $$ = libtysila.ThreeAddressCode.Op.ble_i; }
            |   BLE_R8 { $$ = libtysila.ThreeAddressCode.Op.ble_r8; }
            |   BLE_R4 { $$ = libtysila.ThreeAddressCode.Op.ble_r4; }
            |   BLE_R8_UN { $$ = libtysila.ThreeAddressCode.Op.ble_r8_un; }
            |   BLE_R4_UN { $$ = libtysila.ThreeAddressCode.Op.ble_r4_un; }
            |   BA_I4 { $$ = libtysila.ThreeAddressCode.Op.ba_i4; }
            |   BA_I8 { $$ = libtysila.ThreeAddressCode.Op.ba_i8; }
            |   BA_I { $$ = libtysila.ThreeAddressCode.Op.ba_i; }
            |   BA_R8 { $$ = libtysila.ThreeAddressCode.Op.ba_r8; }
            |   BA_R4 { $$ = libtysila.ThreeAddressCode.Op.ba_r4; }
            |   BA_R8_UN { $$ = libtysila.ThreeAddressCode.Op.ba_r8_un; }
            |   BA_R4_UN { $$ = libtysila.ThreeAddressCode.Op.ba_r4_un; }
            |   BAE_I4 { $$ = libtysila.ThreeAddressCode.Op.bae_i4; }
            |   BAE_I8 { $$ = libtysila.ThreeAddressCode.Op.bae_i8; }
            |   BAE_I { $$ = libtysila.ThreeAddressCode.Op.bae_i; }
            |   BAE_R8 { $$ = libtysila.ThreeAddressCode.Op.bae_r8; }
            |   BAE_R4 { $$ = libtysila.ThreeAddressCode.Op.bae_r4; }
            |   BAE_R8_UN { $$ = libtysila.ThreeAddressCode.Op.bae_r8_un; }
            |   BAE_R4_UN { $$ = libtysila.ThreeAddressCode.Op.bae_r4_un; }
            |   BB_I4 { $$ = libtysila.ThreeAddressCode.Op.bb_i4; }
            |   BB_I8 { $$ = libtysila.ThreeAddressCode.Op.bb_i8; }
            |   BB_I { $$ = libtysila.ThreeAddressCode.Op.bb_i; }
            |   BB_R8 { $$ = libtysila.ThreeAddressCode.Op.bb_r8; }
            |   BB_R4 { $$ = libtysila.ThreeAddressCode.Op.bb_r4; }
            |   BB_R8_UN { $$ = libtysila.ThreeAddressCode.Op.bb_r8_un; }
            |   BB_R4_UN { $$ = libtysila.ThreeAddressCode.Op.bb_r4_un; }
            |   BBE_I4 { $$ = libtysila.ThreeAddressCode.Op.bbe_i4; }
            |   BBE_I8 { $$ = libtysila.ThreeAddressCode.Op.bbe_i8; }
            |   BBE_I { $$ = libtysila.ThreeAddressCode.Op.bbe_i; }
            |   BBE_R8 { $$ = libtysila.ThreeAddressCode.Op.bbe_r8; }
            |   BBE_R4 { $$ = libtysila.ThreeAddressCode.Op.bbe_r4; }
            |   BBE_R8_UN { $$ = libtysila.ThreeAddressCode.Op.bbe_r8_un; }
            |   BBE_R4_UN { $$ = libtysila.ThreeAddressCode.Op.bbe_r4_un; }
            ;
			
%%

string cur_func = "unnamed_func";
public Dictionary<string, List<libtysila.timple.TreeNode>> tacs = new Dictionary<string, List<libtysila.timple.TreeNode>>();
Dictionary<string, string> callconvs = new Dictionary<string, string>();
Dictionary<string, Signature.Method> msigs = new Dictionary<string, Signature.Method>();
Dictionary<int, libtysila.Assembler.CliType> dts = new Dictionary<int, libtysila.Assembler.CliType>();
string cur_module = "mscorlib";
string cur_nspace = "System";

vara var_if_exist(int arg_no, List<vara> args)
{
	arg_no--;
	if((arg_no >= 0) && (arg_no < args.Count))
		return args[arg_no];
	return vara.Void();
}
