/* generate a simple function to map cli types to registers */
export function cc(string namespace, string class, string name,
	array map)
{
	print("namespace " + namespace + "\n");
	print("{\n");
	print("\tpartial class " + class + "\n");
	print("\t{\n");
	print("\t\tpublic static System.Collections.Generic.Dictionary<int, int[]> cc_map_" + name + " = ");
	print("new System.Collections.Generic.Dictionary<int, int[]>");
	print("(new libtysila4.GenericEqualityComparer<int>());\n");

	print("\t\tinternal static void init_" + name + "()\n");
	print("\t\t{\n");

	foreach(entry in map)
	{
		print("\t\t\tcc_map_" + name + "[" + entry[0] + "] = new int[] { ");
		foreach(entry2 in entry[1])
		{
			print(entry2.id + ", ");
		}
		print("};\n");
	}

	print("\t\t}\n");

	print("\t}\n");
	print("}\n");
	print("\n");

	return "cc_map_" + name;
}

export function cc_map(string namespace, string class, array entries)
{
	return simple_map(namespace, class, "cc_map", "init_cc_map", entries,
		1, "System.Collections.Generic.Dictionary<int, int[]>");
}

export function retcc_map(string namespace, string class, array entries)
{
	return simple_map(namespace, class, "retcc_map", "init_retcc_map", entries,
		1, "System.Collections.Generic.Dictionary<int, int[]>");
}