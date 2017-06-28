/* D:\tysos\branches\tysila3\libtysila5\target\Target.cs
 * This is an auto-generated file
 * DO NOT EDIT
 * It was generated at 23:39:56 on 27 June 2017
 * from D:\tysos\branches\tysila3\libtysila5\target\Target.td
 * by TableMap (part of tysos: http://www.tysos.org)
 * Please edit the source file, rather than this file, to make any changes
 */

namespace libtysila5.target
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

namespace libtysila5.target
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

namespace libtysila5.target
{
	partial class Generic
	{
		public const int g_phi = 13;
		public const int g_precall = 14;
		public const int g_postcall = 15;
		public const int g_setupstack = 16;
		public const int g_savecalleepreserves = 17;
		public const int g_restorecalleepreserves = 18;
		public const int g_loadaddress = 19;
		public const int g_mclabel = 20;
		public const int g_label = 21;
		
		internal static void init_instrs()
		{
			insts[13] = "phi";
			insts[14] = "precall";
			insts[15] = "postcall";
			insts[16] = "setupstack";
			insts[17] = "savecalleepreserves";
			insts[18] = "restorecalleepreserves";
			insts[19] = "loadaddress";
			insts[20] = "mclabel";
			insts[21] = "label";
		}
	}
}

namespace libtysila5.target.x86
{
	partial class x86_Assembler
	{
		public const int x86_mov_rm32_r32 = 22;
		public const int x86_mov_r32_rm32 = 23;
		public const int x86_mov_r8_rm8 = 24;
		public const int x86_mov_r16_rm16 = 25;
		public const int x86_mov_rm32_imm32 = 26;
		public const int x86_mov_r32_lab = 27;
		public const int x86_mov_lab_r32 = 28;
		public const int x86_mov_r32_rm32sib = 29;
		public const int x86_mov_r32_rm32disp = 30;
		public const int x86_mov_r32_rm16disp = 31;
		public const int x86_mov_r32_rm8disp = 32;
		public const int x86_mov_r32_rm32sibscaledisp = 33;
		public const int x86_mov_rm32disp_imm32 = 34;
		public const int x86_mov_rm16disp_imm32 = 35;
		public const int x86_mov_rm8disp_imm32 = 36;
		public const int x86_mov_rm32disp_r32 = 37;
		public const int x86_mov_rm16disp_r16 = 38;
		public const int x86_mov_rm8disp_r8 = 39;
		public const int x86_movzxbd_r32_rm8sibscaledisp = 40;
		public const int x86_movzxwd_r32_rm16sibscaledisp = 41;
		public const int x86_movsxbd_r32_rm8sibscaledisp = 42;
		public const int x86_movsxwd_r32_rm16sibscaledisp = 43;
		public const int x86_neg_rm32 = 44;
		public const int x86_not_rm32 = 45;
		public const int x86_cmp_rm32_r32 = 46;
		public const int x86_cmp_r32_rm32 = 47;
		public const int x86_cmp_rm32_imm32 = 48;
		public const int x86_cmp_rm32_imm8 = 49;
		public const int x86_cmp_rm8_imm8 = 50;
		public const int x86_lock_cmpxchg_rm8_r8 = 51;
		public const int x86_pause = 52;
		public const int x86_set_rm32 = 53;
		public const int x86_movsxbd = 54;
		public const int x86_movsxwd = 55;
		public const int x86_movzxbd = 56;
		public const int x86_movzxwd = 57;
		public const int x86_movsxbd_r32_rm8disp = 58;
		public const int x86_movzxbd_r32_rm8disp = 59;
		public const int x86_movsxwd_r32_rm16disp = 60;
		public const int x86_movzxwd_r32_rm16disp = 61;
		public const int x86_jmp_rel32 = 62;
		public const int x86_jcc_rel32 = 63;
		public const int x86_call_rel32 = 64;
		public const int x86_call_rm32 = 65;
		public const int x86_ret = 66;
		public const int x86_pop_r32 = 67;
		public const int x86_pop_rm32 = 68;
		public const int x86_push_r32 = 69;
		public const int x86_push_rm32 = 70;
		public const int x86_push_imm32 = 71;
		public const int x86_add_rm32_imm32 = 72;
		public const int x86_add_rm32_imm8 = 73;
		public const int x86_sub_rm32_imm32 = 74;
		public const int x86_sub_rm32_imm8 = 75;
		public const int x86_add_r32_rm32 = 76;
		public const int x86_add_rm32_r32 = 77;
		public const int x86_sub_r32_rm32 = 78;
		public const int x86_sub_rm32_r32 = 79;
		public const int x86_adc_r32_rm32 = 80;
		public const int x86_adc_rm32_r32 = 81;
		public const int x86_sbb_r32_rm32 = 82;
		public const int x86_sbb_rm32_r32 = 83;
		public const int x86_idiv_rm32 = 84;
		public const int x86_imul_r32_rm32_imm32 = 85;
		public const int x86_imul_r32_rm32 = 86;
		public const int x86_lea_r32 = 87;
		public const int x86_xor_r32_rm32 = 88;
		public const int x86_xor_rm32_r32 = 89;
		public const int x86_and_r32_rm32 = 90;
		public const int x86_and_rm32_r32 = 91;
		public const int x86_or_r32_rm32 = 92;
		public const int x86_or_rm32_r32 = 93;
		public const int x86_sar_rm32_imm8 = 94;
		public const int x86_sal_rm32_cl = 95;
		public const int x86_sar_rm32_cl = 96;
		public const int x86_shr_rm32_cl = 97;
		public const int x86_and_rm32_imm8 = 98;
		public const int x86_and_rm32_imm32 = 99;
		public const int x86_xchg_r32_rm32 = 100;
		public const int x86_xchg_rm32_r32 = 101;
		public const int x86_out_dx_al = 102;
		public const int x86_out_dx_ax = 103;
		public const int x86_out_dx_eax = 104;
		public const int x86_in_al_dx = 105;
		public const int x86_in_ax_dx = 106;
		public const int x86_in_eax_dx = 107;
		public const int x86_int3 = 108;
		public const int x86_movsd_xmm_xmmm64 = 109;
		public const int x86_movsd_xmmm64_xmm = 110;
		public const int x86_movss_xmm_xmmm32 = 111;
		public const int x86_movss_xmmm32_xmm = 112;
		public const int x86_cvtsd2si_r32_xmmm64 = 113;
		public const int x86_cvtsi2sd_xmm_rm32 = 114;
		public const int x86_cvtsd2ss_xmm_xmmm64 = 115;
		public const int x86_cvtss2sd_xmm_xmmm32 = 116;
		public const int x86_addsd_xmm_xmmm64 = 117;
		public const int x86_subsd_xmm_xmmm64 = 118;
		public const int x86_mulsd_xmm_xmmm64 = 119;
		public const int x86_divsd_xmm_xmmm64 = 120;
		public const int x86_comisd_xmm_xmmm64 = 121;
		public const int x86_ucomisd_xmm_xmmm64 = 122;
		public const int x86_cmpsd_xmm_xmmm64_imm8 = 123;
		public const int x86_mov_r64_imm64 = 124;
		public const int x86_mov_rm64_imm32 = 125;
		public const int x86_mov_r64_rm64 = 126;
		public const int x86_mov_rm64_r64 = 127;
		public const int x86_mov_rm64disp_imm32 = 128;
		public const int x86_mov_r64_rm64disp = 129;
		public const int x86_mov_r64_rm32disp = 130;
		public const int x86_mov_r64_rm16disp = 131;
		public const int x86_mov_r64_rm8disp = 132;
		public const int x86_mov_rm64disp_r64 = 133;
		public const int x86_movsxbq_r64_rm8disp = 134;
		public const int x86_movzxbq_r64_rm8disp = 135;
		public const int x86_movsxwq_r64_rm16disp = 136;
		public const int x86_movzxwq_r64_rm16disp = 137;
		public const int x86_cmp_rm64_r64 = 138;
		public const int x86_cmp_r64_rm64 = 139;
		public const int x86_cmp_rm64_imm32 = 140;
		public const int x86_cmp_rm64_imm8 = 141;
		public const int x86_movsxdq_r64_rm64 = 142;
		public const int x86_xor_r64_rm64 = 143;
		public const int x86_xor_rm64_r64 = 144;
		public const int x86_and_r64_rm64 = 145;
		public const int x86_and_rm64_r64 = 146;
		public const int x86_or_r64_rm64 = 147;
		public const int x86_or_rm64_r64 = 148;
		public const int x86_neg_rm64 = 149;
		public const int x86_not_rm64 = 150;
		public const int x86_imul_r64_rm64 = 151;
		public const int x86_idiv_rm64 = 152;
		public const int x86_sal_rm64_cl = 153;
		public const int x86_sar_rm64_cl = 154;
		public const int x86_shr_rm64_cl = 155;
		public const int x86_xchg_r64_rm64 = 156;
		public const int x86_sub_rm64_imm8 = 157;
		public const int x86_sub_rm64_imm32 = 158;
		public const int x86_add_rm64_imm8 = 159;
		public const int x86_add_rm64_imm32 = 160;
		public const int x86_lea_r64 = 161;
		public const int x86_add_r64_rm64 = 162;
		public const int x86_add_rm64_r64 = 163;
		public const int x86_sub_r64_rm64 = 164;
		public const int x86_sub_rm64_r64 = 165;
		public const int x86_adc_r64_rm64 = 166;
		public const int x86_adc_rm64_r64 = 167;
		public const int x86_sbb_r64_rm64 = 168;
		public const int x86_sbb_rm64_r64 = 169;
		public const int x86_cvtsi2sd_xmm_rm64 = 170;
		public const int x86_cvtsd2si_r64_xmmm64 = 171;
		
