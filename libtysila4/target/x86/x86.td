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
	"mov_rm32_r32",
	"mov_r32_rm32",
	"mov_rm32_imm32",
	"mov_r32_lab",
	"mov_lab_r32",
	"mov_r32_rm32sib",
	"mov_r32_rm32disp",
	"mov_r32_rm32sibscaledisp",
	"mov_rm32disp_imm32",
	"mov_rm16disp_imm32",
	"mov_rm8disp_imm32",
	"mov_rm32disp_r32",
	"mov_rm16disp_r32",
	"mov_rm8disp_r32",
	"movzxb_r32_rm32sibscaledisp",
	"movzxw_r32_rm32sibscaledisp",
	"cmp_rm32_r32",
	"cmp_r32_rm32",
	"cmp_rm32_imm32",
	"set_rm32",
	"movsxbd", 	"movsxwd", 	"movzxbd", 	"movzxwd",
	"jmp_rel32",
	"jcc_rel32",
	"call_rel32",
	"call_rm32",
	"ret",
	"pop_r32",
	"pop_rm32",
	"push_r32",
	"push_rm32",
	"push_imm32",
	"add_rm32_imm32",
	"sub_rm32_imm32",
	"add_r32_rm32",
	"sub_r32_rm32",
	"imul_r32_rm32_imm32",
	"lea_r32",
]);

gprmem = [ vl_stack32, vl_lv32, vl_arg32, vl_stack, vl_lv, vl_arg ];
gpr = [ vl_stack32, vl_stack ];
imm = [ vl_c32, vl_c ];
gprmemimm = [ vl_stack32, vl_lv32, vl_arg32, vl_c32, vl_c, vl_stack,
	vl_lv, vl_arg ];
lvs = [ vl_lv32, vl_lv64, vl_lv ];

multiinstr(name, oc_ldloca, [ lvs ], [ gpr ], [
	[ "x86_lea_r32", "$d0", "$u0" ]
], [ contentsreg ], [ Gprs ], 0);

multiinstr(name, oc_store, [ gprmem ], [ gpr ], [
	[ "x86_mov_r32_rm32", "$d0", "$u0" ]
], [ IntRegs ], [ Gprs ], 0);

multiinstr(name, oc_store, [ gpr ], [ gprmem ], [
	[ "x86_mov_rm32_r32", "$d0", "$u0" ]
], [ Gprs ], [ IntRegs ], 0);

multiinstr(name, oc_store, [ gprmem ], [ gprmem ], [
	[ "x86_mov_r32_rm32", "$td0", "$u0" ],
	[ "x86_mov_rm32_r32", "$d0", "$tu0" ]
], [ IntRegs ], [ IntRegs ], 0);

multiinstr(name, oc_store, [ imm ], [ gprmem ], [
	[ "x86_mov_rm32_imm32", "$d0", "$u0" ]
], [ 0 ], [ IntRegs ], 0);

multiinstr(name, oc_store, [ gpr ], [ gprmem, gprmem ],
	[	[ "x86_mov_rm32_r32", "$d0", "$u0" ],
		[ "x86_mov_rm32_r32", "$d1", "$u0" ]
], [ IntRegs ], [ IntRegs, IntRegs ], 0);

instr(name, oc_nop, [], [], [], [], [], 0);

multiinstr(name, oc_cmp, [ gpr, gprmem ], [ gpr ],
	[	[ "x86_cmp_r32_rm32", "$u0", "$u1" ],
		[ "x86_set_rm32", "$cc", "$td0" ],
		[ "x86_movsxbd", "$d0", "$tu0" ]
], [ Gprs, IntRegs ], [ Gprs ], 0);

multiinstr(name, oc_cmp, [ gprmem, gpr ], [ gpr ],
	[	[ "x86_cmp_rm32_r32", "$u0", "$u1" ],
		[ "x86_set_rm32", "$cc", "$td0" ],
		[ "x86_movsxbd", "$d0", "$tu0" ]
], [ IntRegs, Gprs ], [ Gprs ], 0);

multiinstr(name, oc_cmp, [ gprmem, imm ], [ gpr ],
	[	[ "x86_cmp_rm32_imm32", "$u0", "$u1" ],
		[ "x86_set_rm32", "$cc", "$td0" ],
		[ "x86_movsxbd", "$d0", "$tu0" ]
], [ IntRegs, 0 ], [ Gprs ], 0);

multiinstr(name, oc_cmp, [ imm, gprmem ], [ gpr ],
	[	[ "x86_cmp_rm32_imm32", "$u1", "$u0" ],
		[ "x86_set_rm32", "$icc", "$td0" ],
		[ "x86_movsxbd", "$d0", "$tu0" ]
], [ 0, IntRegs ], [ Gprs ], 0);

multiinstr(name, oc_brif, [ gpr, gprmem ], [ ],
	[	[ "x86_cmp_r32_rm32", "$u0", "$u1" ],
		[ "x86_jcc_rel32", "$cc", "$br1" ]
], [ Gprs, IntRegs ], [ ], 0);

multiinstr(name, oc_brif, [ gprmem, gpr ], [ ],
	[	[ "x86_cmp_rm32_r32", "$u0", "$u1" ],
		[ "x86_jcc_rel32", "$cc", "$br1" ]
], [ IntRegs, Gprs ], [ ], 0);

