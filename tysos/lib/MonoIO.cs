/* Copyright (C) 2008 - 2015 by John Cronin
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
        [libsupcs.ReinterpretAsMethod]
        internal static extern tysos.IFile ReinterpretAsIFile(IntPtr handle);

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_23get_VolumeSeparatorChar_Rc_P0")]
        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_26get_DirectorySeparatorChar_Rc_P0")]
        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_29get_AltDirectorySeparatorChar_Rc_P0")]
        [libsupcs.AlwaysCompile]
        static char get_DirectorySeparatorChar() { return '/'; }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_17get_PathSeparator_Rc_P0")]
        [libsupcs.AlwaysCompile]
        static char get_PathSeparator() { return ':'; }

        [libsupcs.MethodAlias("_ZW6System13ConsoleDriver_6Isatty_Rb_P1u1I")]
        [libsupcs.AlwaysCompile]
        static bool Isatty(lib.File handle)
        {
            return handle.Isatty;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_17get_ConsoleOutput_Ru1I_P0")]
        [libsupcs.AlwaysCompile]
        static tysos.lib.File get_ConsoleOutput()
        {
            Formatter.WriteLine("MonoIO.getConsoleOutput: called", Program.arch.DebugOutput);
            return Program.arch.CurrentCpu.CurrentThread.owning_process.stdout;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_16get_ConsoleInput_Ru1I_P0")]
        [libsupcs.AlwaysCompile]
        static tysos.lib.File get_ConsoleInput()
        {
            Formatter.WriteLine("MonoIO.getConsoleInput: called", Program.arch.DebugOutput);
            return Program.arch.CurrentCpu.CurrentThread.owning_process.stdin;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_16get_ConsoleError_Ru1I_P0")]
        [libsupcs.AlwaysCompile]
        static tysos.lib.File get_ConsoleError()
        {
            Formatter.WriteLine("MonoIO.getConsoleError: called", Program.arch.DebugOutput);
            return Program.arch.CurrentCpu.CurrentThread.owning_process.stderr;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_11GetFileType_RV12MonoFileType_P2u1IRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static MonoFileType GetFileType(tysos.lib.File handle, out MonoIOError err)
        {
            err = MonoIOError.ERROR_SUCCESS;
            return handle.FileType;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_4Read_Ri_P5u1Iu1ZhiiRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static int Read(lib.File handle, byte[] dest, int dest_offset, int count, out MonoIOError error)
        {
            Formatter.Write("MonoIO.Read: called (handle: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)libsupcs.CastOperations.ReinterpretAsUlong(handle), "X", Program.arch.DebugOutput);
            Formatter.Write(" dest_offset: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)dest_offset, Program.arch.DebugOutput);
            Formatter.Write(" count: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)count, Program.arch.DebugOutput);
            Formatter.WriteLine(")", Program.arch.DebugOutput);

            if(handle == null)
            {
                error = MonoIOError.ERROR_INVALID_HANDLE;
                return -1;
            }

            int ret = handle.Read(dest, dest_offset, count);
            error = handle.Error;
            return ret;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_5Write_Ri_P5u1Iu1ZhiiRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static int Write(lib.File handle, byte[] src, int src_offset, int count, out MonoIOError error)
        {
            Formatter.Write("MonoIO.Write: called (handle: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)libsupcs.CastOperations.ReinterpretAsUlong(handle), "X", Program.arch.DebugOutput);
            Formatter.Write(" src_offset: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)src_offset, Program.arch.DebugOutput);
            Formatter.Write(" count: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)count, Program.arch.DebugOutput);
            Formatter.WriteLine(")", Program.arch.DebugOutput);

            if (handle == null)
            {
                error = MonoIOError.ERROR_INVALID_HANDLE;
                return -1;
            }

            int ret = handle.Write(src, src_offset, count);
            error = handle.Error;
            return ret;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_19GetCurrentDirectory_Ru1S_P1RV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static string GetCurrentDirectory(out MonoIOError error)
        {
            error = MonoIOError.ERROR_SUCCESS;
            return Program.arch.CurrentCpu.CurrentThread.owning_process.current_directory;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_17GetFileAttributes_RV14FileAttributes_P2u1SRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static System.IO.FileAttributes GetFileAttributes(string path, out MonoIOError error)
        {
            //Formatter.WriteLine("MonoIO: GetFileAttributes: called with path: " + path, Program.arch.DebugOutput);
            while (Program.Vfs == null) ;
            /*if (Program.Vfs == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return InvalidFileAttributes;
            }*/

            //System.IO.FileAttributes fa = (System.IO.FileAttributes)Program.Vfs.Invoke("GetFileAttributes", new object[] { path }, File.sig_vfs_GetFileAttributes);
            System.IO.FileAttributes fa = Program.Vfs.GetFileAttributes(path).Sync();

            if (fa == InvalidFileAttributes)
                error = MonoIOError.ERROR_FILE_NOT_FOUND;
            else
                error = MonoIOError.ERROR_SUCCESS;
            return fa;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_20GetFileSystemEntries_Ru1Zu1S_P5u1Su1SiiRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static string[] GetFileSystemEntries(string path, string path_with_pattern, int attrs, int mask, out MonoIOError error)
        {
            //Formatter.WriteLine("MonoIO: GetFileSystemEntries: called with path: " + path + " and path_with_pattern: " + path_with_pattern, Program.arch.DebugOutput);
            while (Program.Vfs == null) ;

            //string[] ret = Program.Vfs.Invoke("GetFileSystemEntries",
            //    new object[] { path, path_with_pattern, attrs, mask },
            //    File.sig_vfs_GetFileSystemEntries) as string[];
            string[] ret = Program.Vfs.GetFileSystemEntries(path, path_with_pattern, attrs, mask).Sync();

            if(ret == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return new string[] { };
            }

            error = MonoIOError.ERROR_SUCCESS;
            return ret;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_19SetCurrentDirectory_Rb_P2u1SRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static bool SetCurrentDirectory(string path, out MonoIOError error)
        {
            Formatter.WriteLine("MonoIO: SetCurrentDirectory: called with path: " + path, Program.arch.DebugOutput);
            if (Program.Vfs == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return false;
            }

            //System.IO.FileAttributes fa = (System.IO.FileAttributes)Program.Vfs.Invoke("GetFileAttributes", new object[] { path }, File.sig_vfs_GetFileAttributes);
            System.IO.FileAttributes fa = Program.Vfs.GetFileAttributes(path).Sync();
            if ((fa == InvalidFileAttributes) || ((fa & System.IO.FileAttributes.Directory) != System.IO.FileAttributes.Directory))
            {
                error = MonoIOError.ERROR_PATH_NOT_FOUND;
                return false;
            }

            Formatter.WriteLine("MonoIO: SetCurrentDirectory: setting current directory to " + path, Program.arch.DebugOutput);
            Program.arch.CurrentCpu.CurrentThread.owning_process.current_directory = path;
            error = MonoIOError.ERROR_SUCCESS;
            return true;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_4Open_Ru1I_P6u1SV8FileModeV10FileAccessV9FileShareV11FileOptionsRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static internal lib.File Open(string name, System.IO.FileMode mode, System.IO.FileAccess access,
            System.IO.FileShare share, System.IO.FileOptions options, out MonoIOError error)
        {
            if (Program.Vfs == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return null;
            }

            //tysos.lib.File ret = (tysos.lib.File)Program.Vfs.Invoke("OpenFile", 
            //    new object[] { name, mode, access, share, options },
            //    File.sig_vfs_OpenFile);
            File ret = Program.Vfs.OpenFile(name, mode, access, share, options).Sync();
            error = ret.Error;
            return ret;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_5Close_Rb_P2u1IRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static bool Close(lib.File handle, out MonoIOError error)
        {
            if (Program.Vfs == null)
            {
                error = MonoIOError.ERROR_GEN_FAILURE;
                return false;
            }

            //Program.Vfs.Invoke("CloseFile",
            //    new object[] { handle }, File.sig_vfs_CloseFile);
            Program.Vfs.CloseFile(handle);
            error = handle.Error;
            return error == MonoIOError.ERROR_SUCCESS;
        }

        [libsupcs.MethodAlias("_ZW11System#2EIO6MonoIO_11GetFileStat_Rb_P3u1SRV10MonoIOStatRV11MonoIOError")]
        [libsupcs.AlwaysCompile]
        static bool GetFileStat(string path, out MonoIOStat stat, out MonoIOError error)
        {
            stat = new MonoIOStat();

            while (Program.Vfs == null) ;

            //lib.File handle = (lib.File)Program.Vfs.Invoke("OpenFile",
            //    new object[] { path, System.IO.FileMode.Open, System.IO.FileAccess.Read,
            //        System.IO.FileShare.ReadWrite, System.IO.FileOptions.None },
            //    File.sig_vfs_OpenFile);
            File handle = Program.Vfs.OpenFile(path, System.IO.FileMode.Open, System.IO.FileAccess.Read,
                System.IO.FileShare.ReadWrite, System.IO.FileOptions.None).Sync();
            if(handle.Error != MonoIOError.ERROR_SUCCESS)
            {
                error = handle.Error;
                stat.Attributes = System.IO.FileAttributes.Offline;
                return false;
            }

            stat.Attributes = (System.IO.FileAttributes)handle.IntProperties;
            stat.Length = handle.Length;
            stat.Name = handle.Name;

            error = MonoIOError.ERROR_SUCCESS;

            return true;
        }
    }
}
