/* Copyright (C) 2016 by John Cronin
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
using libtysila5.util;
using metadata;

/* Implements a generic requestor to use for vtables and methods, and a
    default implementation that cache requests */
namespace libtysila5
{
    public abstract class Requestor
    {
        public abstract IndividualRequestor<TypeSpec> VTableRequestor { get; }
        public abstract IndividualRequestor<MethodSpec> MethodRequestor { get; }
        public abstract IndividualRequestor<MethodSpec> MethodSpecRequestor { get; }
        public abstract IndividualRequestor<MethodSpec> FieldSpecRequestor { get; }
        public abstract IndividualRequestor<TypeSpec> StaticFieldRequestor { get; }
    }

    public class CachingRequestor : Requestor
    {
        CachingIndividualRequestor<MethodSpec> m = new CachingIndividualRequestor<MethodSpec>();
        CachingIndividualRequestor<MethodSpec> ms = new CachingIndividualRequestor<MethodSpec>();
        CachingIndividualRequestor<MethodSpec> fs = new CachingIndividualRequestor<MethodSpec>();
        CachingIndividualRequestor<TypeSpec> vt = new CachingIndividualRequestor<TypeSpec>();
        CachingIndividualRequestor<TypeSpec> sf = new CachingIndividualRequestor<TypeSpec>();

        public override IndividualRequestor<MethodSpec> MethodRequestor
        {
            get
            {
                return m;
            }
        }

        public override IndividualRequestor<MethodSpec> MethodSpecRequestor
        {
            get
            {
                return ms;
            }
        }

        public override IndividualRequestor<MethodSpec> FieldSpecRequestor
        {
            get
            {
                return fs;
            }
        }

        public override IndividualRequestor<TypeSpec> VTableRequestor
        {
            get
            {
                return vt;
            }
        }

        public override IndividualRequestor<TypeSpec> StaticFieldRequestor
        {
            get
            {
                return sf;
            }
        }
    }

    public abstract class IndividualRequestor<T> where T : IEquatable<T>
    {
        public abstract T GetNext();
        public abstract bool Empty { get; }
        public abstract void Request(T v);
    }

    public class CachingIndividualRequestor<T> : IndividualRequestor<T> where T : class, IEquatable<T>
    {
        Set<T> done_and_pending = new Set<T>();
        util.Stack<T> pending = new util.Stack<T>();

        public override bool Empty
        {
            get
            {
                return pending.Count == 0;
            }
        }

        public override T GetNext()
        {
            return pending.Pop();
        }

        public override void Request(T v)
        {
            if(!done_and_pending.Contains(v))
            {
                done_and_pending.Add(v);
                pending.Push(v);
            }
        }
    }
}