		internal static void init_instrs()
		{
			insts[22] = "mov_rm32_r32";
			insts[23] = "mov_r32_rm32";
			insts[24] = "mov_r8_rm8";
			insts[25] = "mov_r16_rm16";
			insts[26] = "mov_rm32_imm32";
			insts[27] = "mov_r32_lab";
			insts[28] = "mov_lab_r32";
			insts[29] = "mov_r32_rm32sib";
			insts[30] = "mov_r32_rm32disp";
			insts[31] = "mov_r32_rm16disp";
			insts[32] = "mov_r32_rm8disp";
			insts[33] = "mov_r32_rm32sibscaledisp";
			insts[34] = "mov_rm32disp_imm32";
			insts[35] = "mov_rm16disp_imm32";
			insts[36] = "mov_rm8disp_imm32";
			insts[37] = "mov_rm32disp_r32";
			insts[38] = "mov_rm16disp_r16";
			insts[39] = "mov_rm8disp_r8";
			insts[40] = "movzxbd_r32_rm8sibscaledisp";
			insts[41] = "movzxwd_r32_rm16sibscaledisp";
			insts[42] = "movsxbd_r32_rm8sibscaledisp";
			insts[43] = "movsxwd_r32_rm16sibscaledisp";
			insts[44] = "neg_rm32";
			insts[45] = "not_rm32";
			insts[46] = "cmp_rm32_r32";
			insts[47] = "cmp_r32_rm32";
			insts[48] = "cmp_rm32_imm32";
			insts[49] = "cmp_rm32_imm8";
			insts[50] = "cmp_rm8_imm8";
			insts[51] = "lock_cmpxchg_rm8_r8";
			insts[52] = "pause";
			insts[53] = "set_rm32";
			insts[54] = "movsxbd";
			insts[55] = "movsxwd";
			insts[56] = "movzxbd";
			insts[57] = "movzxwd";
			insts[58] = "movsxbd_r32_rm8disp";
			insts[59] = "movzxbd_r32_rm8disp";
			insts[60] = "movsxwd_r32_rm16disp";
			insts[61] = "movzxwd_r32_rm16disp";
			insts[62] = "jmp_rel32";
			insts[63] = "jcc_rel32";
			insts[64] = "call_rel32";
			insts[65] = "call_rm32";
			insts[66] = "ret";
			insts[67] = "pop_r32";
			insts[68] = "pop_rm32";
			insts[69] = "push_r32";
			insts[70] = "push_rm32";
			insts[71] = "push_imm32";
			insts[72] = "add_rm32_imm32";
			insts[73] = "add_rm32_imm8";
			insts[74] = "sub_rm32_imm32";
			insts[75] = "sub_rm32_imm8";
			insts[76] = "add_r32_rm32";
			insts[77] = "add_rm32_r32";
			insts[78] = "sub_r32_rm32";
			insts[79] = "sub_rm32_r32";
			insts[80] = "adc_r32_rm32";
			insts[81] = "adc_rm32_r32";
			insts[82] = "sbb_r32_rm32";
			insts[83] = "sbb_rm32_r32";
			insts[84] = "idiv_rm32";
			insts[85] = "imul_r32_rm32_imm32";
			insts[86] = "imul_r32_rm32";
			insts[87] = "lea_r32";
			insts[88] = "xor_r32_rm32";
			insts[89] = "xor_rm32_r32";
			insts[90] = "and_r32_rm32";
			insts[91] = "and_rm32_r32";
			insts[92] = "or_r32_rm32";
			insts[93] = "or_rm32_r32";
			insts[94] = "sar_rm32_imm8";
			insts[95] = "sal_rm32_cl";
			insts[96] = "sar_rm32_cl";
			insts[97] = "shr_rm32_cl";
			insts[98] = "and_rm32_imm8";
			insts[99] = "and_rm32_imm32";
			insts[100] = "xchg_r32_rm32";
			insts[101] = "xchg_rm32_r32";
			insts[102] = "out_dx_al";
			insts[103] = "out_dx_ax";
			insts[104] = "out_dx_eax";
			insts[105] = "in_al_dx";
			insts[106] = "in_ax_dx";
			insts[107] = "in_eax_dx";
			insts[108] = "int3";
			insts[109] = "movsd_xmm_xmmm64";
			insts[110] = "movsd_xmmm64_xmm";
			insts[111] = "movss_xmm_xmmm32";
			insts[112] = "movss_xmmm32_xmm";
			insts[113] = "cvtsd2si_r32_xmmm64";
			insts[114] = "cvtsi2sd_xmm_rm32";
			insts[115] = "cvtsd2ss_xmm_xmmm64";
			insts[116] = "cvtss2sd_xmm_xmmm32";
			insts[117] = "addsd_xmm_xmmm64";
			insts[118] = "subsd_xmm_xmmm64";
			insts[119] = "mulsd_xmm_xmmm64";
			insts[120] = "divsd_xmm_xmmm64";
			insts[121] = "comisd_xmm_xmmm64";
			insts[122] = "ucomisd_xmm_xmmm64";
			insts[123] = "cmpsd_xmm_xmmm64_imm8";
			insts[124] = "mov_r64_imm64";
			insts[125] = "mov_rm64_imm32";
			insts[126] = "mov_r64_rm64";
			insts[127] = "mov_rm64_r64";
			insts[128] = "mov_rm64disp_imm32";
			insts[129] = "mov_r64_rm64disp";
			insts[130] = "mov_r64_rm32disp";
			insts[131] = "mov_r64_rm16disp";
			insts[132] = "mov_r64_rm8disp";
			insts[133] = "mov_rm64disp_r64";
			insts[134] = "movsxbq_r64_rm8disp";
			insts[135] = "movzxbq_r64_rm8disp";
			insts[136] = "movsxwq_r64_rm16disp";
			insts[137] = "movzxwq_r64_rm16disp";
			insts[138] = "cmp_rm64_r64";
			insts[139] = "cmp_r64_rm64";
			insts[140] = "cmp_rm64_imm32";
			insts[141] = "cmp_rm64_imm8";
			insts[142] = "movsxdq_r64_rm64";
			insts[143] = "xor_r64_rm64";
			insts[144] = "xor_rm64_r64";
			insts[145] = "and_r64_rm64";
			insts[146] = "and_rm64_r64";
			insts[147] = "or_r64_rm64";
			insts[148] = "or_rm64_r64";
			insts[149] = "neg_rm64";
			insts[150] = "not_rm64";
			insts[151] = "imul_r64_rm64";
			insts[152] = "idiv_rm64";
			insts[153] = "sal_rm64_cl";
			insts[154] = "sar_rm64_cl";
			insts[155] = "shr_rm64_cl";
			insts[156] = "xchg_r64_rm64";
			insts[157] = "sub_rm64_imm8";
			insts[158] = "sub_rm64_imm32";
			insts[159] = "add_rm64_imm8";
			insts[160] = "add_rm64_imm32";
			insts[161] = "lea_r64";
			insts[162] = "add_r64_rm64";
			insts[163] = "add_rm64_r64";
			insts[164] = "sub_r64_rm64";
			insts[165] = "sub_rm64_r64";
			insts[166] = "adc_r64_rm64";
			insts[167] = "adc_rm64_r64";
			insts[168] = "sbb_r64_rm64";
			insts[169] = "sbb_rm64_r64";
			insts[170] = "cvtsi2sd_xmm_rm64";
			insts[171] = "cvtsd2si_r64_xmmm64";
		}
	}
}

