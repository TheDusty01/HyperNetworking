using HyperNetworking.Core;
using HyperNetworking.Messaging.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace HyperNetworking.Messaging
{

    // Packet Type     | 1 byte  | 00000000
    // Packet Id       | 4 bytes | 00000000 00000000 00000000 00000000
    // Data Length     | 4 bytes | 00000000 00000000 00000000 00000000
    // Data            | x bytes |

    public class NetworkPacket
    {
        private readonly IPacketConverter packetConverter;

        public int PacketId { get; set; } = 0;
        public int DataLength { get; set; }
        public byte[]? Data { get; set; }

        public bool IsValid
        {
            get
            {
                return PacketId != 0 && Data != null && DataLength == Data.Length;
            }
        }

        public NetworkPacket(IPacketConverter packetConverter)
        {
            this.packetConverter = packetConverter;
        }

        public NetworkPacket(IPacketConverter packetConverter, Type type, object obj) : this(packetConverter)
        {
            PacketId = packetConverter.GetPacketId(type);
            Data = packetConverter.Serialize(obj);
            DataLength = Data.Length;
        }


        public void Read(BinaryReader br)
        {
            // TODO: Add some error checking
            PacketId = br.ReadInt32();
            DataLength = br.ReadInt32();
            Data = br.ReadBytes(DataLength);
        }

        public byte[] Serialize()
        {
            List<byte> packetData = new List<byte>();
            
            packetData.AddRange(BitConverter.GetBytes(PacketId));
            packetData.AddRange(BitConverter.GetBytes(DataLength));
            packetData.AddRange(Data);

            return packetData.ToArray();
        }

        public T? Deserialize<T>()
        {
            //return StaticPacketConverter.Deserialize<T>(Data);
            return packetConverter.Deserialize<T>(Data!);
        }

        public object? Deserialize(Type type)
        {
            //return StaticPacketConverter.Deserialize(type, Data);
            return packetConverter.Deserialize(type, Data!);
        }

        //public const int PacketTypePosition = 0;
        //public const int PacketTypeBytes = 1;

        //public const int PacketIdPosition = PacketTypeBytes;   // 1
        //public const int PacketIdBytes = 4;

        //public const int DataLengthPosition = PacketTypeBytes + PacketIdBytes; // 5
        //public const int DataLengthBytes = 4;

        //public const int DataPosition = PacketTypeBytes + PacketIdBytes + DataLengthBytes; // 9

        //private readonly List<byte> bytes = new List<byte>();
        //private readonly MemoryStream data;
        //private readonly BinaryWriter writer;

        //public int TotalLength => bytes.Count;

        //public NetPacket()
        //{
        //    data = new MemoryStream();
        //    writer = new BinaryWriter(data);
        //}

        //public void Write(byte[] buffer, int offset, int count)
        //{
        //    bytes.AddRange(buffer);
        //    data.Write(buffer, offset, count);
        //}

        //public void Write(byte data)
        //{
        //    bytes.Add(data);
        //}

        //public void Write(int data)
        //{
        //    bytes.AddRange(BitConverter.GetBytes(data));
        //}

        //public void Clear()
        //{
        //    bytes.Clear();
        //}

        //public bool IsFullyRead()
        //{
        //    int dataLength = GetDataLength();
        //    if (dataLength == -1)
        //        return false;

        //    return bytes.Count == DataPosition + dataLength;
        //}

        //public byte GetPacketType()
        //{
        //    if (bytes.Count < PacketTypePosition + PacketTypeBytes)
        //        return byte.MaxValue;

        //    return bytes[PacketTypePosition];
        //}

        //public int GetPacketId()
        //{
        //    if (bytes.Count < PacketIdPosition + PacketIdBytes)
        //        return -1;

        //    return BitConverter.ToInt32(bytes.GetRange(PacketIdPosition, PacketIdBytes).ToArray(), 0);
        //}

        //public int GetDataLength()
        //{
        //    if (bytes.Count < DataLengthPosition + DataLengthBytes)
        //        return -1;

        //    return BitConverter.ToInt32(bytes.GetRange(DataLengthPosition, DataLengthBytes).ToArray(), 0);
        //}

        //public byte[] GetData()
        //{
        //    int dataLength = GetDataLength();
        //    if (dataLength == -1 || bytes.Count != DataPosition + dataLength)
        //        return null;

        //    return bytes.GetRange(DataPosition, dataLength).ToArray();
        //}


    }
}
