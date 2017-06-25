
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.6.1055.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly extern libsupcs
{
  .ver 1:0:0:0
}
.assembly simple_loop
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module simple_loop.exe
// MVID: {1431E7FF-EA5D-46B7-960F-8C1B1FD7CF3A}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x027A0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit simple_loop.Test
       extends [mscorlib]System.Object
{
  .custom instance void [libsupcs]libsupcs.NoBaseClassAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static int32  Main(string[] args) cil managed
  {
    .entrypoint
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0,
             bool V_1)
	IL_0000: br.s IL_0000
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class simple_loop.Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file simple_loop.res