namespace libtysila5.target.x86
{
	partial class x86_Assembler
	{
		public static System.Collections.Generic.Dictionary<int, int[]> cc_map_sysv = new System.Collections.Generic.Dictionary<int, int[]>(new libtysila5.GenericEqualityComparer<int>());
		public static System.Collections.Generic.Dictionary<int, int> cc_classmap_sysv = new System.Collections.Generic.Dictionary<int, int>(new libtysila5.GenericEqualityComparer<int>());
		internal static void init_sysv()
		{
			cc_map_sysv[89] = new int[] { 25, };
			cc_map_sysv[91] = new int[] { 25, };
			cc_map_sysv[90] = new int[] { 25, };
			cc_map_sysv[93] = new int[] { 25, };
			cc_map_sysv[94] = new int[] { 25, };
			cc_map_sysv[95] = new int[] { 25, };
			cc_map_sysv[92] = new int[] { 25, };
		}
		
		internal const ulong sysv_caller_preserves = 1664;
		internal const ulong sysv_callee_preserves = 6400;
	}
}

namespace libtysila5.target.x86
{
	partial class x86_Assembler
	{
		public static System.Collections.Generic.Dictionary<int, int[]> cc_map_ret_sysv = new System.Collections.Generic.Dictionary<int, int[]>(new libtysila5.GenericEqualityComparer<int>());
		public static System.Collections.Generic.Dictionary<int, int> cc_classmap_ret_sysv = new System.Collections.Generic.Dictionary<int, int>(new libtysila5.GenericEqualityComparer<int>());
		internal static void init_ret_sysv()
		{
			cc_map_ret_sysv[89] = new int[] { 7, };
			cc_map_ret_sysv[91] = new int[] { 7, };
			cc_map_ret_sysv[93] = new int[] { 7, };
			cc_map_ret_sysv[94] = new int[] { 7, };
			cc_map_ret_sysv[90] = new int[] { 24, };
			cc_map_ret_sysv[92] = new int[] { 15, };
		}
		
		internal const ulong ret_sysv_caller_preserves = 0;
		internal const ulong ret_sysv_callee_preserves = 0;
	}
}

namespace libtysila5.target.x86
{
	partial class x86_Assembler
	{
		internal void init_cc_caller_preserves_map()
		{
			cc_caller_preserves_map["sysv"] = sysv_caller_preserves;
		}
	}
}

namespace libtysila5.target.x86
{
	partial class x86_Assembler
	{
		internal void init_cc_callee_preserves_map()
		{
			cc_callee_preserves_map["sysv"] = sysv_callee_preserves;
		}
	}
}

namespace libtysila5.target.x86
{
	partial class x86_Assembler
	{
		internal void init_cc_classmap()
		{
			cc_classmap["sysv"] = cc_classmap_sysv;
		}
	}
}

