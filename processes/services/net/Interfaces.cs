using System;
using System.Collections.Generic;
using System.Text;
using static tysos.ServerObject;

namespace net
{
    [libsupcs.AlwaysInvoke]
    public interface INetworkDevice
    {
        RPCResult<bool> Start();
        RPCResult<bool> Stop();

        RPCResult<HWAddr> GetHardwareAddress();
        RPCResult<bool> TransmitPacket(byte[] packet, int dev_no, int packet_offset,
            int packet_len, p_addr dest);

        RPCResult<bool> RegisterDevNo(int dev_no);
    }

    [libsupcs.AlwaysInvoke]
    public interface INetInternal
    {
        RPCResult<bool> PacketReceived(byte[] packet, int dev_no, int payload_offset,
            int payload_len, p_addr devsrc);

        RPCResult<bool> RegisterPacketHandler(ushort id, IPacketHandler handler);
    }

    [libsupcs.AlwaysInvoke]
    public interface IPacketHandler
    {
        RPCResult<bool> PacketReceived(byte[] packet, int dev_no, int payload_offset,
            int payload_len, p_addr src);
    }

    [libsupcs.AlwaysInvoke]
    public interface IArp
    {
        RPCResult<bool> AnnounceDevice(int dev_no, ushort etype);
        RPCResult<HWAddr> ResolveAddress(int dev_no, p_addr addr);
    }
}
