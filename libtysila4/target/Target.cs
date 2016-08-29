/* D:\tysos\branches\tysila3\libtysila4\target\Target.cs
 * This is an auto-generated file
 * DO NOT EDIT
 * It was generated at 18:10:58 on 29 August 2016
 * from D:\tysos\branches\tysila3\libtysila4\target\Target.td
 * by TableMap (part of tysos: http://www.tysos.org)
 * Please edit the source file, rather than this file, to make any changes
 */

namespace libtysila4.target
{
	partial class Target
	{
		public const int rt_gpr = 0;
		public const int rt_float = 1;
		public const int rt_stack = 2;
		public const int rt_contents = 3;
		public const int rt_multi = 4;
		
		internal static void init_rtmap()
		{
			rt_map[0] = "gpr";
			rt_map[1] = "float";
			rt_map[2] = "stack";
			rt_map[3] = "contents";
			rt_map[4] = "multi";
		}
	}
}

namespace libtysila4.target
{
	partial class Target
	{
		public const int pt_def = 5;
		public const int pt_use = 6;
		public const int pt_cc = 7;
		public const int pt_icc = 8;
		public const int pt_br = 9;
		public const int pt_mc = 10;
		public const int pt_tu = 11;
		public const int pt_td = 12;
		
		internal static void init_pt()
		{
			pt_names[5] = "def";
			pt_names[6] = "use";
			pt_names[7] = "cc";
			pt_names[8] = "icc";
			pt_names[9] = "br";
			pt_names[10] = "mc";
			pt_names[11] = "tu";
			pt_names[12] = "td";
		}
	}
}

namespace libtysila4.target
{
	partial class Generic
	{
		public const int g_phi = 13;
		public const int g_precall = 14;
		public const int g_postcall = 15;
		public const int g_setupstack = 16;
		public const int g_savecalleepreserves = 17;
		public const int g_restorecalleepreserves = 18;
		
		internal static void init_instrs()
		{
			insts[13] = "phi";
			insts[14] = "precall";
			insts[15] = "postcall";
			insts[16] = "setupstack";
			insts[17] = "savecalleepreserves";
			insts[18] = "restorecalleepreserves";
		}
	}
}

namespace libtysila4.target.x86
{
	partial class x86_Assembler
	{
		public System.Collections.Generic.Dictionary<int, int> ct_map = new System.Collections.Generic.Dictionary<int, int>(new libtysila4.GenericEqualityComparer<int>());
		internal void init_ctmap()
		{
			ct_map[0] = 505;
			ct_map[2] = 505;
			ct_map[1] = 1;
		}
	}
}

namespace libtysila4.target.x86
{
	partial class x86_Assembler
	{
		public const int x86_mov_rm32_r32 = 19;
		public const int x86_mov_r32_rm32 = 20;
		public const int x86_mov_rm32_imm32 = 21;
		public const int x86_cmp_rm32_r32 = 22;
		public const int x86_cmp_r32_rm32 = 23;
		public const int x86_cmp_rm32_imm32 = 24;
		public const int x86_set_rm32 = 25;
		public const int x86_movsxbd = 26;
		public const int x86_jmp_rel32 = 27;
		public const int x86_jcc_rel32 = 28;
		public const int x86_call_rel32 = 29;
		public const int x86_ret = 30;
		public const int x86_pop_r32 = 31;
		public const int x86_pop_rm32 = 32;
		public const int x86_push_r32 = 33;
		public const int x86_push_rm32 = 34;
		public const int x86_push_imm32 = 35;
		public const int x86_sub_rm32_imm32 = 36;
		
		internal static void init_instrs()
		{
			insts[19] = "mov_rm32_r32";
			insts[20] = "mov_r32_rm32";
			insts[21] = "mov_rm32_imm32";
			insts[22] = "cmp_rm32_r32";
			insts[23] = "cmp_r32_rm32";
			insts[24] = "cmp_rm32_imm32";
			insts[25] = "set_rm32";
			insts[26] = "movsxbd";
			insts[27] = "jmp_rel32";
			insts[28] = "jcc_rel32";
			insts[29] = "call_rel32";
			insts[30] = "ret";
			insts[31] = "pop_r32";
			insts[32] = "pop_rm32";
			insts[33] = "push_r32";
			insts[34] = "push_rm32";
			insts[35] = "push_imm32";
			insts[36] = "sub_rm32_imm32";
		}
	}
}

