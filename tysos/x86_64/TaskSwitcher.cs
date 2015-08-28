/* Copyright (C) 2011 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace tysos.x86_64
{
    [libsupcs.SpecialType]
	class TaskSwitcher : tysos.TaskSwitcher
	{
        ulong cur_thread_pointer;
        ulong tsi_within_thread;
        ulong rsp_within_tsi;

        public TaskSwitcher()
        {
            // Load up the offsets of fields within structures

            System.Type cpu_type = typeof(Cpu);
            cur_thread_pointer = (ulong)((libsupcs.TysosField)cpu_type.GetField("currentThread")).Offset;
            cur_thread_pointer += libsupcs.CastOperations.ReinterpretAsUlong(Program.arch.CurrentCpu);

            System.Type thread_type = typeof(Thread);
            tsi_within_thread = (ulong)((libsupcs.TysosField)thread_type.GetField("saved_state")).Offset;

            System.Type tsi_type = typeof(x86_64.TaskSwitchInfo);
            rsp_within_tsi = (ulong)((libsupcs.TysosField)tsi_type.GetField("rsp")).Offset;
        }

        public override void Switch(Thread next)
        {
            //Formatter.WriteLine("x86_64: switching to " + next.name, Program.arch.DebugOutput);
            do_x86_64_switch(cur_thread_pointer, next, tsi_within_thread, rsp_within_tsi);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        extern static void do_x86_64_switch(ulong cur_thread_pointer, Thread next_thread, ulong tsi_offset_within_thread, ulong rsp_offset_within_tsi);
	}

    class TaskSwitchInfo : tysos.TaskSwitchInfo
    {
        public ulong rsp;

        public ulong send_eoi;

        ulong max_stack;

        const ulong DEFAULT_RFLAGS = 0x202;     // IF and bit 2 (bit 2 must be set - see Intel 3a:Figure 2.4)

        ulong get_param(object[] parameters, int idx)
        {
            if(parameters == null)
                return 0;
            if (idx >= parameters.Length)
                return 0;
            return libsupcs.CastOperations.ReinterpretAsUlong(parameters[idx]);
        }

        public override void Init(UIntPtr entry_address, Virtual_Regions.Region stack, UIntPtr exit_address, object[] parameters)
        {
            unsafe
            {
                max_stack = stack.start + stack.length;
                ulong* p_st = (ulong*)(max_stack);                

                *--p_st = (ulong)exit_address;          // address of __exit()

                /* The entry point to the thread */
                *--p_st = (ulong)entry_address;

                if (parameters.Length > 6)
                    throw new NotImplementedException("x86_64 switcher: only <= 6 parameters of type INTEGER supported");

                /* The next entries are designed to reflect what is pushed when we call the task
                 * switch function, so that returing from the task switch function, with rsp set to the end
                 * of these entries will effect a task switch and restore the state */
                *--p_st = DEFAULT_RFLAGS;
                *--p_st = 0;                            // RAX
                *--p_st = 0;                            // RBX
                *--p_st = get_param(parameters, 3);     // RCX
                *--p_st = get_param(parameters, 2);     // RDX
                *--p_st = get_param(parameters, 1);     // RSI
                *--p_st = get_param(parameters, 0);     // RDI  
                *--p_st = 0;                            // RBP
                *--p_st = get_param(parameters, 4);     // R8
                *--p_st = get_param(parameters, 5);     // R9
                *--p_st = 0;                            // R10
                *--p_st = 0;                            // R11
                *--p_st = 0;                            // R12
                *--p_st = 0;                            // R13
                *--p_st = 0;                            // R14
                *--p_st = 0;                            // R15

                // XMMs
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;
                *--p_st = 0;

                rsp = (ulong)p_st;
            }
        }

        public override bool StackGrowsDownwards()
        {
            return true;
        }

        public override ulong GetMaximumStack()
        {
            return max_stack;
        }

        public override ulong GetSavedStackPointer()
        {
            return rsp;
        }

        public override ulong GetStackItemSize()
        {
            return 8;
        }
    }
}
