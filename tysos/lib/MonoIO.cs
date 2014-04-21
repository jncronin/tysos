/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace tysos.lib
{
    class MonoIO
    {
        internal const long STDOUT = 1;
        internal const long STDIN = 0;
        internal const long STDERR = 2;
        internal const long INVALID = -1;

        internal const System.IO.FileAttributes InvalidFileAttributes = (System.IO.FileAttributes)(-1);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern tysos.IFile ReinterpretAsIFile(IntPtr handle);

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_23get_VolumeSeparatorChar_Rc_P0")]
        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_26get_DirectorySeparatorChar_Rc_P0")]
        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_29get_AltDirectorySeparatorChar_Rc_P0")]
        [libsupcs.AlwaysCompile]
        static char get_DirectorySeparatorChar() { return '/'; }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_17get_PathSeparator_Rc_P0")]
        [libsupcs.AlwaysCompile]
        static char get_PathSeparator() { return ':'; }

        [libsupcs.MethodAlias("_ZW6System13ConsoleDriverM_0_6Isatty_Rb_P1u1I")]
        [libsupcs.AlwaysCompile]
        static bool Isatty(IntPtr handle)
        {
            if ((handle == (IntPtr)STDIN) || (handle == (IntPtr)STDOUT) || (handle == (IntPtr)STDERR))
                return true;
            return false;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_17get_ConsoleOutput_Ru1I_P0")]
        [libsupcs.AlwaysCompile]
        static IntPtr get_ConsoleOutput()
        {
            Formatter.WriteLine("MonoIO.getConsoleOutput: called", Program.arch.DebugOutput);
            return (IntPtr)STDOUT;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_16get_ConsoleInput_Ru1I_P0")]
        [libsupcs.AlwaysCompile]
        static IntPtr get_ConsoleInput()
        {
            Formatter.WriteLine("MonoIO.getConsoleInput: called", Program.arch.DebugOutput);
            return (IntPtr)STDIN;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_16get_ConsoleError_Ru1I_P0")]
        [libsupcs.AlwaysCompile]
        static IntPtr get_ConsoleError()
        {
            Formatter.WriteLine("MonoIO.getConsoleError: called", Program.arch.DebugOutput);
            return (IntPtr)STDERR;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_11GetFileType_RV12MonoFileType_P2u1IRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static MonoFileType GetFileType(IntPtr handle, out MonoIOError err)
        {
            long h = (long)handle;

            Formatter.WriteLine("MonoIO.GetFileType: called", Program.arch.DebugOutput);

            if (h == INVALID)
            {
                err = MonoIOError.ERROR_INVALID_HANDLE;
                return MonoFileType.Unknown;
            }

            if ((h == STDOUT) || (h == STDIN) || (h == STDERR))
            {
                err = MonoIOError.ERROR_SUCCESS;
                return MonoFileType.Char;
            }

            if ((h < 0) || (h >= 0x1000))
            {
                err = MonoIOError.ERROR_SUCCESS;
                return MonoFileType.Char;
            }

            err = MonoIOError.ERROR_INVALID_HANDLE;
            return MonoFileType.Unknown;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_4Read_Ri_P5u1Iu1ZhiiRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static int Read(IntPtr handle, byte[] dest, int dest_offset, int count, out MonoIOError error)
        {
            long h = (long)handle;

            Formatter.Write("MonoIO.Read: called (handle: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)h, "X", Program.arch.DebugOutput);
            Formatter.Write(" dest_offset: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)dest_offset, Program.arch.DebugOutput);
            Formatter.Write(" count: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)count, Program.arch.DebugOutput);
            Formatter.WriteLine(")", Program.arch.DebugOutput);


            if (h == INVALID)
            {
                error = MonoIOError.ERROR_INVALID_HANDLE;
                return -1;
            }

            if (h == STDIN)
            {
                if (Program.cur_cpu_data.CurrentThread.owning_process.stdin != null)
                {
                    int ret = Program.cur_cpu_data.CurrentThread.owning_process.stdin.Read(dest, dest_offset, count);
                    error = MonoIOError.ERROR_SUCCESS;
                    return ret;
                }
                else
                    throw new Exception("Process does not have a stdin stream");
            }

            if ((h < 0) || (h >= 0x1000))
            {
                /* A 'safe' limit to ensure we're dealing with an actual pointer */
                tysos.IFile file = ReinterpretAsIFile(handle);

                tysos.IInputStream istream = file.GetInputStream();
                if (istream == null)
                {
                    error = MonoIOError.ERROR_READ_FAULT;
                    return -1;
                }

                error = MonoIOError.ERROR_SUCCESS;
                int ret = istream.Read(dest, dest_offset, count);
                tysos.Syscalls.DebugFunctions.DebugWrite("MonoIO.Read: reading from file, returning " + ret.ToString() + "\n");
                return ret;
            }

            error = MonoIOError.ERROR_INVALID_HANDLE;
            return -1;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_5Write_Ri_P5u1Iu1ZhiiRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static int Write(IntPtr handle, byte[] src, int src_offset, int count, out MonoIOError error)
        {
            long h = (long)handle;

            Formatter.WriteLine("MonoIO.Write: called", Program.arch.DebugOutput);

            if (h == INVALID)
            {
                error = MonoIOError.ERROR_INVALID_HANDLE;
                return -1;
            }

            if (h == STDOUT)
            {
                Formatter.WriteLine("MonoIO.Write: handle is STDOUT", Program.arch.DebugOutput);
                if (Program.cur_cpu_data.CurrentThread.owning_process.stdout != null)
                {
                    Formatter.WriteLine("MonoIO.Write: thread has stdout stream", Program.arch.DebugOutput);
                    Program.cur_cpu_data.CurrentThread.owning_process.stdout.Write(src, src_offset, count);
                    error = MonoIOError.ERROR_SUCCESS;
                    return count;
                }
                else
                {
                    error = MonoIOError.ERROR_SUCCESS;
                    return count;
                }
            }

            if (h == STDERR)
            {
                if (Program.cur_cpu_data.CurrentThread.owning_process.stderr != null)
                {
                    Program.cur_cpu_data.CurrentThread.owning_process.stderr.Write(src, src_offset, count);
                    error = MonoIOError.ERROR_SUCCESS;
                    return count;
                }
                else
                {
                    error = MonoIOError.ERROR_SUCCESS;
                    return count;
                }
            }

            throw new Exception("Write not yet implemented");
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_19GetCurrentDirectory_Ru1S_P1RV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static string GetCurrentDirectory(out MonoIOError error)
        {
            error = MonoIOError.ERROR_SUCCESS;
            return Program.cur_cpu_data.CurrentThread.owning_process.current_directory;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_17GetFileAttributes_RV14FileAttributes_P2u1SRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static System.IO.FileAttributes GetFileAttributes(string path, out MonoIOError error)
        {
            if(Program.vfs == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return InvalidFileAttributes;
            }

            System.IO.FileAttributes fa = Program.vfs.GetFileAttributes(ref path);
            if (fa == InvalidFileAttributes)
                error = MonoIOError.ERROR_FILE_NOT_FOUND;
            else
                error = MonoIOError.ERROR_SUCCESS;
            return fa;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_20GetFileSystemEntries_Ru1Zu1S_P5u1Su1SiiRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static string[] GetFileSystemEntries(string path, string path_with_pattern, int attrs, int mask, out MonoIOError error)
        {
            Formatter.WriteLine("MonoIO: GetFileSystemEntries: called with path: " + path + " and path_with_pattern: " + path_with_pattern, Program.arch.DebugOutput);
            if (Program.vfs == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return null;
            }

            string[] ret = Program.vfs.GetFileSystemEntries(path, path_with_pattern, attrs, mask);
            if(ret == null)
                error = MonoIOError.ERROR_FILE_NOT_FOUND;
            else
                error = MonoIOError.ERROR_SUCCESS;
            return ret;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_19SetCurrentDirectory_Rb_P2u1SRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static bool SetCurrentDirectory(string path, out MonoIOError error)
        {
            if (Program.vfs == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return false;
            }

            System.IO.FileAttributes fa = Program.vfs.GetFileAttributes(ref path);
            if ((fa == InvalidFileAttributes) || ((fa & System.IO.FileAttributes.Directory) != System.IO.FileAttributes.Directory))
            {
                error = MonoIOError.ERROR_PATH_NOT_FOUND;
                return false;
            }

            Formatter.WriteLine("MonoIO: SetCurrentDirectory: setting current directory to " + path, Program.arch.DebugOutput);
            Program.cur_cpu_data.CurrentThread.owning_process.current_directory = path;
            error = MonoIOError.ERROR_SUCCESS;
            return true;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_4Open_Ru1I_P6u1SV8FileModeV10FileAccessV9FileShareV11FileOptionsRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static tysos.IFile Open(string name, System.IO.FileMode mode, System.IO.FileAccess access,
            System.IO.FileShare share, System.IO.FileOptions options, out MonoIOError error)
        {
            if (Program.vfs == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return null;
            }

            return Program.vfs.OpenFile(name, mode, access, share, options, out error);
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIOM_0_5Close_Rb_P2u1IRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static bool Close(tysos.IFile handle, out MonoIOError error)
        {
            if (Program.vfs == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return false;
            }

            return Program.vfs.CloseFile(handle, out error);
        }
    }
}