namespace libtysila4.target.x86
{
	partial class x86_Assembler
	{
		public static System.Collections.Generic.Dictionary<int, int[]> cc_map_sysv = new System.Collections.Generic.Dictionary<int, int[]>(new libtysila4.GenericEqualityComparer<int>());
		internal static void init_sysv()
		{
			cc_map_sysv[44] = new int[] { 0, };
			cc_map_sysv[45] = new int[] { 0, };
		}
		
		internal const ulong sysv_caller_preserves = 104;
		internal const ulong sysv_callee_preserves = 400;
	}
}

namespace libtysila4.target.x86
{
	partial class x86_Assembler
	{
		public static System.Collections.Generic.Dictionary<int, int[]> cc_map_ret_sysv = new System.Collections.Generic.Dictionary<int, int[]>(new libtysila4.GenericEqualityComparer<int>());
		internal static void init_ret_sysv()
		{
			cc_map_ret_sysv[44] = new int[] { 3, };
			cc_map_ret_sysv[45] = new int[] { 11, };
		}
		
		internal const ulong ret_sysv_caller_preserves = 0;
		internal const ulong ret_sysv_callee_preserves = 0;
	}
}

namespace libtysila4.target.x86
{
	partial class x86_Assembler
	{
		internal void init_cc_caller_preserves_map()
		{
			cc_caller_preserves_map["sysv"] = sysv_caller_preserves;
		}
	}
}

namespace libtysila4.target.x86
{
	partial class x86_Assembler
	{
		internal void init_cc_callee_preserves_map()
		{
			cc_callee_preserves_map["sysv"] = sysv_callee_preserves;
		}
	}
}

namespace libtysila4.target.x86
{
	partial class x86_Assembler
	{
		internal void init_cc_map()
		{
			cc_map["sysv"] = cc_map_sysv;
		}
	}
}

namespace libtysila4.target.x86
{
	partial class x86_Assembler
	{
		internal void init_retcc_map()
		{
			retcc_map["ret_sysv"] = cc_map_ret_sysv;
		}
	}
}

namespace libtysila4.target
{
	public partial class Generic
	{
		public static System.Collections.Generic.Dictionary<int, string> insts = new System.Collections.Generic.Dictionary<int, string>(new libtysila4.GenericEqualityComparer<int>());
	}
	
