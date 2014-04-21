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
using System.IO;

namespace tydb
{
    /* Interface with the Microsoft CciMetadata library */

    class CciInterface
    {
        object pdb;
        object tok_dev;

        Dictionary<uint, Dictionary<uint, string>> cached_functions = new Dictionary<uint, Dictionary<uint, string>>();

        static bool bound_interfaces = false;

        static System.Reflection.ConstructorInfo ctor_DefaultHost;
        static System.Reflection.ConstructorInfo ctor_PdbReader_Stream_IMetadataHost;
        static System.Reflection.MethodInfo meth_MetadataReaderHost_LoadUnitFrom_string;
        static System.Reflection.MethodInfo meth_ITokenDecoder_GetObjectForToken_uint;
        static System.Reflection.PropertyInfo prop_IMethodDefinition_Body;
        static System.Reflection.PropertyInfo prop_IMethodBody_Operations;
        static System.Reflection.PropertyInfo prop_IOperation_Offset;
        static System.Reflection.MethodInfo meth_PdbReader_GetPrimarySourceLocationsFor_ILocation;
        static System.Reflection.PropertyInfo prop_ISourceLocation_Source;

        static System.Type type_MetadataReaderHost;
        static System.Type type_PeReader_DefaultHost;
        static System.Type type_PdbReader;
        static System.Type type_ITokenDecoder;
        static System.Type type_IMetadataHost;
        static System.Type type_IMethodDefinition;
        static System.Type type_IMethodBody;
        static System.Type type_IOperation;
        static System.Type type_IPrimarySourceLocation;
        static System.Type type_ISourceLocation;
        static System.Type type_ILocation;

        static CciInterface()
        {
            // Attempt to dynamically bind the CciMetadata library

            try
            {
                System.Reflection.Assembly mod_mh = LoadAssembly("Microsoft.Cci.MetadataHelper.dll");
                System.Reflection.Assembly mod_mm = LoadAssembly("Microsoft.Cci.MetadataModel.dll");
                System.Reflection.Assembly mod_pdb = LoadAssembly("Microsoft.Cci.PdbReader.dll");
                System.Reflection.Assembly mod_pe = LoadAssembly("Microsoft.Cci.PeReader.dll");
                System.Reflection.Assembly mod_sm = LoadAssembly("Microsoft.Cci.SourceModel.dll");

                type_MetadataReaderHost = mod_mh.GetType("Microsoft.Cci.MetadataReaderHost");
                type_PeReader_DefaultHost = mod_pe.GetType("Microsoft.Cci.PeReader+DefaultHost");
                type_PdbReader = mod_pdb.GetType("Microsoft.Cci.PdbReader");
                type_ITokenDecoder = mod_mm.GetType("Microsoft.Cci.ITokenDecoder");
                type_IMetadataHost = mod_mm.GetType("Microsoft.Cci.IMetadataHost");
                type_IMethodDefinition = mod_mm.GetType("Microsoft.Cci.IMethodDefinition");
                type_IMethodBody = mod_mm.GetType("Microsoft.Cci.IMethodBody");
                type_IOperation = mod_mm.GetType("Microsoft.Cci.IOperation");
                type_IPrimarySourceLocation = mod_sm.GetType("Microsoft.Cci.IPrimarySourceLocation");
                type_ISourceLocation = mod_sm.GetType("Microsoft.Cci.ISourceLocation");
                type_ILocation = mod_mm.GetType("Microsoft.Cci.ILocation");

                ctor_DefaultHost = type_PeReader_DefaultHost.GetConstructor(new Type[] { });
                ctor_PdbReader_Stream_IMetadataHost = type_PdbReader.GetConstructor(new Type[] { typeof(Stream), type_IMetadataHost });
                meth_MetadataReaderHost_LoadUnitFrom_string = type_MetadataReaderHost.GetMethod("LoadUnitFrom", new Type[] { typeof(string) });
                meth_ITokenDecoder_GetObjectForToken_uint = type_ITokenDecoder.GetMethod("GetObjectForToken", new Type[] { typeof(uint) });
                prop_IMethodDefinition_Body = type_IMethodDefinition.GetProperty("Body");
                prop_IMethodBody_Operations = type_IMethodBody.GetProperty("Operations");
                prop_IOperation_Offset = type_IOperation.GetProperty("Offset");
                meth_PdbReader_GetPrimarySourceLocationsFor_ILocation = type_PdbReader.GetMethod("GetPrimarySourceLocationsFor", new Type[] { type_ILocation });
                prop_ISourceLocation_Source = type_ISourceLocation.GetProperty("Source");

                bound_interfaces = true;
            }
            catch (FileNotFoundException)
            {
                bound_interfaces = false;
            }
        }

        public static bool LibraryFound { get { return bound_interfaces; } }

        private static System.Reflection.Assembly LoadAssembly(string mod_name)
        {
            System.Reflection.Assembly ret =  System.Reflection.Assembly.LoadFrom(mod_name);
            if (ret == null)
                throw new FileLoadException("Unable to load assembly " + mod_name);
            return ret;
        }

        public CciInterface(string pefilename)
        {
            object dh = ctor_DefaultHost.Invoke(new object[] { });
            pdb = ctor_PdbReader_Stream_IMetadataHost.Invoke(new object[] { new FileStream(Path.ChangeExtension(pefilename, "pdb"), FileMode.Open), dh });
            tok_dev = meth_MetadataReaderHost_LoadUnitFrom_string.Invoke(dh, new object[] { pefilename });
        }

        public string GetSourceLineFromToken(uint method_token, uint il_offset)
        {
            Dictionary<uint, string> func = GetFunction(method_token);
            if (func.ContainsKey(il_offset))
                return func[il_offset];
            return null;
        }

        private Dictionary<uint, string> GetFunction(uint method_token)
        {
            if (cached_functions.ContainsKey(method_token))
                return cached_functions[method_token];

            object tok = meth_ITokenDecoder_GetObjectForToken_uint.Invoke(tok_dev, new object[] { method_token });
            if (!type_IMethodDefinition.IsAssignableFrom(tok.GetType()))
                return new Dictionary<uint, string>();

            object body = prop_IMethodDefinition_Body.GetValue(tok, null);
            IEnumerable<object> ops = prop_IMethodBody_Operations.GetValue(body, null) as IEnumerable<object>;

            Dictionary<uint, string> ret = new Dictionary<uint, string>();

            foreach (object op in ops)
            {
                uint offset = (uint)prop_IOperation_Offset.GetValue(op, null);
                if (!ret.ContainsKey(offset))
                {
                    List<object> pslocs_list = new List<object>(meth_PdbReader_GetPrimarySourceLocationsFor_ILocation.Invoke(pdb, new object[] { op }) as IEnumerable<object>);
                    if (pslocs_list.Count > 0)
                        ret[offset] = prop_ISourceLocation_Source.GetValue(pslocs_list[0], null) as string;
                }
            }

            cached_functions[method_token] = ret;
            return ret;
        }
    }
}
