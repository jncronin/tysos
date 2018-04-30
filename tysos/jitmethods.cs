using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using binary_library;
using libtysila5;
using libtysila5.layout;
using metadata;

namespace tysos
{
    class jittestmethods
    {
        internal static void test(libsupcs.TysosMethod meth)
        {
            var ms = meth.mspec;
            var t = libtysila5.target.Target.targets["x86_64"];
            var bf = new jit_binary();
            bf.Init();
            bf.Architecture = "x86_64";
            var st = new libtysila5.StringTable("jit", libsupcs.Metadata.BAL, t);
            t.st = st;
            t.r = new jit_requestor();
            t.InitIntcalls();
            
            libtysila5.libtysila.AssembleMethod(ms, bf, t);
        }

        static string get_string()
        {
            return "I am tysos";
        }

        class jit_binary : binary_library.BinaryFile
        {
            public override Bitness Bitness { get { return Bitness.Bits64; } set { } }

            public override IProgramHeader ProgramHeader => throw new NotImplementedException();
        }

        class jit_requestor : libtysila5.Requestor
        {
            vt_requestor vtr = new vt_requestor();
            m_requestor mr = new m_requestor();
            eh_requestor ehr = new eh_requestor();
            bm_requestor bmr = new bm_requestor();
            sf_requestor sfr = new sf_requestor();
            d_requestor dr = new d_requestor();

            public override IndividualRequestor<TypeSpec> VTableRequestor => vtr;
            public override IndividualRequestor<Layout.MethodSpecWithEhdr> MethodRequestor => mr;
            public override IndividualRequestor<Layout.MethodSpecWithEhdr> EHRequestor => ehr;
            public override IndividualRequestor<Layout.MethodSpecWithEhdr> BoxedMethodRequestor => bmr;
            public override IndividualRequestor<TypeSpec> StaticFieldRequestor => sfr;
            public override IndividualRequestor<TypeSpec> DelegateRequestor => dr;
        }

        class vt_requestor : IndividualRequestor<TypeSpec>
        {
            public override bool Empty => throw new NotImplementedException();

            public override TypeSpec GetNext()
            {
                throw new NotImplementedException();
            }

            public override void Remove(TypeSpec v)
            {
                throw new NotImplementedException();
            }

            public override void Request(TypeSpec v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "vt_requestor: request for " + v.MangleType());
            }
        }

        class eh_requestor : m_requestor
        {
            public override void Request(Layout.MethodSpecWithEhdr v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "eh_requestor: request for " + v.ms.MangleMethod());
            }
        }

        class m_requestor : IndividualRequestor<Layout.MethodSpecWithEhdr>
        {
            public override bool Empty => throw new NotImplementedException();

            public override Layout.MethodSpecWithEhdr GetNext()
            {
                throw new NotImplementedException();
            }

            public override void Remove(Layout.MethodSpecWithEhdr v)
            {
                throw new NotImplementedException();
            }

            public override void Request(Layout.MethodSpecWithEhdr v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "m_requestor: request for " + v.ms.MangleMethod());
            }
        }

        class bm_requestor : m_requestor
        {
            public override void Request(Layout.MethodSpecWithEhdr v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "bm_requestor: request for " + v.ms.MangleMethod());
            }
        }

        class sf_requestor : vt_requestor
        {
            public override void Request(TypeSpec v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "sf_requestor: request for " + v.MangleType());
            }
        }

        class d_requestor : vt_requestor
        {
            public override void Request(TypeSpec v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "d_requestor: request for " + v.MangleType());
            }
        }
    }
}