	public partial class Target
	{
		public static void init_targets()
		{
			libtysila4.target.Generic.init_instrs();
			
			var x86_instrs = new HashTable {
				nbucket = 9,
				nchain = 93,
				bucket = new int[] {
					92, 36, 91, 83, 90, 62, 76, 82, 
					81, 
				},
				chain = new int[] {
					-1, -1, -1, -1, -1, -1, 0, 5, 
					1, 4, 6, 7, 3, -1, 9, 14, 
					10, 11, 2, -1, 12, 8, -1, 18, 
					16, 17, 21, 20, 13, 15, 23, 19, 
					27, 26, 22, 30, 35, 32, 29, 25, 
					34, 31, 28, 38, 39, 40, 41, 42, 
					24, 37, 43, 46, 47, 52, 48, 49, 
					50, 56, 44, 53, 54, 58, 57, 55, 
					60, 59, 51, 61, 63, 66, 67, 64, 
					69, 65, 72, 45, 71, 73, 74, 77, 
					68, 33, 70, 80, 78, 79, 75, 85, 
					84, 86, 87, 88, 89, 
				},
				idx_map = new int[] {
					0, 21, 42, 63, 84, 105, 126, 147, 
					168, 189, 210, 231, 252, 271, 290, 309, 
					328, 347, 366, 397, 428, 459, 490, 521, 
					552, 583, 614, 645, 676, 707, 738, 769, 
					800, 831, 862, 893, 924, 932, 970, 1008, 
					1046, 1084, 1122, 1160, 1198, 1236, 1274, 1312, 
					1350, 1388, 1426, 1464, 1502, 1540, 1578, 1616, 
					1654, 1692, 1730, 1768, 1806, 1844, 1872, 1900, 
					1928, 1956, 1984, 2012, 2040, 2068, 2096, 2124, 
					2152, 2180, 2207, 2234, 2261, 2288, 2315, 2342, 
					2369, 2396, 2423, 2450, 2477, 2504, 2523, 2534, 
					2548, 2562, 2576, 2590, 2604, 
				},
				data = new byte[] {
					5, 19, 1, 0, 1, 0, 1, 3, 10, 20, 5, 0, 6, 0, 1, 129, 
					251, 1, 129, 248, 0, 5, 19, 1, 0, 1, 2, 1, 3, 10, 20, 5, 
					0, 6, 0, 1, 129, 251, 1, 129, 248, 0, 5, 19, 1, 1, 1, 0, 
					1, 3, 10, 20, 5, 0, 6, 0, 1, 129, 251, 1, 129, 248, 0, 5, 
					19, 1, 1, 1, 2, 1, 3, 10, 20, 5, 0, 6, 0, 1, 129, 251, 
					1, 129, 248, 0, 5, 19, 1, 2, 1, 0, 1, 3, 10, 20, 5, 0, 
					6, 0, 1, 129, 251, 1, 129, 248, 0, 5, 19, 1, 2, 1, 2, 1, 
					3, 10, 20, 5, 0, 6, 0, 1, 129, 251, 1, 129, 248, 0, 5, 19, 
					1, 0, 1, 0, 1, 3, 10, 19, 5, 0, 6, 0, 1, 129, 248, 1, 
					129, 251, 0, 5, 19, 1, 0, 1, 1, 1, 3, 10, 19, 5, 0, 6, 
					0, 1, 129, 248, 1, 129, 251, 0, 5, 19, 1, 0, 1, 2, 1, 3, 
					10, 19, 5, 0, 6, 0, 1, 129, 248, 1, 129, 251, 0, 5, 19, 1, 
					2, 1, 0, 1, 3, 10, 19, 5, 0, 6, 0, 1, 129, 248, 1, 129, 
					251, 0, 5, 19, 1, 2, 1, 1, 1, 3, 10, 19, 5, 0, 6, 0, 
					1, 129, 248, 1, 129, 251, 0, 5, 19, 1, 2, 1, 2, 1, 3, 10, 
					19, 5, 0, 6, 0, 1, 129, 248, 1, 129, 251, 0, 5, 19, 1, 6, 
					1, 0, 1, 3, 10, 21, 5, 0, 6, 0, 0, 1, 129, 251, 0, 5, 
					19, 1, 6, 1, 1, 1, 3, 10, 21, 5, 0, 6, 0, 0, 1, 129, 
					251, 0, 5, 19, 1, 6, 1, 2, 1, 3, 10, 21, 5, 0, 6, 0, 
					0, 1, 129, 251, 0, 5, 19, 1, 11, 1, 0, 1, 3, 10, 21, 5, 
					0, 6, 0, 0, 1, 129, 251, 0, 5, 19, 1, 11, 1, 1, 1, 3, 
					10, 21, 5, 0, 6, 0, 0, 1, 129, 251, 0, 5, 19, 1, 11, 1, 
					2, 1, 3, 10, 21, 5, 0, 6, 0, 0, 1, 129, 251, 0, 6, 19, 
					1, 0, 2, 0, 0, 2, 3, 10, 19, 5, 0, 6, 0, 3, 10, 19, 
					5, 1, 6, 0, 1, 129, 251, 2, 129, 251, 129, 251, 0, 6, 19, 1, 
					0, 2, 0, 1, 2, 3, 10, 19, 5, 0, 6, 0, 3, 10, 19, 5, 
					1, 6, 0, 1, 129, 251, 2, 129, 251, 129, 251, 0, 6, 19, 1, 0, 
					2, 0, 2, 2, 3, 10, 19, 5, 0, 6, 0, 3, 10, 19, 5, 1, 
					6, 0, 1, 129, 251, 2, 129, 251, 129, 251, 0, 6, 19, 1, 0, 2, 
					1, 0, 2, 3, 10, 19, 5, 0, 6, 0, 3, 10, 19, 5, 1, 6, 
					0, 1, 129, 251, 2, 129, 251, 129, 251, 0, 6, 19, 1, 0, 2, 1, 
					1, 2, 3, 10, 19, 5, 0, 6, 0, 3, 10, 19, 5, 1, 6, 0, 
					1, 129, 251, 2, 129, 251, 129, 251, 0, 6, 19, 1, 0, 2, 1, 2, 
					2, 3, 10, 19, 5, 0, 6, 0, 3, 10, 19, 5, 1, 6, 0, 1, 
					129, 251, 2, 129, 251, 129, 251, 0, 6, 19, 1, 0, 2, 2, 0, 2, 
					3, 10, 19, 5, 0, 6, 0, 3, 10, 19, 5, 1, 6, 0, 1, 129, 
					251, 2, 129, 251, 129, 251, 0, 6, 19, 1, 0, 2, 2, 1, 2, 3, 
					10, 19, 5, 0, 6, 0, 3, 10, 19, 5, 1, 6, 0, 1, 129, 251, 
					2, 129, 251, 129, 251, 0, 6, 19, 1, 0, 2, 2, 2, 2, 3, 10, 
					19, 5, 0, 6, 0, 3, 10, 19, 5, 1, 6, 0, 1, 129, 251, 2, 
					129, 251, 129, 251, 0, 6, 19, 1, 2, 2, 0, 0, 2, 3, 10, 19, 
					5, 0, 6, 0, 3, 10, 19, 5, 1, 6, 0, 1, 129, 251, 2, 129, 
					251, 129, 251, 0, 6, 19, 1, 2, 2, 0, 1, 2, 3, 10, 19, 5, 
					0, 6, 0, 3, 10, 19, 5, 1, 6, 0, 1, 129, 251, 2, 129, 251, 
					129, 251, 0, 6, 19, 1, 2, 2, 0, 2, 2, 3, 10, 19, 5, 0, 
					6, 0, 3, 10, 19, 5, 1, 6, 0, 1, 129, 251, 2, 129, 251, 129, 
					251, 0, 6, 19, 1, 2, 2, 1, 0, 2, 3, 10, 19, 5, 0, 6, 
					0, 3, 10, 19, 5, 1, 6, 0, 1, 129, 251, 2, 129, 251, 129, 251, 
					0, 6, 19, 1, 2, 2, 1, 1, 2, 3, 10, 19, 5, 0, 6, 0, 
					3, 10, 19, 5, 1, 6, 0, 1, 129, 251, 2, 129, 251, 129, 251, 0, 
					6, 19, 1, 2, 2, 1, 2, 2, 3, 10, 19, 5, 0, 6, 0, 3, 
					10, 19, 5, 1, 6, 0, 1, 129, 251, 2, 129, 251, 129, 251, 0, 6, 
					19, 1, 2, 2, 2, 0, 2, 3, 10, 19, 5, 0, 6, 0, 3, 10, 
					19, 5, 1, 6, 0, 1, 129, 251, 2, 129, 251, 129, 251, 0, 6, 19, 
					1, 2, 2, 2, 1, 2, 3, 10, 19, 5, 0, 6, 0, 3, 10, 19, 
					5, 1, 6, 0, 1, 129, 251, 2, 129, 251, 129, 251, 0, 6, 19, 1, 
					2, 2, 2, 2, 2, 3, 10, 19, 5, 0, 6, 0, 3, 10, 19, 5, 
					1, 6, 0, 1, 129, 251, 2, 129, 251, 129, 251, 0, 3, 25, 0, 0, 
					0, 0, 0, 0, 6, 27, 2, 0, 0, 1, 0, 3, 3, 10, 23, 6, 
					0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 
					0, 2, 129, 248, 129, 251, 1, 129, 248, 0, 6, 27, 2, 0, 0, 1, 
					2, 3, 3, 10, 23, 6, 0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 
					3, 10, 26, 5, 0, 11, 0, 2, 129, 248, 129, 251, 1, 129, 248, 0, 
					6, 27, 2, 0, 1, 1, 0, 3, 3, 10, 23, 6, 0, 6, 1, 3, 
					10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 129, 248, 
					129, 251, 1, 129, 248, 0, 6, 27, 2, 0, 1, 1, 2, 3, 3, 10, 
					23, 6, 0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 
					0, 11, 0, 2, 129, 248, 129, 251, 1, 129, 248, 0, 6, 27, 2, 0, 
					2, 1, 0, 3, 3, 10, 23, 6, 0, 6, 1, 3, 10, 25, 7, 0, 
					12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 129, 248, 129, 251, 1, 129, 
					248, 0, 6, 27, 2, 0, 2, 1, 2, 3, 3, 10, 23, 6, 0, 6, 
					1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 
					129, 248, 129, 251, 1, 129, 248, 0, 6, 27, 2, 2, 0, 1, 0, 3, 
					3, 10, 23, 6, 0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 
					26, 5, 0, 11, 0, 2, 129, 248, 129, 251, 1, 129, 248, 0, 6, 27, 
					2, 2, 0, 1, 2, 3, 3, 10, 23, 6, 0, 6, 1, 3, 10, 25, 
					7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 129, 248, 129, 251, 
					1, 129, 248, 0, 6, 27, 2, 2, 1, 1, 0, 3, 3, 10, 23, 6, 
					0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 
					0, 2, 129, 248, 129, 251, 1, 129, 248, 0, 6, 27, 2, 2, 1, 1, 
					2, 3, 3, 10, 23, 6, 0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 
					3, 10, 26, 5, 0, 11, 0, 2, 129, 248, 129, 251, 1, 129, 248, 0, 
					6, 27, 2, 2, 2, 1, 0, 3, 3, 10, 23, 6, 0, 6, 1, 3, 
					10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 129, 248, 
					129, 251, 1, 129, 248, 0, 6, 27, 2, 2, 2, 1, 2, 3, 3, 10, 
					23, 6, 0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 
					0, 11, 0, 2, 129, 248, 129, 251, 1, 129, 248, 0, 6, 27, 2, 0, 
					0, 1, 0, 3, 3, 10, 22, 6, 0, 6, 1, 3, 10, 25, 7, 0, 
					12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 129, 251, 129, 248, 1, 129, 
					248, 0, 6, 27, 2, 0, 0, 1, 2, 3, 3, 10, 22, 6, 0, 6, 
					1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 
					129, 251, 129, 248, 1, 129, 248, 0, 6, 27, 2, 0, 2, 1, 0, 3, 
					3, 10, 22, 6, 0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 
					26, 5, 0, 11, 0, 2, 129, 251, 129, 248, 1, 129, 248, 0, 6, 27, 
					2, 0, 2, 1, 2, 3, 3, 10, 22, 6, 0, 6, 1, 3, 10, 25, 
					7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 129, 251, 129, 248, 
					1, 129, 248, 0, 6, 27, 2, 1, 0, 1, 0, 3, 3, 10, 22, 6, 
					0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 
					0, 2, 129, 251, 129, 248, 1, 129, 248, 0, 6, 27, 2, 1, 0, 1, 
					2, 3, 3, 10, 22, 6, 0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 
					3, 10, 26, 5, 0, 11, 0, 2, 129, 251, 129, 248, 1, 129, 248, 0, 
					6, 27, 2, 1, 2, 1, 0, 3, 3, 10, 22, 6, 0, 6, 1, 3, 
					10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 129, 251, 
					129, 248, 1, 129, 248, 0, 6, 27, 2, 1, 2, 1, 2, 3, 3, 10, 
					22, 6, 0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 
					0, 11, 0, 2, 129, 251, 129, 248, 1, 129, 248, 0, 6, 27, 2, 2, 
					0, 1, 0, 3, 3, 10, 22, 6, 0, 6, 1, 3, 10, 25, 7, 0, 
					12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 129, 251, 129, 248, 1, 129, 
					248, 0, 6, 27, 2, 2, 0, 1, 2, 3, 3, 10, 22, 6, 0, 6, 
					1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 
					129, 251, 129, 248, 1, 129, 248, 0, 6, 27, 2, 2, 2, 1, 0, 3, 
					3, 10, 22, 6, 0, 6, 1, 3, 10, 25, 7, 0, 12, 0, 3, 10, 
					26, 5, 0, 11, 0, 2, 129, 251, 129, 248, 1, 129, 248, 0, 6, 27, 
					2, 2, 2, 1, 2, 3, 3, 10, 22, 6, 0, 6, 1, 3, 10, 25, 
					7, 0, 12, 0, 3, 10, 26, 5, 0, 11, 0, 2, 129, 251, 129, 248, 
					1, 129, 248, 0, 5, 29, 2, 0, 0, 0, 2, 3, 10, 23, 6, 0, 
					6, 1, 3, 10, 28, 7, 0, 9, 1, 2, 129, 248, 129, 251, 0, 0, 
					5, 29, 2, 0, 1, 0, 2, 3, 10, 23, 6, 0, 6, 1, 3, 10, 
					28, 7, 0, 9, 1, 2, 129, 248, 129, 251, 0, 0, 5, 29, 2, 0, 
					2, 0, 2, 3, 10, 23, 6, 0, 6, 1, 3, 10, 28, 7, 0, 9, 
					1, 2, 129, 248, 129, 251, 0, 0, 5, 29, 2, 2, 0, 0, 2, 3, 
					10, 23, 6, 0, 6, 1, 3, 10, 28, 7, 0, 9, 1, 2, 129, 248, 
					129, 251, 0, 0, 5, 29, 2, 2, 1, 0, 2, 3, 10, 23, 6, 0, 
					6, 1, 3, 10, 28, 7, 0, 9, 1, 2, 129, 248, 129, 251, 0, 0, 
					5, 29, 2, 2, 2, 0, 2, 3, 10, 23, 6, 0, 6, 1, 3, 10, 
					28, 7, 0, 9, 1, 2, 129, 248, 129, 251, 0, 0, 5, 29, 2, 0, 
					0, 0, 2, 3, 10, 22, 6, 0, 6, 1, 3, 10, 28, 7, 0, 9, 
					1, 2, 129, 251, 129, 248, 0, 0, 5, 29, 2, 0, 2, 0, 2, 3, 
					10, 22, 6, 0, 6, 1, 3, 10, 28, 7, 0, 9, 1, 2, 129, 251, 
					129, 248, 0, 0, 5, 29, 2, 1, 0, 0, 2, 3, 10, 22, 6, 0, 
					6, 1, 3, 10, 28, 7, 0, 9, 1, 2, 129, 251, 129, 248, 0, 0, 
					5, 29, 2, 1, 2, 0, 2, 3, 10, 22, 6, 0, 6, 1, 3, 10, 
					28, 7, 0, 9, 1, 2, 129, 251, 129, 248, 0, 0, 5, 29, 2, 2, 
					0, 0, 2, 3, 10, 22, 6, 0, 6, 1, 3, 10, 28, 7, 0, 9, 
					1, 2, 129, 251, 129, 248, 0, 0, 5, 29, 2, 2, 2, 0, 2, 3, 
					10, 22, 6, 0, 6, 1, 3, 10, 28, 7, 0, 9, 1, 2, 129, 251, 
					129, 248, 0, 0, 5, 29, 2, 6, 0, 0, 2, 3, 10, 24, 6, 1, 
					6, 0, 3, 10, 28, 8, 0, 9, 1, 2, 0, 129, 251, 0, 0, 5, 
					29, 2, 6, 1, 0, 2, 3, 10, 24, 6, 1, 6, 0, 3, 10, 28, 
					8, 0, 9, 1, 2, 0, 129, 251, 0, 0, 5, 29, 2, 6, 2, 0, 
					2, 3, 10, 24, 6, 1, 6, 0, 3, 10, 28, 8, 0, 9, 1, 2, 
					0, 129, 251, 0, 0, 5, 29, 2, 11, 0, 0, 2, 3, 10, 24, 6, 
					1, 6, 0, 3, 10, 28, 8, 0, 9, 1, 2, 0, 129, 251, 0, 0, 
					5, 29, 2, 11, 1, 0, 2, 3, 10, 24, 6, 1, 6, 0, 3, 10, 
					28, 8, 0, 9, 1, 2, 0, 129, 251, 0, 0, 5, 29, 2, 11, 2, 
					0, 2, 3, 10, 24, 6, 1, 6, 0, 3, 10, 28, 8, 0, 9, 1, 
					2, 0, 129, 251, 0, 0, 5, 29, 2, 0, 6, 0, 2, 3, 10, 24, 
					6, 0, 6, 1, 3, 10, 28, 7, 0, 9, 1, 2, 129, 251, 0, 0, 
					0, 5, 29, 2, 0, 11, 0, 2, 3, 10, 24, 6, 0, 6, 1, 3, 
					10, 28, 7, 0, 9, 1, 2, 129, 251, 0, 0, 0, 5, 29, 2, 1, 
					6, 0, 2, 3, 10, 24, 6, 0, 6, 1, 3, 10, 28, 7, 0, 9, 
					1, 2, 129, 251, 0, 0, 0, 5, 29, 2, 1, 11, 0, 2, 3, 10, 
					24, 6, 0, 6, 1, 3, 10, 28, 7, 0, 9, 1, 2, 129, 251, 0, 
					0, 0, 5, 29, 2, 2, 6, 0, 2, 3, 10, 24, 6, 0, 6, 1, 
					3, 10, 28, 7, 0, 9, 1, 2, 129, 251, 0, 0, 0, 5, 29, 2, 
					2, 11, 0, 2, 3, 10, 24, 6, 0, 6, 1, 3, 10, 28, 7, 0, 
					9, 1, 2, 129, 251, 0, 0, 0, 3, 28, 0, 0, 1, 2, 10, 27, 
					9, 0, 2, 129, 251, 129, 251, 1, 129, 251, 0, 3, 18, 0, 0, 1, 
					1, 10, 30, 0, 0, 0, 4, 18, 1, 0, 0, 1, 2, 10, 32, 6, 
					0, 0, 0, 0, 4, 18, 1, 1, 0, 1, 2, 10, 32, 6, 0, 0, 
					0, 0, 4, 18, 1, 2, 0, 1, 2, 10, 32, 6, 0, 0, 0, 0, 
					4, 18, 1, 0, 0, 1, 2, 10, 34, 6, 0, 0, 0, 0, 4, 18, 
					1, 1, 0, 1, 2, 10, 34, 6, 0, 0, 0, 0, 4, 18, 1, 2, 
					0, 1, 2, 10, 34, 6, 0, 0, 0, 0, 
				},
			};

			target.x86.x86_Assembler.init_sysv();
			target.x86.x86_Assembler.init_ret_sysv();
			var x86 = new x86.x86_Assembler();
			x86.name = "x86";
			x86.ptype = ir.Opcode.ct_int32;
			x86.instrs = x86_instrs;
			x86.regs = new Target.Reg[12];
			x86.regs[0] = new Target.Reg { name = "stack", id = 0, type = 2, size = 0, mask = 1 };
			x86.r_stack = x86.regs[0];
			x86.regs[1] = new Target.Reg { name = "contents", id = 1, type = 3, size = 0, mask = 2 };
			x86.r_contents = x86.regs[1];
			x86.regs[2] = new Target.Reg { name = "eip", id = 2, type = 0, size = 32, mask = 4 };
			x86.r_eip = x86.regs[2];
			x86.regs[3] = new Target.Reg { name = "eax", id = 3, type = 0, size = 32, mask = 8 };
			x86.r_eax = x86.regs[3];
			x86.regs[4] = new Target.Reg { name = "ebx", id = 4, type = 0, size = 32, mask = 16 };
			x86.r_ebx = x86.regs[4];
			x86.regs[5] = new Target.Reg { name = "ecx", id = 5, type = 0, size = 32, mask = 32 };
			x86.r_ecx = x86.regs[5];
			x86.regs[6] = new Target.Reg { name = "edx", id = 6, type = 0, size = 32, mask = 64 };
			x86.r_edx = x86.regs[6];
			x86.regs[7] = new Target.Reg { name = "edi", id = 7, type = 0, size = 32, mask = 128 };
			x86.r_edi = x86.regs[7];
			x86.regs[8] = new Target.Reg { name = "esi", id = 8, type = 0, size = 32, mask = 256 };
			x86.r_esi = x86.regs[8];
			x86.regs[9] = new Target.Reg { name = "esp", id = 9, type = 0, size = 32, mask = 512 };
			x86.r_esp = x86.regs[9];
			x86.regs[10] = new Target.Reg { name = "ebp", id = 10, type = 0, size = 32, mask = 1024 };
			x86.r_ebp = x86.regs[10];
			x86.regs[11] = new Target.Reg { name = "eaxedx", id = 11, type = 4, size = 32, mask = 72 };
			x86.r_eaxedx = x86.regs[11];
			targets["x86"] = x86;
		}
	}
}

namespace libtysila4.target.x86
{
	public partial class x86_Assembler
	{
		public Target.Reg r_stack;
		public Target.Reg r_contents;
		public Target.Reg r_eip;
		public Target.Reg r_eax;
		public Target.Reg r_ebx;
		public Target.Reg r_ecx;
		public Target.Reg r_edx;
		public Target.Reg r_edi;
		public Target.Reg r_esi;
		public Target.Reg r_esp;
		public Target.Reg r_ebp;
		public Target.Reg r_eaxedx;
		
		void init_ccs()
		{
			init_cc_callee_preserves_map();			init_cc_caller_preserves_map();			init_cc_map();			init_retcc_map();		}
		
		internal x86_Assembler()
		{
			init_ccs();
		}
	}
}

