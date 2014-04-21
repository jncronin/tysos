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

namespace tydb
{
    class var
    {
        internal static Dictionary<string, obj> vars = new Dictionary<string, obj>();

        internal static obj get_var(string name)
        {
            if (name.Contains("."))
            {
                // Composite value
                string[] names = name.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                obj o = get_var(names[0]);
                if (o == null)
                    return null;

                for (int i = 1; i < names.Length; i++)
                {
                    obj.field found = null;

                    if (o.value is obj.field[])
                    {
                        obj.field[] fs = o.value as obj.field[];

                        foreach (obj.field f in fs)
                        {
                            if (f.name == names[i])
                            {
                                found = f;
                                break;
                            }
                        }
                    }

                    if (found == null)
                    {
                        Console.WriteLine(o.type + " does not contain field " + names[i]);
                        return null;
                    }

                    if (found.is_vt)
                    {
                        obj new_o = new obj();
                        new_o.addr = o.addr + (ulong)found.offset;
                        new_o.is_vt = true;
                        new_o.type = found.type;
                        new_o.value = mem.get_mem(new_o.addr, (int)found.size);
                        o = new_o;
                    }
                    else
                        o = obj.get_obj((ulong)found.value);
                }

                return o;
            }

            if (vars.ContainsKey(name))
            {
                vars[name] = obj.get_obj(vars[name].addr);
                return vars[name];
            }

            if (name.StartsWith("[") && name.EndsWith("]"))
            {
                string reg_name = name.Substring(1, name.Length - 2);

                dbgarch.register r = Program.arch.get_reg(reg_name);
                if (r == null)
                    return null;

                ulong? r_val = await.get_register(new await.state(), r.id);
                if (r_val.HasValue)
                    return obj.get_obj(r_val.Value);
                else
                    return null;
            }

            if (name.StartsWith("*"))
            {
                string mem_addr = name.Substring(1);
                if (mem_addr.StartsWith("0x"))
                    mem_addr = mem_addr.Substring(2);

                try
                {
                    ulong m_addr_u = ulong.Parse(mem_addr, System.Globalization.NumberStyles.HexNumber);
                    return obj.get_obj(m_addr_u);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }
    }
}
