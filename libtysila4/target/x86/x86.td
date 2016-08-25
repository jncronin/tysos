nspace = "libtysila4.target.x86";
assembler = "x86_Assembler";
name = "x86";

x86_def = [
	name = name,
	ptr_type = "int32",
	nspace = nspace,
	assembler = assembler
 ];

next_reg_id = 0;
regs = [ Rstack(), Rcontents(),
	Ri32("eip"), Ri32("eax"), Ri32("ebx"), Ri32("ecx"),
	Ri32("edx"), Ri32("edi"), Ri32("esi"), Ri32("esp"),
	Ri32("ebp"),
	MultiReg("eaxedx", [ eax, edx ]) ];
x86_def.regs = regs;

// Mask of all usable registers in most instructions
IntRegs = stackreg | contentsreg | eaxreg | ebxreg | ecxreg | edxreg | edireg | esireg;
Gprs = eaxreg | ebxreg | ecxreg | edxreg | edireg | esireg;

// Where can cli types go?
x86_def.ct_map = simple_map(nspace, assembler, "ct_map", "init_ctmap", [
	[ vl_stack32, Gprs | stackreg ],
	[ vl_arg32, Gprs | stackreg ],
	[ vl_lv32, stackreg ],
	
]);

intmap(nspace, assembler, name, "insts", "init_instrs", [
	"mov",
	"cmp",
	"set",
	"movsx",
	"br",
	"bcc",
	"call",
	"ret",
	"pop",
	"push"
]);

gprmem = [ vl_stack32, vl_lv32, vl_arg32 ];
gpr = [ vl_stack32, vl_arg32 ];
imm = [ vl_c32, vl_c ];
gprmemimm = [ vl_stack32, vl_lv32, vl_arg32, vl_c32, vl_c ];

multiinstr(name, oc_store, [ gprmem ], [ gpr ], [
	[ "x86_mov", "$d0", "$u0" ]
], [ IntRegs ], [ Gprs ], 0);

multiinstr(name, oc_store, [ gpr ], [ gprmem ], [
	[ "x86_mov", "$d0", "$u0" ]
], [ Gprs ], [ IntRegs ], 0);

multiinstr(name, oc_store, [ imm ], [ gprmem ], [
	[ "x86_mov", "$d0", "$u0" ]
], [ ], [ IntRegs ], 0);

multiinstr(name, oc_store, [ vl_st_lv_arg_32 ], [ vl_st_lv_arg_32, vl_st_lv_arg_32 ],
	[	[ "x86_mov", "$d0", "$u0" ],
		[ "x86_mov", "$d1", "$u0" ]
], [ IntRegs ], [ IntRegs, IntRegs ], 0);

instr(name, oc_nop, [], [], [], [], [], 0);

multiinstr(name, oc_cmp, [ gpr, gprmem ], [ gpr ],
	[	[ "x86_cmp", "$u0", "$u1" ],
		[ "x86_set", "$cc", "$td0" ],
		[ "x86_movsx", "$d0", "$tu0" ]
], [ Gprs, IntRegs ], [ Gprs ], 0);

multiinstr(name, oc_cmp, [ gprmem, gpr ], [ gpr ],
	[	[ "x86_cmp", "$u0", "$u1" ],
		[ "x86_set", "$cc", "$td0" ],
		[ "x86_movsx", "$d0", "$tu0" ]
], [ IntRegs, Gprs ], [ Gprs ], 0);

multiinstr(name, oc_brif, [ gpr, gprmem ], [ ],
	[	[ "x86_cmp", "$u0", "$u1" ],
		[ "x86_bcc", "$cc", "$br1" ]
], [ Gprs, IntRegs ], [ ], 0);

multiinstr(name, oc_brif, [ gprmem, gpr ], [ ],
	[	[ "x86_cmp", "$u0", "$u1" ],
		[ "x86_bcc", "$cc", "$br1" ]
], [ IntRegs, Gprs ], [ ], 0);

multiinstr(name, oc_brif, [ imm, gprmem ], [ ],
	[	[ "x86_cmp", "$u1", "$u0" ],
		[ "x86_bcc", "$icc", "$br1" ]
], [ 0, IntRegs ], [ ], 0);

multiinstr(name, oc_brif, [ gprmem, imm ], [ ],
	[	[ "x86_cmp", "$u0", "$u1" ],
		[ "x86_bcc", "$cc", "$br1" ]
], [ IntRegs, 0 ], [ ], 0);

instr(name, oc_br, [], [],
	[	[ "x86_br", "$br0" ]
], [ IntRegs, IntRegs ], [ IntRegs ], 0);

/* These are used programatically and shouldn't automatically
	be used to lower anything (thus use of oc_null) */
instr(name, oc_null, [], [],
	[	[ "x86_ret" ]
], [], [], 0);
multiinstr(name, oc_null, [ gprmemimm ], [],
	[	[ "x86_pop", "$u0" ]
], [ ], [ ], 0);
multiinstr(name, oc_null, [ gprmemimm ], [],
	[	[ "x86_push", "$u0" ]
], [ ], [ ], 0);


// Calling conventions
// SysV
cc_sysv = cc(nspace, assembler, "sysv",
[
	[ ct_int32, [ stack ] ],
	[ ct_int64, [ stack ] ]
],
eaxreg | ecxreg | edxreg,			// caller preserves
ebxreg | esireg | edireg			// callee preserves
);

cc_ret_sysv = cc(nspace, assembler, "ret_sysv",
[
	[ ct_int32, [ eax ] ],
	[ ct_int64, [ eaxedx ] ],
]);

x86_def.cc = cc_map(nspace, assembler, [
	[ "sysv", cc_sysv ]
]);
x86_def.retcc = retcc_map(nspace, assembler, [
	[ "ret_sysv", cc_ret_sysv ]
]);

target_defs.add(x86_def);
export target_defs;