namespace libtysila5.target.x86
{
	partial class x86_Assembler
	{
		internal void init_cc_map()
		{
			cc_map["sysv"] = cc_map_sysv;
		}
	}
}

namespace libtysila5.target.x86
{
	partial class x86_Assembler
	{
		internal void init_retcc_classmap()
		{
			retcc_classmap["ret_sysv"] = cc_classmap_ret_sysv;
		}
	}
}

namespace libtysila5.target.x86
{
	partial class x86_Assembler
	{
		internal void init_retcc_map()
		{
			retcc_map["ret_sysv"] = cc_map_ret_sysv;
		}
	}
}

namespace libtysila5.target.x86_64
{
	partial class x86_64_Assembler
	{
		public const int sysvc_MEMORY = 172;
		public const int sysvc_INTEGER = 173;
		public const int sysvc_SSE = 174;
		public const int sysvc_SSEUP = 175;
		public const int sysvc_X87 = 176;
		public const int sysvc_X87UP = 177;
		public const int sysvc_COMPLEX_X87 = 178;
	}
}

namespace libtysila5.target.x86_64
{
	partial class x86_64_Assembler
	{
		public static System.Collections.Generic.Dictionary<int, int[]> cc_map_sysv = new System.Collections.Generic.Dictionary<int, int[]>(new libtysila5.GenericEqualityComparer<int>());
		public static System.Collections.Generic.Dictionary<int, int> cc_classmap_sysv = new System.Collections.Generic.Dictionary<int, int>(new libtysila5.GenericEqualityComparer<int>());
		internal static void init_sysv()
		{
			cc_classmap_sysv[89] = 173;
			cc_classmap_sysv[91] = 173;
			cc_classmap_sysv[90] = 173;
			cc_classmap_sysv[93] = 173;
			cc_classmap_sysv[94] = 173;
			cc_classmap_sysv[92] = 174;
			cc_map_sysv[173] = new int[] { 11, 12, 10, 9, 27, 28, 25, };
			cc_map_sysv[174] = new int[] { 16, 17, 18, 19, 20, 21, 22, 23, 25, };
			cc_map_sysv[172] = new int[] { 25, };
		}
		
		internal const ulong sysv_caller_preserves = 1664;
		internal const ulong sysv_callee_preserves = 6400;
	}
}

namespace libtysila5.target.x86_64
{
	partial class x86_64_Assembler
	{
		public static System.Collections.Generic.Dictionary<int, int[]> cc_map_ret_sysv = new System.Collections.Generic.Dictionary<int, int[]>(new libtysila5.GenericEqualityComparer<int>());
		public static System.Collections.Generic.Dictionary<int, int> cc_classmap_ret_sysv = new System.Collections.Generic.Dictionary<int, int>(new libtysila5.GenericEqualityComparer<int>());
		internal static void init_ret_sysv()
		{
			cc_map_ret_sysv[89] = new int[] { 7, };
			cc_map_ret_sysv[91] = new int[] { 7, };
			cc_map_ret_sysv[93] = new int[] { 7, };
			cc_map_ret_sysv[94] = new int[] { 7, };
			cc_map_ret_sysv[90] = new int[] { 7, };
			cc_map_ret_sysv[92] = new int[] { 16, };
		}
		
		internal const ulong ret_sysv_caller_preserves = 0;
		internal const ulong ret_sysv_callee_preserves = 0;
	}
}

namespace libtysila5.target.x86_64
{
	partial class x86_64_Assembler
	{
		internal void init_cc_caller_preserves_map()
		{
			cc_caller_preserves_map["sysv"] = sysv_caller_preserves;
		}
	}
}

namespace libtysila5.target.x86_64
{
	partial class x86_64_Assembler
	{
		internal void init_cc_callee_preserves_map()
		{
			cc_callee_preserves_map["sysv"] = sysv_callee_preserves;
		}
	}
}

namespace libtysila5.target.x86_64
{
	partial class x86_64_Assembler
	{
		internal void init_cc_classmap()
		{
			cc_classmap["sysv"] = cc_classmap_sysv;
		}
	}
}

namespace libtysila5.target.x86_64
{
	partial class x86_64_Assembler
	{
		internal void init_cc_map()
		{
			cc_map["sysv"] = cc_map_sysv;
		}
	}
}

namespace libtysila5.target.x86_64
{
	partial class x86_64_Assembler
	{
		internal void init_retcc_classmap()
		{
			retcc_classmap["ret_sysv"] = cc_classmap_ret_sysv;
		}
	}
}

namespace libtysila5.target.x86_64
{
	partial class x86_64_Assembler
	{
		internal void init_retcc_map()
		{
			retcc_map["ret_sysv"] = cc_map_ret_sysv;
		}
	}
}

namespace libtysila5.target
{
	public partial class Generic
	{
		public static System.Collections.Generic.Dictionary<int, string> insts = new System.Collections.Generic.Dictionary<int, string>(new libtysila5.GenericEqualityComparer<int>());
	}
	
