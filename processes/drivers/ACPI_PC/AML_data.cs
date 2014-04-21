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
using System.Diagnostics;

namespace aml_interpreter
{
    partial class AML
    {
        static object ReadData(AMLData.Resource_Access resource)
        {
            if (resource.RegionSpace == OpRegion.RegionSpaceType.Indexed)
            {
                WriteData(resource.IndexResource, resource.ByteIndex);
                return ReadData(resource.ValueResource);
            }

            switch (resource.RegionSpace)
            {
                case OpRegion.RegionSpaceType.SystemIO:
                    {
                        switch (resource.AccessType)
                        {
                            case Field.AccessTypeType.ByteAcc:
                                return (long)libsupcs.IoOperations.PortInb((ushort)resource.ByteIndex);
                            case Field.AccessTypeType.WordAcc:
                                return (long)libsupcs.IoOperations.PortInw((ushort)resource.ByteIndex);
                            case Field.AccessTypeType.DWordAcc:
                                return (long)libsupcs.IoOperations.PortInd((ushort)resource.ByteIndex);
                            default:
                                throw new Exception("Invalid AccessType (" + resource.AccessType.ToString() + ") in accessing port " + resource.ByteIndex.ToString("X"));
                        }
                    }
            }

            return (long)0x0;
        }

        static void WriteData(AMLData.Resource_Access resource, object data)
        {
            if (resource.RegionSpace == OpRegion.RegionSpaceType.Indexed)
            {
                WriteData(resource.IndexResource, resource.ByteIndex);
                WriteData(resource.ValueResource, data);
            }

            switch (resource.RegionSpace)
            {
                case OpRegion.RegionSpaceType.SystemIO:
                    {
                        switch (resource.AccessType)
                        {
                            case Field.AccessTypeType.ByteAcc:
                                libsupcs.IoOperations.PortOut((ushort)resource.ByteIndex, (byte)data);
                                return;
                            case Field.AccessTypeType.WordAcc:
                                libsupcs.IoOperations.PortOut((ushort)resource.ByteIndex, (ushort)data);
                                return;
                            case Field.AccessTypeType.DWordAcc:
                                libsupcs.IoOperations.PortOut((ushort)resource.ByteIndex, (uint)data);
                                return;
                            default:
                                throw new Exception("Invalid AccessType (" + resource.AccessType.ToString() + ") in accessing port " + resource.ByteIndex.ToString("X"));
                        }
                    }
            }
        }
    }
}