multiinstr(name, oc_brif, [ imm, gprmem ], [ ],
	[	[ "x86_cmp_rm32_imm32", "$u1", "$u0" ],
		[ "x86_jcc_rel32", "$icc", "$br1" ]
], [ 0, IntRegs ], [ ], 0);

multiinstr(name, oc_brif, [ gprmem, imm ], [ ],
	[	[ "x86_cmp_rm32_imm32", "$u0", "$u1" ],
		[ "x86_jcc_rel32", "$cc", "$br1" ]
], [ IntRegs, 0 ], [ ], 0);

multiinstr(name, oc_mul, [ gprmem, imm ], [ gpr ],
	[	[ "x86_imul_r32_rm32_imm32", "$d0", "$u0", "$u1" ],
], [ IntRegs, 0 ], [ Gprs ], 0);

multiinstr(name, oc_ldind, [ gpr, gpr ], [ gpr ],
	[	[ "x86_mov_r32_rm32sib", "$d0", "$u0", "$u1" ],
], [ Gprs, Gprs ], [ Gprs ], 0);

multiinstr(name, oc_ldind, [ gpr, imm ], [ gpr ],
	[	[ "x86_mov_r32_rm32disp", "$d0", "$u0", "$u1" ],
], [ Gprs, 0 ], [ Gprs ], 0);

multiinstr(name, oc_ldind, [ gpr, gpr, imm, imm ], [ gpr ],
	[	[ "x86_mov_r32_rm32sibscaledisp", "$d0", "$u0", "$u1", "$u2", "$u3" ],
], [ Gprs, Gprs, 0, 0 ], [ Gprs ], 0);

multiinstr(name, oc_ldindzb, [ gpr, gpr, imm, imm ], [ gpr ],
	[	[ "x86_movzxb_r32_rm32sibscaledisp", "$d0", "$u0", "$u1", "$u2", "$u3" ],
], [ Gprs, Gprs, 0, 0 ], [ Gprs ], 0);

multiinstr(name, oc_ldindzw, [ gpr, gpr, imm, imm ], [ gpr ],
	[	[ "x86_movzxw_r32_rm32sibscaledisp", "$d0", "$u0", "$u1", "$u2", "$u3" ],
], [ Gprs, Gprs, 0, 0 ], [ Gprs ], 0);


/* These define a temporary internal register so that we can
	handle the times when the destination is not the same as the
	first source operand.  The register allocator should coalesce
	them back together if possible.  The extra parameter to the
	add/sub is to ensure the dead-code eliminator doesn't remove
	the first assign (as its value it never used) */
multiinstr(name, oc_add, [ gprmem, gprmem ], [ gprmem ],
	[	[ "x86_mov_r32_rm32", "$td0", "$u0" ],
		[ "x86_add_r32_rm32", "$td0", "$tu0", "$u1" ],
		[ "x86_mov_rm32_r32", "$d0", "$tu0" ]
], [ IntRegs, IntRegs ], [ IntRegs ], 0);

multiinstr(name, oc_add, [ gprmem, gprmem ], [ gprmem ],
	[	[ "x86_mov_r32_rm32", "$td0", "$u0" ],
		[ "x86_add_r32_rm32", "$td0", "$tu0", "$u1" ],
		[ "x86_mov_rm32_r32", "$d0", "$tu0" ]
], [ IntRegs, IntRegs ], [ IntRegs ], 0);

multiinstr(name, oc_add, [ gprmem, imm ], [ gprmem ],
	[	[ "x86_mov_r32_rm32", "$td0", "$u0" ],
		[ "x86_add_rm32_imm32", "$td0", "$tu0", "$u1" ],
		[ "x86_mov_rm32_r32", "$d0", "$tu0" ]
], [ IntRegs, 0 ], [ IntRegs ], 0);

multiinstr(name, oc_add, [ imm, gprmem ], [ gprmem ],
	[	[ "x86_mov_r32_rm32", "$td0", "$u1" ],
		[ "x86_add_rm32_imm32", "$td0", "$tu0", "$u0" ],
		[ "x86_mov_rm32_r32", "$d0", "$tu0" ]
], [ 0, IntRegs ], [ IntRegs ], 0);


instr(name, oc_br, [], [],
	[	[ "x86_jmp_rel32", "$br0" ]
], [ IntRegs, IntRegs ], [ IntRegs ], 0);

/* These are used programatically and shouldn't automatically
	be used to lower anything (thus use of oc_null) */
instr(name, oc_null, [], [],
	[	[ "x86_ret" ]
], [], [], 0);
multiinstr(name, oc_null, [ gprmem ], [],
	[	[ "x86_pop_rm32", "$u0" ]
], [ ], [ ], 0);
multiinstr(name, oc_null, [ gprmem ], [],
	[	[ "x86_push_rm32", "$u0" ]
], [ ], [ ], 0);


// Calling conventions
// SysV
cc_sysv = cc(nspace, assembler, "sysv",
[
	[ ct_int32, [ stack ] ],
	[ ct_intptr, [ stack ] ],
	[ ct_int64, [ stack ] ],
	[ ct_object, [ stack ] ],
	[ ct_ref, [ stack ] ],
	[ ct_vt, [ stack ] ],
],
eaxreg | ecxreg | edxreg,			// caller preserves
ebxreg | esireg | edireg			// callee preserves
);

cc_ret_sysv = cc(nspace, assembler, "ret_sysv",
[
	[ ct_int32, [ eax ] ],
	[ ct_intptr, [ eax ] ],
	[ ct_object, [ eax ] ],
	[ ct_ref, [ eax ] ],

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
