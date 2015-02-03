using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmRobotControlAndCommunication
{
    enum e_MessageType : byte
    {
        MESSAGE_TYPE_HOST_REQUEST = 0x00,
        MESSAGE_TYPE_HOST_COMMAND = 0x01,
        MESSAGE_TYPE_ROBOT_REQUEST = 0x02,
        MESSAGE_TYPE_ROBOT_RESPONSE = 0x03,
        MESSAGE_TYPE_SMARTPHONE_REQUEST = 0x04,
        MESSAGE_TYPE_SMARTPHONE_COMMAND = 0x05
    }

    class SwarmMessageHeader
    {
        private e_MessageType eMessageType;
        private byte ui8Cmd;

        public SwarmMessageHeader(e_MessageType eMessType, byte cmd)
        {
            eMessageType = eMessType;
            ui8Cmd = cmd;
        }
        public e_MessageType getMessageType() { return eMessageType; }
        public byte getCmd() { return ui8Cmd; }
    }

    class SwarmMessage 
    {
        private SwarmMessageHeader header;
        private byte[] data;

        public SwarmMessage(SwarmMessageHeader header)
        {
            this.header = header;
            data = null;
        }
        public SwarmMessage(SwarmMessageHeader header, byte[] data)
        {
            this.header = header;
            if (data == null)
            {
                this.data = null;
            }
            else
            {
                this.data = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    this.data[i] = data[i];
                }
            }
        }
        public uint getSize()
        {
            if (data != null)
                return (uint)(sizeof(e_MessageType) + 1 + data.Length);
            else
                return (sizeof(e_MessageType) + 1);
                    
        }
        public byte[] toByteArray()
        {
            if (data != null)
            {
                byte[] output = new byte[sizeof(e_MessageType) + 1 + data.Length];

                output[0] = (byte)(header.getMessageType());
                output[1] = header.getCmd();
                for (int i = 0; i < data.Length; i++)
                {
                    output[i + 2] = data[i];
                }
                return output;
            }
            else 
            {
                byte[] output = new byte[sizeof(e_MessageType) + 1];

                output[0] = (byte)(header.getMessageType());
                output[1] = header.getCmd();

                return output;
            }
        }

        public static SwarmMessage ConstructFromByteArray(byte[] buffer)
        {
            if (buffer.Length < 2)
                return null;

            e_MessageType messType = (e_MessageType)Enum.ToObject(typeof(e_MessageType), buffer[0]);

            if (buffer.Length == 2)
            {
                return (new SwarmMessage(new SwarmMessageHeader(messType, buffer[1])));
            }
            else 
            {
                const byte HEADER_LENGTH = 2;
                byte[] data = new byte[buffer.Length - HEADER_LENGTH];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = buffer[i + HEADER_LENGTH];
                }
                return (new SwarmMessage(new SwarmMessageHeader(messType, buffer[1]), data));
            }
        }
        public SwarmMessageHeader getHeader()
        {
            return header;
        }
        
        public byte[] getData()
        {
            return data;
        }
    };
}
