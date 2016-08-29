/* Output machine definition objects */

target_defs = [];

include "Maps.td";
include "Register.td";
include "Instr.td";
include "cc.td";

// Some generic instructions
intmap("libtysila4.target", "Generic", "g", "insts", "init_instrs", [
	"phi",
	"precall",
	"postcall",
	"setupstack",
	"savecalleepreserves",
	"restorecalleepreserves"
]);

// Include machine specific definitions here
include "x86/x86.td";



/* Dump a target definition */
function dump_target(object target_def)
{
	n = target_def.name;

	dumpblob(n, n + "_instrs");

	
	foreach(cc in target_def.cc)
	{
		print("\t\t\ttarget." + n + "." + n + "_Assembler.init_" + cc[0] + "();\n");
	}
	foreach(cc in target_def.retcc)
	{
		print("\t\t\ttarget." + n + "." + n + "_Assembler.init_" + cc[0] + "();\n");
	}
	print("\t\t\tvar " + n + " = new " + n + "." +
		n + "_Assembler();\n");
	print("\t\t\t" + n + ".name = \"" + n + "\";\n");
	print("\t\t\t" + n + ".ptype = ir.Opcode.ct_" + target_def.ptr_type + ";\n");

	print("\t\t\t" + n + ".instrs = " + n + "_instrs;\n");

	/* registers */
	// first, get max register id
	max_id = -1;
	foreach(reg in target_def.regs)
	{
		if(reg.id > max_id)
		{
			max_id = reg.id;
			export max_id;	
		}
	}
	max_id = max_id + 1;
	print("\t\t\t" + n + ".regs = new Target.Reg[" + max_id + "];\n");
	foreach(reg in target_def.regs)
	{
		print("\t\t\t" + n + ".regs[" + reg.id + "] = new Target.Reg { ");
		print("name = \"" + reg.name + "\", id = " + reg.id + ", ");
		print("type = " + reg.type + ", size = " + reg.size + ", ");
		print("mask = " + reg.mask + " };\n");

		print("\t\t\t" + n + ".r_" + reg.name + " = " + n + ".regs[" + reg.id + "];\n");
	}

	print("\t\t\ttargets[\"" + n + "\"] = " + n + ";\n");
}

print("namespace libtysila4.target\n");
print("{\n");
print("\tpublic partial class Generic\n");
print("\t{\n");
print("\t\tpublic static System.Collections.Generic.Dictionary<int, string> insts = new System.Collections.Generic.Dictionary<int, string>(new libtysila4.GenericEqualityComparer<int>());\n");
print("\t}\n");
print("\t\n");
print("\tpublic partial class Target\n");
print("\t{\n");
print("\t\tpublic static void init_targets()\n");
print("\t\t{\n");
print("\t\t\tlibtysila4.target.Generic.init_instrs();\n");
print("\t\t\t\n");
foreach(target_def in target_defs)
{
	dump_target(target_def);
}



print("\t\t}\n");
print("\t}\n");
print("}\n");
print("\n");

/* define register names so they can be used as target.r_etc */
foreach(target_def in target_defs)
{
	print("namespace " + target_def.nspace + "\n");
	print("{\n");
	print("\tpublic partial class " + target_def.assembler + "\n");
	print("\t{\n");

	foreach(reg in target_def.regs)
	{
		print("\t\tpublic Target.Reg r_" + reg.name + ";\n");
	}

	/* Add an init_cc function */
	print("\t\t\n");
	print("\t\tvoid init_ccs()\n");
	print("\t\t{\n");
	print("\t\t\tinit_cc_callee_preserves_map();");
	print("\t\t\tinit_cc_caller_preserves_map();");
	print("\t\t\tinit_cc_map();");
	print("\t\t\tinit_retcc_map();");
	print("\t\t}\n");

	/* Add constructor */
	print("\t\t\n");
	print("\t\tinternal " + target_def.assembler + "()\n");
	print("\t\t{\n");
	print("\t\t\tinit_ccs();\n");
	print("\t\t}\n");

	print("\t}\n");
	print("}\n");
	print("\n");
}