	public partial class Target
	{
		public static void init_targets()
		{
			libtysila5.target.Generic.init_instrs();
			
			libtysila5.target.x86.x86_Assembler.init_sysv();
			libtysila5.target.x86.x86_Assembler.init_ret_sysv();
			var x86 = new libtysila5.target.x86.x86_Assembler();
			x86.name = "x86";
			x86.ptype = ir.Opcode.ct_int32;
			libtysila5.target.x86.x86_Assembler.registers = new Target.Reg[25];
			x86.regs = libtysila5.target.x86.x86_Assembler.registers;
			x86.regs[4] = new Target.Reg { name = "stack", id = 4, type = 2, size = 0, mask = 16 };
			libtysila5.target.x86.x86_Assembler.r_stack = x86.regs[4];
			x86.regs[5] = new Target.Reg { name = "contents", id = 5, type = 3, size = 0, mask = 32 };
			libtysila5.target.x86.x86_Assembler.r_contents = x86.regs[5];
			x86.regs[6] = new Target.Reg { name = "eip", id = 6, type = 0, size = 4, mask = 64 };
			libtysila5.target.x86.x86_Assembler.r_eip = x86.regs[6];
			x86.regs[7] = new Target.Reg { name = "eax", id = 7, type = 0, size = 4, mask = 128 };
			libtysila5.target.x86.x86_Assembler.r_eax = x86.regs[7];
			x86.regs[8] = new Target.Reg { name = "ebx", id = 8, type = 0, size = 4, mask = 256 };
			libtysila5.target.x86.x86_Assembler.r_ebx = x86.regs[8];
			x86.regs[9] = new Target.Reg { name = "ecx", id = 9, type = 0, size = 4, mask = 512 };
			libtysila5.target.x86.x86_Assembler.r_ecx = x86.regs[9];
			x86.regs[10] = new Target.Reg { name = "edx", id = 10, type = 0, size = 4, mask = 1024 };
			libtysila5.target.x86.x86_Assembler.r_edx = x86.regs[10];
			x86.regs[11] = new Target.Reg { name = "edi", id = 11, type = 0, size = 4, mask = 2048 };
			libtysila5.target.x86.x86_Assembler.r_edi = x86.regs[11];
			x86.regs[12] = new Target.Reg { name = "esi", id = 12, type = 0, size = 4, mask = 4096 };
			libtysila5.target.x86.x86_Assembler.r_esi = x86.regs[12];
			x86.regs[13] = new Target.Reg { name = "esp", id = 13, type = 0, size = 4, mask = 8192 };
			libtysila5.target.x86.x86_Assembler.r_esp = x86.regs[13];
			x86.regs[14] = new Target.Reg { name = "ebp", id = 14, type = 0, size = 4, mask = 16384 };
			libtysila5.target.x86.x86_Assembler.r_ebp = x86.regs[14];
			x86.regs[15] = new Target.Reg { name = "st0", id = 15, type = 1, size = 8, mask = 32768 };
			libtysila5.target.x86.x86_Assembler.r_st0 = x86.regs[15];
			x86.regs[16] = new Target.Reg { name = "xmm0", id = 16, type = 1, size = 8, mask = 65536 };
			libtysila5.target.x86.x86_Assembler.r_xmm0 = x86.regs[16];
			x86.regs[17] = new Target.Reg { name = "xmm1", id = 17, type = 1, size = 8, mask = 131072 };
			libtysila5.target.x86.x86_Assembler.r_xmm1 = x86.regs[17];
			x86.regs[18] = new Target.Reg { name = "xmm2", id = 18, type = 1, size = 8, mask = 262144 };
			libtysila5.target.x86.x86_Assembler.r_xmm2 = x86.regs[18];
			x86.regs[19] = new Target.Reg { name = "xmm3", id = 19, type = 1, size = 8, mask = 524288 };
			libtysila5.target.x86.x86_Assembler.r_xmm3 = x86.regs[19];
			x86.regs[20] = new Target.Reg { name = "xmm4", id = 20, type = 1, size = 8, mask = 1048576 };
			libtysila5.target.x86.x86_Assembler.r_xmm4 = x86.regs[20];
			x86.regs[21] = new Target.Reg { name = "xmm5", id = 21, type = 1, size = 8, mask = 2097152 };
			libtysila5.target.x86.x86_Assembler.r_xmm5 = x86.regs[21];
			x86.regs[22] = new Target.Reg { name = "xmm6", id = 22, type = 1, size = 8, mask = 4194304 };
			libtysila5.target.x86.x86_Assembler.r_xmm6 = x86.regs[22];
			x86.regs[23] = new Target.Reg { name = "xmm7", id = 23, type = 1, size = 8, mask = 8388608 };
			libtysila5.target.x86.x86_Assembler.r_xmm7 = x86.regs[23];
			x86.regs[24] = new Target.Reg { name = "eaxedx", id = 24, type = 4, size = 4, mask = 1152 };
			libtysila5.target.x86.x86_Assembler.r_eaxedx = x86.regs[24];
			targets["x86"] = x86;
			libtysila5.target.x86_64.x86_64_Assembler.init_sysv();
			libtysila5.target.x86_64.x86_64_Assembler.init_ret_sysv();
			var x86_64 = new libtysila5.target.x86_64.x86_64_Assembler();
			x86_64.name = "x86_64";
			x86_64.ptype = ir.Opcode.ct_int64;
			libtysila5.target.x86_64.x86_64_Assembler.registers = new Target.Reg[44];
			x86_64.regs = libtysila5.target.x86_64.x86_64_Assembler.registers;
			x86_64.regs[25] = new Target.Reg { name = "stack", id = 25, type = 2, size = 0, mask = 33554432 };
			libtysila5.target.x86_64.x86_64_Assembler.r_stack = x86_64.regs[25];
			x86_64.regs[26] = new Target.Reg { name = "contents", id = 26, type = 3, size = 0, mask = 67108864 };
			libtysila5.target.x86_64.x86_64_Assembler.r_contents = x86_64.regs[26];
			x86_64.regs[6] = new Target.Reg { name = "rip", id = 6, type = 0, size = 8, mask = 64 };
			libtysila5.target.x86_64.x86_64_Assembler.r_rip = x86_64.regs[6];
			x86_64.regs[7] = new Target.Reg { name = "rax", id = 7, type = 0, size = 8, mask = 128 };
			libtysila5.target.x86_64.x86_64_Assembler.r_rax = x86_64.regs[7];
			x86_64.regs[8] = new Target.Reg { name = "rbx", id = 8, type = 0, size = 8, mask = 256 };
			libtysila5.target.x86_64.x86_64_Assembler.r_rbx = x86_64.regs[8];
			x86_64.regs[9] = new Target.Reg { name = "rcx", id = 9, type = 0, size = 8, mask = 512 };
			libtysila5.target.x86_64.x86_64_Assembler.r_rcx = x86_64.regs[9];
			x86_64.regs[10] = new Target.Reg { name = "rdx", id = 10, type = 0, size = 8, mask = 1024 };
			libtysila5.target.x86_64.x86_64_Assembler.r_rdx = x86_64.regs[10];
			x86_64.regs[12] = new Target.Reg { name = "rsi", id = 12, type = 0, size = 8, mask = 4096 };
			libtysila5.target.x86_64.x86_64_Assembler.r_rsi = x86_64.regs[12];
			x86_64.regs[11] = new Target.Reg { name = "rdi", id = 11, type = 0, size = 8, mask = 2048 };
			libtysila5.target.x86_64.x86_64_Assembler.r_rdi = x86_64.regs[11];
			x86_64.regs[27] = new Target.Reg { name = "r8", id = 27, type = 0, size = 8, mask = 134217728 };
			libtysila5.target.x86_64.x86_64_Assembler.r_r8 = x86_64.regs[27];
			x86_64.regs[28] = new Target.Reg { name = "r9", id = 28, type = 0, size = 8, mask = 268435456 };
			libtysila5.target.x86_64.x86_64_Assembler.r_r9 = x86_64.regs[28];
			x86_64.regs[29] = new Target.Reg { name = "r10", id = 29, type = 0, size = 8, mask = 536870912 };
			libtysila5.target.x86_64.x86_64_Assembler.r_r10 = x86_64.regs[29];
			x86_64.regs[30] = new Target.Reg { name = "r11", id = 30, type = 0, size = 8, mask = 1073741824 };
			libtysila5.target.x86_64.x86_64_Assembler.r_r11 = x86_64.regs[30];
			x86_64.regs[31] = new Target.Reg { name = "r12", id = 31, type = 0, size = 8, mask = 2147483648 };
			libtysila5.target.x86_64.x86_64_Assembler.r_r12 = x86_64.regs[31];
			x86_64.regs[32] = new Target.Reg { name = "r13", id = 32, type = 0, size = 8, mask = 4294967296 };
			libtysila5.target.x86_64.x86_64_Assembler.r_r13 = x86_64.regs[32];
			x86_64.regs[33] = new Target.Reg { name = "r14", id = 33, type = 0, size = 8, mask = 8589934592 };
			libtysila5.target.x86_64.x86_64_Assembler.r_r14 = x86_64.regs[33];
			x86_64.regs[34] = new Target.Reg { name = "r15", id = 34, type = 0, size = 8, mask = 17179869184 };
			libtysila5.target.x86_64.x86_64_Assembler.r_r15 = x86_64.regs[34];
			x86_64.regs[16] = new Target.Reg { name = "xmm0", id = 16, type = 1, size = 8, mask = 65536 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm0 = x86_64.regs[16];
			x86_64.regs[17] = new Target.Reg { name = "xmm1", id = 17, type = 1, size = 8, mask = 131072 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm1 = x86_64.regs[17];
			x86_64.regs[18] = new Target.Reg { name = "xmm2", id = 18, type = 1, size = 8, mask = 262144 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm2 = x86_64.regs[18];
			x86_64.regs[19] = new Target.Reg { name = "xmm3", id = 19, type = 1, size = 8, mask = 524288 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm3 = x86_64.regs[19];
			x86_64.regs[20] = new Target.Reg { name = "xmm4", id = 20, type = 1, size = 8, mask = 1048576 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm4 = x86_64.regs[20];
			x86_64.regs[21] = new Target.Reg { name = "xmm5", id = 21, type = 1, size = 8, mask = 2097152 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm5 = x86_64.regs[21];
			x86_64.regs[22] = new Target.Reg { name = "xmm6", id = 22, type = 1, size = 8, mask = 4194304 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm6 = x86_64.regs[22];
			x86_64.regs[23] = new Target.Reg { name = "xmm7", id = 23, type = 1, size = 8, mask = 8388608 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm7 = x86_64.regs[23];
			x86_64.regs[35] = new Target.Reg { name = "xmm8", id = 35, type = 1, size = 8, mask = 34359738368 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm8 = x86_64.regs[35];
			x86_64.regs[36] = new Target.Reg { name = "xmm9", id = 36, type = 1, size = 8, mask = 68719476736 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm9 = x86_64.regs[36];
			x86_64.regs[37] = new Target.Reg { name = "xmm10", id = 37, type = 1, size = 8, mask = 137438953472 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm10 = x86_64.regs[37];
			x86_64.regs[38] = new Target.Reg { name = "xmm11", id = 38, type = 1, size = 8, mask = 274877906944 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm11 = x86_64.regs[38];
			x86_64.regs[39] = new Target.Reg { name = "xmm12", id = 39, type = 1, size = 8, mask = 549755813888 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm12 = x86_64.regs[39];
			x86_64.regs[40] = new Target.Reg { name = "xmm13", id = 40, type = 1, size = 8, mask = 1099511627776 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm13 = x86_64.regs[40];
			x86_64.regs[41] = new Target.Reg { name = "xmm14", id = 41, type = 1, size = 8, mask = 2199023255552 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm14 = x86_64.regs[41];
			x86_64.regs[42] = new Target.Reg { name = "xmm15", id = 42, type = 1, size = 8, mask = 4398046511104 };
			libtysila5.target.x86_64.x86_64_Assembler.r_xmm15 = x86_64.regs[42];
			x86_64.regs[43] = new Target.Reg { name = "raxrdx", id = 43, type = 4, size = 8, mask = 1152 };
			libtysila5.target.x86_64.x86_64_Assembler.r_raxrdx = x86_64.regs[43];
			targets["x86_64"] = x86_64;
		}
	}
}

namespace libtysila5.target.x86
{
	public partial class x86_Assembler
	{
		public static Target.Reg[] registers;
		public static Target.Reg r_stack;
		public static Target.Reg r_contents;
		public static Target.Reg r_eip;
		public static Target.Reg r_eax;
		public static Target.Reg r_ebx;
		public static Target.Reg r_ecx;
		public static Target.Reg r_edx;
		public static Target.Reg r_edi;
		public static Target.Reg r_esi;
		public static Target.Reg r_esp;
		public static Target.Reg r_ebp;
		public static Target.Reg r_st0;
		public static Target.Reg r_xmm0;
		public static Target.Reg r_xmm1;
		public static Target.Reg r_xmm2;
		public static Target.Reg r_xmm3;
		public static Target.Reg r_xmm4;
		public static Target.Reg r_xmm5;
		public static Target.Reg r_xmm6;
		public static Target.Reg r_xmm7;
		public static Target.Reg r_eaxedx;
		
		void init_ccs()
		{
			init_cc_callee_preserves_map();
			init_cc_caller_preserves_map();
			init_cc_map();
			init_retcc_map();
			init_cc_classmap();
			init_retcc_classmap();
		}
		
		internal x86_Assembler()
		{
			init_ccs();
			ct_regs[89] = 6912;
			ct_regs[90] = 0;
			ct_regs[92] = 8323072;
			ct_regs[91] = ct_regs[89];
			ct_regs[93] = ct_regs[89];
			ct_regs[94] = ct_regs[89];
			instrs.trie = x86_instrs;
			instrs.start = x86_instrs_start;
			instrs.vals = x86_instrs_vals;
			psize = 4;
		}
		
		int[] x86_instrs = new int[] {
			0, 1, 0, 0, 1, 0, 2, 0, 0, 1, 0, 3, 0, 0, 1, 0, 
			4, 0, 0, 1, 0, 5, 0, 0, 1, 0, 6, 0, 0, 1, 0, 7, 0, 
			0, 1, 0, 8, 0, 0, 1, 0, 9, 0, 0, 1, 0, 10, 0, 0, 1, 
			0, 11, 0, 0, 1, 0, 12, 0, 0, 1, 0, 13, 0, 0, 1, 0, 14, 
			0, 0, 1, 0, 15, 0, 0, 1, 0, 16, 0, 0, 1, 0, 17, 0, 0, 
			1, 0, 18, 0, 0, 1, 0, 19, 0, 0, 1, 0, 20, 0, 0, 1, 0, 
			21, 0, 0, 1, 0, 22, 0, 0, 1, 0, 23, 0, 0, 1, 0, 24, 0, 
			0, 1, 0, 25, 0, 0, 1, 0, 26, 0, 0, 1, 0, 27, 1, 41, 2, 
			121, 126, 28, 0, 0, 1, 0, 0, 1, 42, 1, 137, 0, 2, 21, 1, 142, 
			0, 3, 58, 1, 147, 0, 4, 21, 1, 152, 0, 5, 40, 1, 157, 29, 0, 
			0, 1, 0, 30, 0, 0, 1, 0, 0, 1, 41, 1, 172, 0, 2, 21, 1, 
			177, 31, 6, 21, 38, 131, 0, 162, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
			0, 167, 0, 0, 0, 0, 0, 0, 182, 32, 0, 0, 1, 0, 33, 0, 0, 
			1, 0, 34, 0, 0, 1, 0, 35, 0, 0, 1, 0, 36, 0, 0, 1, 0, 
			37, 0, 0, 1, 0, 38, 0, 0, 1, 0, 39, 0, 0, 1, 0, 40, 0, 
			0, 1, 0, 41, 0, 0, 1, 0, 42, 0, 0, 1, 0, 43, 0, 0, 1, 
			0, 44, 0, 0, 1, 0, 45, 0, 0, 1, 0, 46, 0, 0, 1, 0, 47, 
			0, 0, 1, 0, 48, 0, 0, 1, 0, 0, 7, 21, 55, 1, 6, 11, 16, 
			21, 26, 31, 36, 41, 46, 0, 51, 0, 56, 61, 66, 71, 76, 81, 86, 91, 
			96, 0, 0, 0, 0, 101, 0, 106, 111, 116, 0, 0, 0, 0, 0, 0, 187, 
			229, 234, 239, 244, 249, 254, 259, 264, 269, 274, 279, 284, 289, 294, 299, 304, 309, 
		};
		
		InstructionHandler[] x86_instrs_vals = new InstructionHandler[] {
			default(InstructionHandler),
			libtysila5.target.x86.x86_Assembler.handle_add,
			libtysila5.target.x86.x86_Assembler.handle_sub,
			libtysila5.target.x86.x86_Assembler.handle_mul,
			libtysila5.target.x86.x86_Assembler.handle_div,
			libtysila5.target.x86.x86_Assembler.handle_and,
			libtysila5.target.x86.x86_Assembler.handle_or,
			libtysila5.target.x86.x86_Assembler.handle_xor,
			libtysila5.target.x86.x86_Assembler.handle_not,
			libtysila5.target.x86.x86_Assembler.handle_neg,
			libtysila5.target.x86.x86_Assembler.handle_call,
			libtysila5.target.x86.x86_Assembler.handle_calli,
			libtysila5.target.x86.x86_Assembler.handle_ret,
			libtysila5.target.x86.x86_Assembler.handle_cmp,
			libtysila5.target.x86.x86_Assembler.handle_br,
			libtysila5.target.x86.x86_Assembler.handle_brif,
			libtysila5.target.x86.x86_Assembler.handle_enter,
			libtysila5.target.x86.x86_Assembler.handle_enter_handler,
			libtysila5.target.x86.x86_Assembler.handle_conv,
			libtysila5.target.x86.x86_Assembler.handle_stind,
			libtysila5.target.x86.x86_Assembler.handle_ldind,
			libtysila5.target.x86.x86_Assembler.handle_ldlabaddr,
			libtysila5.target.x86.x86_Assembler.handle_ldfp,
			libtysila5.target.x86.x86_Assembler.handle_ldloca,
			libtysila5.target.x86.x86_Assembler.handle_zeromem,
			libtysila5.target.x86.x86_Assembler.handle_ldc_add_stind,
			libtysila5.target.x86.x86_Assembler.handle_ldc_add_ldind,
			libtysila5.target.x86.x86_Assembler.handle_ldc_add,
			libtysila5.target.x86.x86_Assembler.handle_getCharSeq,
			libtysila5.target.x86.x86_Assembler.handle_ldc_zeromem,
			libtysila5.target.x86.x86_Assembler.handle_ldc_ldc_add_stind,
			libtysila5.target.x86.x86_Assembler.handle_ldc,
			libtysila5.target.x86.x86_Assembler.handle_ldloc,
			libtysila5.target.x86.x86_Assembler.handle_stloc,
			libtysila5.target.x86.x86_Assembler.handle_rem,
			libtysila5.target.x86.x86_Assembler.handle_ldarg,
			libtysila5.target.x86.x86_Assembler.handle_starg,
			libtysila5.target.x86.x86_Assembler.handle_ldarga,
			libtysila5.target.x86.x86_Assembler.handle_stackcopy,
			libtysila5.target.x86.x86_Assembler.handle_localloc,
			libtysila5.target.x86.x86_Assembler.handle_shift,
			libtysila5.target.x86.x86_Assembler.handle_shift,
			libtysila5.target.x86.x86_Assembler.handle_shift,
			libtysila5.target.x86.x86_Assembler.handle_switch,
			libtysila5.target.x86.x86_Assembler.handle_ldobja,
			libtysila5.target.x86.x86_Assembler.handle_cctor_runonce,
			libtysila5.target.x86.x86_Assembler.handle_break,
			libtysila5.target.x86.x86_Assembler.handle_portin,
			libtysila5.target.x86.x86_Assembler.handle_portout,
		};
		
		int x86_instrs_start = 314;
	}
}

namespace libtysila5.target.x86_64
{
	public partial class x86_64_Assembler
	{
		public static Target.Reg[] registers;
		public static Target.Reg r_stack;
		public static Target.Reg r_contents;
		public static Target.Reg r_rip;
		public static Target.Reg r_rax;
		public static Target.Reg r_rbx;
		public static Target.Reg r_rcx;
		public static Target.Reg r_rdx;
		public static Target.Reg r_rsi;
		public static Target.Reg r_rdi;
		public static Target.Reg r_r8;
		public static Target.Reg r_r9;
		public static Target.Reg r_r10;
		public static Target.Reg r_r11;
		public static Target.Reg r_r12;
		public static Target.Reg r_r13;
		public static Target.Reg r_r14;
		public static Target.Reg r_r15;
		public static Target.Reg r_xmm0;
		public static Target.Reg r_xmm1;
		public static Target.Reg r_xmm2;
		public static Target.Reg r_xmm3;
		public static Target.Reg r_xmm4;
		public static Target.Reg r_xmm5;
		public static Target.Reg r_xmm6;
		public static Target.Reg r_xmm7;
		public static Target.Reg r_xmm8;
		public static Target.Reg r_xmm9;
		public static Target.Reg r_xmm10;
		public static Target.Reg r_xmm11;
		public static Target.Reg r_xmm12;
		public static Target.Reg r_xmm13;
		public static Target.Reg r_xmm14;
		public static Target.Reg r_xmm15;
		public static Target.Reg r_raxrdx;
		
		void init_ccs()
		{
			init_cc_callee_preserves_map();
			init_cc_caller_preserves_map();
			init_cc_map();
			init_retcc_map();
			init_cc_classmap();
			init_retcc_classmap();
		}
		
		internal x86_64_Assembler()
		{
			init_ccs();
			ct_regs[89] = 34225527552;
			ct_regs[90] = 34225527552;
			ct_regs[92] = 8761741606912;
			ct_regs[91] = ct_regs[90];
			ct_regs[93] = ct_regs[90];
			ct_regs[94] = ct_regs[90];
			instrs.trie = x86_64_instrs;
			instrs.start = x86_64_instrs_start;
			instrs.vals = x86_64_instrs_vals;
			psize = 8;
		}
		
		int[] x86_64_instrs = new int[] {
			0, 1, 0, 0, 1, 0, 2, 0, 0, 1, 0, 3, 0, 0, 1, 0, 
			4, 0, 0, 1, 0, 5, 0, 0, 1, 0, 6, 0, 0, 1, 0, 7, 0, 
			0, 1, 0, 8, 0, 0, 1, 0, 9, 0, 0, 1, 0, 10, 0, 0, 1, 
			0, 11, 0, 0, 1, 0, 12, 0, 0, 1, 0, 13, 0, 0, 1, 0, 14, 
			0, 0, 1, 0, 15, 0, 0, 1, 0, 16, 0, 0, 1, 0, 17, 0, 0, 
			1, 0, 18, 0, 0, 1, 0, 19, 0, 0, 1, 0, 20, 0, 0, 1, 0, 
			21, 0, 0, 1, 0, 22, 0, 0, 1, 0, 23, 0, 0, 1, 0, 24, 0, 
			0, 1, 0, 25, 0, 0, 1, 0, 26, 0, 0, 1, 0, 27, 1, 41, 2, 
			121, 126, 28, 0, 0, 1, 0, 0, 1, 42, 1, 137, 0, 2, 21, 1, 142, 
			0, 3, 58, 1, 147, 0, 4, 21, 1, 152, 0, 5, 40, 1, 157, 29, 0, 
			0, 1, 0, 30, 0, 0, 1, 0, 0, 1, 41, 1, 172, 0, 2, 21, 1, 
			177, 31, 6, 21, 38, 131, 0, 162, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
			0, 167, 0, 0, 0, 0, 0, 0, 182, 32, 0, 0, 1, 0, 33, 0, 0, 
			1, 0, 34, 0, 0, 1, 0, 35, 0, 0, 1, 0, 36, 0, 0, 1, 0, 
			37, 0, 0, 1, 0, 38, 0, 0, 1, 0, 39, 0, 0, 1, 0, 40, 0, 
			0, 1, 0, 41, 0, 0, 1, 0, 42, 0, 0, 1, 0, 43, 0, 0, 1, 
			0, 44, 0, 0, 1, 0, 45, 0, 0, 1, 0, 46, 0, 0, 1, 0, 47, 
			0, 0, 1, 0, 48, 0, 0, 1, 0, 0, 7, 21, 55, 1, 6, 11, 16, 
			21, 26, 31, 36, 41, 46, 0, 51, 0, 56, 61, 66, 71, 76, 81, 86, 91, 
			96, 0, 0, 0, 0, 101, 0, 106, 111, 116, 0, 0, 0, 0, 0, 0, 187, 
			229, 234, 239, 244, 249, 254, 259, 264, 269, 274, 279, 284, 289, 294, 299, 304, 309, 
		};
		
		InstructionHandler[] x86_64_instrs_vals = new InstructionHandler[] {
			default(InstructionHandler),
			libtysila5.target.x86_64.x86_64_Assembler.handle_add,
			libtysila5.target.x86_64.x86_64_Assembler.handle_sub,
			libtysila5.target.x86_64.x86_64_Assembler.handle_mul,
			libtysila5.target.x86_64.x86_64_Assembler.handle_div,
			libtysila5.target.x86_64.x86_64_Assembler.handle_and,
			libtysila5.target.x86_64.x86_64_Assembler.handle_or,
			libtysila5.target.x86_64.x86_64_Assembler.handle_xor,
			libtysila5.target.x86_64.x86_64_Assembler.handle_not,
			libtysila5.target.x86_64.x86_64_Assembler.handle_neg,
			libtysila5.target.x86_64.x86_64_Assembler.handle_call,
			libtysila5.target.x86_64.x86_64_Assembler.handle_calli,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ret,
			libtysila5.target.x86_64.x86_64_Assembler.handle_cmp,
			libtysila5.target.x86_64.x86_64_Assembler.handle_br,
			libtysila5.target.x86_64.x86_64_Assembler.handle_brif,
			libtysila5.target.x86_64.x86_64_Assembler.handle_enter,
			libtysila5.target.x86_64.x86_64_Assembler.handle_enter_handler,
			libtysila5.target.x86_64.x86_64_Assembler.handle_conv,
			libtysila5.target.x86_64.x86_64_Assembler.handle_stind,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldind,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldlabaddr,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldfp,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldloca,
			libtysila5.target.x86_64.x86_64_Assembler.handle_zeromem,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldc_add_stind,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldc_add_ldind,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldc_add,
			libtysila5.target.x86_64.x86_64_Assembler.handle_getCharSeq,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldc_zeromem,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldc_ldc_add_stind,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldc,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldloc,
			libtysila5.target.x86_64.x86_64_Assembler.handle_stloc,
			libtysila5.target.x86_64.x86_64_Assembler.handle_rem,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldarg,
			libtysila5.target.x86_64.x86_64_Assembler.handle_starg,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldarga,
			libtysila5.target.x86_64.x86_64_Assembler.handle_stackcopy,
			libtysila5.target.x86_64.x86_64_Assembler.handle_localloc,
			libtysila5.target.x86_64.x86_64_Assembler.handle_shift,
			libtysila5.target.x86_64.x86_64_Assembler.handle_shift,
			libtysila5.target.x86_64.x86_64_Assembler.handle_shift,
			libtysila5.target.x86_64.x86_64_Assembler.handle_switch,
			libtysila5.target.x86_64.x86_64_Assembler.handle_ldobja,
			libtysila5.target.x86_64.x86_64_Assembler.handle_cctor_runonce,
			libtysila5.target.x86_64.x86_64_Assembler.handle_break,
			libtysila5.target.x86_64.x86_64_Assembler.handle_portin,
			libtysila5.target.x86_64.x86_64_Assembler.handle_portout,
		};
		
		int x86_64_instrs_start = 314;
	}
}

