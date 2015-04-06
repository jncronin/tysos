/* Copyright (C) 2012 by John Cronin
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

namespace libtysila
{
    partial class Assembler
    {
        class ArchAssembler
        {
            public Architecture Architecture;
            public System.Type Assembler;
        }

        static ArchAssembler[] _ListArchitectures()
        {
            List<ArchAssembler> ret = new List<ArchAssembler>();

            Type[] ass_types = typeof(Assembler).Assembly.GetTypes();
            foreach (Type t in ass_types)
            {
                if (t.Name.ToLower().EndsWith("_assembler"))
                {
                    if ((t.IsSubclassOf(typeof(Assembler))) && (!t.IsAbstract))
                    {
                        System.Reflection.MethodInfo lami = t.GetMethod("ListAssemblerArchitectures", System.Type.EmptyTypes);

                        if (lami != null)
                        {
                            Assembler.Architecture[] archs = (Assembler.Architecture[])lami.Invoke(null, null);

                            foreach (Architecture arch in archs)
                                ret.Add(new ArchAssembler { Architecture = arch, Assembler = t });
                        }
                        else
                            ret.Add(new ArchAssembler
                            {
                                Architecture = new Assembler.Architecture { _instruction_set = t.Name.ToLower().Substring(0, t.Name.ToLower().LastIndexOf("_assembler")) },
                                Assembler = t
                            });
                    }
                }
            }

            return ret.ToArray();

        }

        public static Assembler.Architecture[] ListArchitectures()
        {
            List<Assembler.Architecture> ret = new List<Assembler.Architecture>();
            ArchAssembler[] archs = _ListArchitectures();
            foreach (ArchAssembler arch in archs)
                ret.Add(arch.Architecture);
            return ret.ToArray();            
        }

        public static Assembler CreateAssembler(Architecture arch, FileLoader fileLoader, MemberRequestor memberRequestor, AssemblerOptions options)
        {
            try
            {
                ArchAssembler[] archs = _ListArchitectures();
                Type asst = null;
                foreach (ArchAssembler a in archs)
                {
                    if (a.Architecture.Equals(arch))
                    {
                        asst = a.Assembler;
                        arch = a.Architecture;  // load up the extra options required by the assembler for this architecture
                        break;
                    }
                }

                if (asst == null)
                    throw new TypeLoadException();
                System.Reflection.ConstructorInfo ctorm = asst.GetConstructor(new Type[] { typeof(Architecture), typeof(FileLoader), typeof(MemberRequestor), typeof(AssemblerOptions) });
                if (ctorm == null)
                    throw new TypeLoadException();

                if (options == null)
                    options = new AssemblerOptions();

                return ctorm.Invoke(new object[] { arch, fileLoader, memberRequestor, options }) as Assembler;
            }
            catch (Exception e)
            {
                if ((e is TypeLoadException) || (e is NullReferenceException))
                {
                    Console.WriteLine(arch.ToString() + " is not a supported architecture");
                    Console.WriteLine();
                    return null;
                }
                else if (e is System.Reflection.TargetInvocationException)
                    throw ((System.Reflection.TargetInvocationException)e).InnerException;
                throw;
            }
        }

        public static Architecture ParseArchitectureString(string arch)
        {
            string[] split = arch.Split('-');
            if (split.Length < 3)
                return null;
            int iset_length = split.Length - 2;
            string oformat = split[iset_length];
            string os = split[iset_length + 1];
            string iset = string.Join("-", split, 0, iset_length);

            Architecture ret = new Architecture { _instruction_set = iset, _oformat = oformat, _os = os };

            Architecture[] archs = ListArchitectures();
            foreach (Architecture a in archs)
            {
                if ((a._instruction_set == ret._instruction_set) && (a._oformat == ret._oformat) && (a._os == ret._os))
                    return ret;
            }
            return null;
        }
    }
}
