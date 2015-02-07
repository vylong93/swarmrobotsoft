///-----------------------------------------------------------------------------
///	Swarm Robot Control & Communication Software developed by Le Binh Son
///		Email: lebinhson90@gmail.com
///
///	The USB HID Generic driver for C# is provided by Simon
///	  Web:    http://www.waitingforfriday.com
///	  Email:  simon.inns@gmail.com
///-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections;

// The following namespace allows debugging output (when compiled in debug mode)
using System.Diagnostics;

using System.IO;
using System.Threading;

using usbGenericHidCommunications;
using SwarmRobotControlAndCommunication.CustomInterfaces;

namespace SwarmRobotControlAndCommunication
{

    public class TM4C123ControlBoard : usbGenericHidCommunication, ControlBoardInterface
    {
        #region Definitions to communicate with the control board
        private const byte USB_BUFFER_LENGTH = 65;

        //------------------USB Commands definitions------------------//
        private const byte BOOTLOADER_BROADCAST_PACKET = 0x10;
        private const byte BOOTLOADER_SCAN_JAMMING = 0x11;

        private const byte USB_PACKET_SINGLE = 0x21;
        private const byte USB_PACKET_FIRST = 0x22;
        private const byte USB_PACKET_MIDDLE = 0x23;
        private const byte USB_PACKET_LAST = 0x24;

        private const byte USB_PACKET_MAX_SEGMENT_LENGTH = 56;
        //----------------------------------------//USB Commands definitions

        //--------------------Commands definitions--------------------//
        private const byte CONFIGURE_RF = 0x31;
        private const byte CONFIGURE_SPI = 0x32;

        private const byte TRANSMIT_DATA_TO_ROBOT = 0x33;
        private const byte TRANSMIT_DATA_TO_ROBOT_ACK = 0x34;
        private const byte RECEIVE_DATA_FROM_ROBOT = 0x35;
        private const byte RECEIVE_DATA_FROM_ROBOT_WITH_COMMAND = 0x36;
        //----------------------------------------//Commands definitions

        //----------------------ACK definitions----------------------//
        private const byte BOOTLOADER_BROADCAST_PACKET_DONE = 0x20;
        private const byte BOOTLOADER_BROADCAST_PACKET_FAILED = 0x21;
        private const byte BOOTLOADER_SCAN_JAMMING_CLEAR = 0x22;
        private const byte BOOTLOADER_SCAN_JAMMING_ASSERT = 0x23;

        private const byte USB_TRANSMIT_SEGMENT_DONE = 0x24;
        private const byte USB_TRANSMIT_SEGMENT_FAILED = 0x25;

        private const byte CONFIGURE_RF_OK = 0x26;
        private const byte CONFIGURE_SPI_OK = 0x27;
        private const byte CONFIGURE_RECEIVE_DATA_FAILED = 0x28;

        private const byte TRANSMIT_DATA_TO_ROBOT_DONE = 0xAA;
        private const byte TRANSMIT_DATA_TO_ROBOT_FAILED = 0xFA;

        private const byte RECEIVE_DATA_FROM_ROBOT_ERROR = 0xEE;
        private const byte RECEIVE_DATA_FROM_ROBOT_CONTINUE = 0xAE;

        public string failedToSendData = "Can't send data to the control board";
        public string failedToReadData = "No respone from the control board";
        public string numberOfDataOutOfRange = "The number of sent data is too high";
        public string invalidRespone = "Invalid response from the control board";
        //--------------------------------------------//ACK definitions

        //----------------------Parameter definitions----------------------//
        private const byte DELAY_AFTER_MAX_DATA_TRANSMITTED = 1;
        private const byte MAX_NUM_BYTE_TRANSMITTED = 16;
        private const byte MAX_NUM_BSL_PACKET_LENGTH_TRANSMITTED = 28;
        private const byte MAX_NUM_BYTE_RECEIVED = 16;
        private const byte USB_MAXIMUM_TRANSMISSION_LENGTH = 56;
        //--------------------------------------------//Parameter definitions

        #endregion

        public event usbDeviceChangeEventsHandler usbDeviceChangeEvent;

        public TM4C123ControlBoard(int vid, int pid)
            : base(vid, pid)
        {
        }

        protected override void onUsbEvent(EventArgs e)
        {
            if (usbDeviceChangeEvent != null)
            {
                usbDeviceChangeEvent(this, e);
            }
        }

        #region Control board Configuration API

        /// <summary>
        /// Configure the RF board of the control board
        /// Throw an exception if an error occurs.
        /// </summary>
        /// <param name="setupData">The data frame corresponding to the 
        /// data fram of the control board to configure the RF board</param>
        public void configureRF(byte[] setupData)
        {
            try
            {
                byte[] ConfigPacket = new byte[1 + setupData.Length];

                ConfigPacket[0] = CONFIGURE_RF;

                for (int i = 0; i < setupData.Length; i++)
                {
                    ConfigPacket[1 + i] = setupData[i];
                }

                sendPacketToControlBoard(ConfigPacket);

                isOperationFinishOk(CONFIGURE_RF_OK);
            }
            catch (Exception ex)
            {
                throw new Exception("Control Board Configure RF: " + ex.Message);
            }
        }

        /// <summary>
        /// Configure the SPI module of the control board.
        /// Thrown an exception if an error occurs
        /// </summary>
        /// <param name="setupData">The data frame corresponding to the SPI data frame
        /// of the control board to configure the SPI module</param>
        public void configureSPI(byte[] setupData)
        {
            throw new Exception("SPI configuration function has not been implemented yet");
        }

        /// <summary>
        /// Not implemented. Reserved for future use
        /// </summary>
        /// <param name="setupData"></param>
        public void configurePWM(byte[] setupData)
        {
            throw new Exception("PWM configuration function has not been implemented yet");
        }

        /// <summary>
        /// Not implemented. Reserved for future use
        /// </summary>
        /// <param name="setupData"></param>
        public void configureUART(byte[] setupData)
        {
            throw new Exception("UART configuration function has not been implemented yet");
        }

        #endregion

        #region Transmission Mode
        /// <summary>
        /// Not implemented. Reserved for future use
        /// </summary>
        /// <param name="transmissionModeSelected"></param>
        public void setTransmissionMode(byte transmissionModeSelected)
        {
            //TODO: three tranmission mode need to be consider
            // 1/ Bootloader mode: use for update robot's firmware
            // 2/ TCP mode: realiable transaction with robot (require the hanshake process:: ACK)
            // 3/ UDP mode: for broadcast / realtime transmission purpose. Segment drop is acceptable.
         
            throw new Exception("Set transmission mode function has not been implemented yet");
        }
        #endregion

        #region Bootloader API
        /// <summary>
        /// Transmit bootloader program packet to Robot
        /// <param name="transmittedData">The transmitted data</param>
        /// <param name="numberOfTransmittedBytes">Data length (1 - 2^32)</param>
        /// <param name="delayTimeBeforeSendResponeToPC">The delay time (ms) before the control board send the ok respone to PC.
        /// </summary>
        public void transmitBslPacketToRobot(byte[] transmittedData, UInt32 numberOfTransmittedBytes, byte delayTimeBeforeSendResponeToPC)
        {
            try
            {
                if (numberOfTransmittedBytes < 1)
                    throw new Exception("Data lengh must be larger than 1");

                Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
                Byte[] inputBuffer = new Byte[USB_BUFFER_LENGTH];
                UInt32 dataPointer = 0;
                byte bufferPointer = 0;
                byte dataLength = 0;
                byte delayTime = delayTimeBeforeSendResponeToPC;

                outputBuffer[0] = 0;
                outputBuffer[1] = BOOTLOADER_BROADCAST_PACKET;

                if (numberOfTransmittedBytes <= MAX_NUM_BSL_PACKET_LENGTH_TRANSMITTED)
                {
                    dataLength = (byte)(numberOfTransmittedBytes & 0xFF);
                }
                else
                {
                    throw new Exception("Bootloader Program Packet too large!");
                }

                outputBuffer[2] = dataLength;
                outputBuffer[3] = delayTime;

                for (bufferPointer = 0; bufferPointer < dataLength; bufferPointer++)
                {
                    outputBuffer[4 + bufferPointer] = transmittedData[dataPointer];
                    dataPointer++;
                }

                sendDataToControlBoard(outputBuffer);

                inputBuffer = readDataFromControlBoard();

                if (inputBuffer[1] == BOOTLOADER_BROADCAST_PACKET_DONE)
                    return;

                if (inputBuffer[1] == BOOTLOADER_BROADCAST_PACKET_FAILED)
                    throw new Exception("Operation Failed");
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Control Board: Broadcast bootloader packet: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// Scan for any jamming signal
        /// <param name="waitTime">The scaning period in ms</param>
        /// </summary>
        public bool tryToDetectJammingSignal(UInt32 waitTime)
        {
            byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
            byte[] inputBuffer = new Byte[USB_BUFFER_LENGTH];

            try
            {
                outputBuffer[0] = 0;
                outputBuffer[1] = BOOTLOADER_SCAN_JAMMING;

                outputBuffer[2] = (byte)((waitTime >> 24) & 0x0FF);
                outputBuffer[3] = (byte)((waitTime >> 16) & 0x0FF);
                outputBuffer[4] = (byte)((waitTime >> 8) & 0x0FF);
                outputBuffer[5] = (byte)(waitTime & 0x0FF);

                sendDataToControlBoard(outputBuffer);

                inputBuffer = readDataFromControlBoard();

                if (inputBuffer[1] == BOOTLOADER_SCAN_JAMMING_CLEAR)
                    return false;

                if (inputBuffer[1] == BOOTLOADER_SCAN_JAMMING_ASSERT)
                    return true;

                throw new Exception("Invalid response");
            }
            catch (Exception ex)
            {
                throw new Exception("Try receiving data: " + ex.Message);
            }
        }
        #endregion

        #region High Level Network API: communicating with Robots
        /// <summary>
        /// Broadcast a command to the robots
        /// </summary>
        /// <param name="cmd">The command which transmit to the robots</param>
        public void broadcastCommandToRobot(byte cmd)
        {
            SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, cmd);
            SwarmMessage message = new SwarmMessage(header);

            broadcastMessageToRobot(message);
        }

        /// <summary>
        /// Broadcast a message to the robots
        /// </summary>
        /// <param name="message">The transmitted message</param>
        public void broadcastMessageToRobot(SwarmMessage message)
        {
            transmitSwarmMessage(TRANSMIT_DATA_TO_ROBOT, message);
        }
        public void broadcastDataToRobot(byte[] data)
        {
            transmitDataToRobot(TRANSMIT_DATA_TO_ROBOT, data);
        }

        /// <summary>
        /// Transfer a message to robot. This require acknowledgement response from the robot.
        /// </summary>
        /// <param name="message">The transmitted message</param>
        public bool sendMessageToRobot(SwarmMessage message)
        {
            return transmitSwarmMessage(TRANSMIT_DATA_TO_ROBOT_ACK, message);
        }
        public bool sendDataToRobot(byte[] data)
        {
            return transmitDataToRobot(TRANSMIT_DATA_TO_ROBOT_ACK, data);
        }

        /// <summary>
        /// Try receiving data from target through the control board 
        /// without transmitting command and data length to other devices.
        /// The received data length is limited to 32 bytes.
        /// </summary>
        /// <return> False if no data is received. Otherwise, return True </return>
        /// <param name="responseDataBuffer">The buffer to hold the received data</param>
        /// <param name="requestReceivedLength">The length of the expected data (NOT transmitted to the target)</param>
        /// <param name="delayResponse">The waiting time for a packet to be received</param>
        public bool tryToReceivedDataFromRobot(byte[] responseDataBuffer, UInt32 requestReceivedLength, UInt32 delayResponse)
        {
            try
            {
                byte[] configPacket = new byte[1 + 4 + 4];

                fillConfigPacketHeader(configPacket, RECEIVE_DATA_FROM_ROBOT, requestReceivedLength, delayResponse);

                sendPacketToControlBoard(configPacket);

                startReceivingDataFromRobot(requestReceivedLength, responseDataBuffer);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("tryToReceivedDataFromRobot: " + ex.Message);
            }
        }

        /// <summary>
        /// Receive data from target through the control board.
        /// The received data length is limited to 2^32.
        /// </summary>
        /// <return> An exception if any errors occur or the number of received data is not enough</return>
        /// <param name="responseDataBuffer">The buffer to hold the received data</param>
        /// <param name="requestReceivedLength">The length of the expected data (also transmitted to the target)</param>
        /// <param name="delayResponse">The waiting time for a packet to be received</param>
        /// /// <param name="requestMessage">The command to specify what data should the target transmits to the PC</param>
        public bool receivedDataFromRobot(byte[] responseDataBuffer, UInt32 requestReceivedLength, UInt32 delayResponse, SwarmMessage requestMessage)
        {
            try
            {
                UInt32 messageSize = requestMessage.getSize();
                byte[] messageBuffer = requestMessage.toByteArray();
                byte[] configPacket = new byte[1 + 4 + 4 + 4 + messageSize];

                fillConfigPacketHeader(configPacket, RECEIVE_DATA_FROM_ROBOT_WITH_COMMAND, requestReceivedLength, delayResponse);

                configPacket[9] = (byte)((messageSize >> 24) & 0x0FF);
                configPacket[10] = (byte)((messageSize >> 16) & 0x0FF);
                configPacket[11] = (byte)((messageSize >> 8) & 0x0FF);
                configPacket[12] = (byte)(messageSize & 0x0FF);

                for (int i = 0; i < messageSize; i++)
                {
                    configPacket[13 + i] = messageBuffer[i];
                }

                sendPacketToControlBoard(configPacket);

                startReceivingDataFromRobot(requestReceivedLength, responseDataBuffer);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("receivedDataFromRobot: " + ex.Message);
            }
        }

        private void fillConfigPacketHeader(byte[] packet, byte command, UInt32 requestLength, UInt32 waitTime)
        {
            packet[0] = command;

            packet[1] = (byte)((requestLength >> 24) & 0x0FF);
            packet[2] = (byte)((requestLength >> 16) & 0x0FF);
            packet[3] = (byte)((requestLength >> 8) & 0x0FF);
            packet[4] = (byte)(requestLength & 0x0FF);

            packet[5] = (byte)((waitTime >> 24) & 0x0FF);
            packet[6] = (byte)((waitTime >> 16) & 0x0FF);
            packet[7] = (byte)((waitTime >> 8) & 0x0FF);
            packet[8] = (byte)(waitTime & 0x0FF);
        }
        #endregion

        #region Low Level Link API: handle robot message transmission
        private bool transmitDataToRobot(byte cmd, byte[] data)
        {
            try
            {
                byte[] dataPacket = new byte[1 + 5 + data.Length];

                dataPacket[0] = cmd;

                dataPacket[1] = (byte)((data.Length >> 24) & 0x0FF);
                dataPacket[2] = (byte)((data.Length >> 16) & 0x0FF);
                dataPacket[3] = (byte)((data.Length >> 8) & 0x0FF);
                dataPacket[4] = (byte)(data.Length & 0x0FF);

                for (int i = 0; i < data.Length; i++)
                {
                    dataPacket[5 + i] = data[i];
                }

                if (sendPacketToControlBoard(dataPacket))
                {
                    return isUsbResponseOK(TRANSMIT_DATA_TO_ROBOT_DONE, TRANSMIT_DATA_TO_ROBOT_FAILED);
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("transmitSwarmMessage: " + ex.Message);
            }
        }
        private bool transmitSwarmMessage(byte cmd, SwarmMessage message)
        {
            try
            {
                UInt32 messageSize = message.getSize();
                byte[] messageBuffer = message.toByteArray();

                byte[] messagePacket = new byte[1 + 5 + messageSize];

                messagePacket[0] = cmd;

                messagePacket[1] = (byte)((messageSize >> 24) & 0x0FF);
                messagePacket[2] = (byte)((messageSize >> 16) & 0x0FF);
                messagePacket[3] = (byte)((messageSize >> 8) & 0x0FF);
                messagePacket[4] = (byte)(messageSize & 0x0FF);

                for (int i = 0; i < messageSize; i++)
                {
                    messagePacket[5 + i] = messageBuffer[i];
                }

                if (sendPacketToControlBoard(messagePacket))
                {
                    return isUsbResponseOK(TRANSMIT_DATA_TO_ROBOT_DONE, TRANSMIT_DATA_TO_ROBOT_FAILED);
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("transmitSwarmMessage: " + ex.Message);
            }
        }
        private void isOperationFinishOk(byte successFlag)
        {
            Byte[] inputBuffer = new Byte[USB_BUFFER_LENGTH];
            inputBuffer = readDataFromControlBoard();

            if (inputBuffer[1] != successFlag) throw new Exception(invalidRespone);
        }
        private void startReceivingDataFromRobot(UInt32 numberOfReceivedBytes, byte[] dataBuffer)
        {
            UInt32 length = 0;
            byte[] outputBuffer = new Byte[65];
            byte[] inputBuffer = new Byte[65];
            UInt32 pointer = 0;

            outputBuffer[1] = RECEIVE_DATA_FROM_ROBOT_CONTINUE;

            while (true)
            {
                inputBuffer = readDataFromControlBoard();

                checkIfReceivedDataFromRobot(inputBuffer);

                if (numberOfReceivedBytes > USB_MAXIMUM_TRANSMISSION_LENGTH)
                    length = USB_MAXIMUM_TRANSMISSION_LENGTH;
                else
                    length = numberOfReceivedBytes;

                for (UInt32 j = 1; j <= length; j++)
                {
                    dataBuffer[pointer] = inputBuffer[j];
                    pointer++;
                }

                numberOfReceivedBytes -= length;
                if (numberOfReceivedBytes <= 0)
                    break;

                sendDataToControlBoard(outputBuffer);
            }
        }
        void checkIfReceivedDataFromRobot(byte[] inputBuffer)
        {
            if (inputBuffer[USB_MAXIMUM_TRANSMISSION_LENGTH + 1] == RECEIVE_DATA_FROM_ROBOT_ERROR)
                throw new Exception("Did not receive data from robot");
        }
        #endregion

        #region USB protocol for Control Board API
        private bool sendPacketToControlBoard(byte[] PacketBuffer)
        {
            try
            {
                if (PacketBuffer.Length <= USB_PACKET_MAX_SEGMENT_LENGTH)
                {
                    return transmitSingleSegment(PacketBuffer);
                }
                else
                {
                    return transmitMultiSegments(PacketBuffer);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("sendPacketToControlBoard: " + ex.Message);
            }
        }
        private bool transmitSingleSegment(byte[] PacketBuffer)
        {
            Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
            outputBuffer[0] = 0;
            outputBuffer[1] = USB_PACKET_SINGLE;

            outputBuffer[2] = (byte)((PacketBuffer.Length >> 24) & 0x0FF);
            outputBuffer[3] = (byte)((PacketBuffer.Length >> 16) & 0x0FF);
            outputBuffer[4] = (byte)((PacketBuffer.Length >> 8) & 0x0FF);
            outputBuffer[5] = (byte)(PacketBuffer.Length & 0x0FF);

            for (int i = 0; i < PacketBuffer.Length; i++)
            {
                outputBuffer[6 + i] = PacketBuffer[i]; 
            }

            sendDataToControlBoard(outputBuffer);

            return isUsbResponseOK(USB_TRANSMIT_SEGMENT_DONE, USB_TRANSMIT_SEGMENT_FAILED);
        }
        private bool transmitMultiSegments(byte[] PacketBuffer)
        {
            int dataPointer;
            int remaindPacketLength;

            Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
            outputBuffer[0] = 0;
            outputBuffer[1] = USB_PACKET_FIRST;

            outputBuffer[2] = (byte)((PacketBuffer.Length >> 24) & 0x0FF);
            outputBuffer[3] = (byte)((PacketBuffer.Length >> 16) & 0x0FF);
            outputBuffer[4] = (byte)((PacketBuffer.Length >> 8) & 0x0FF);
            outputBuffer[5] = (byte)(PacketBuffer.Length & 0x0FF);

            dataPointer = 0;
            for (int i = 0; i < USB_PACKET_MAX_SEGMENT_LENGTH; i++)
            {
                outputBuffer[6 + i] = PacketBuffer[dataPointer++];
            }
            remaindPacketLength = PacketBuffer.Length - dataPointer;

            sendDataToControlBoard(outputBuffer);

            if (!isUsbResponseOK(USB_TRANSMIT_SEGMENT_DONE, USB_TRANSMIT_SEGMENT_FAILED))
                return false;

            while (true)
            {
                if (remaindPacketLength <= USB_PACKET_MAX_SEGMENT_LENGTH)
                {
                    outputBuffer[1] = USB_PACKET_LAST;

                    for (int i = 0; i < remaindPacketLength; i++)
                    {
                        outputBuffer[2 + i] = PacketBuffer[dataPointer++];
                    }

                    sendDataToControlBoard(outputBuffer);

                    return isUsbResponseOK(USB_TRANSMIT_SEGMENT_DONE, USB_TRANSMIT_SEGMENT_FAILED);
                }
                else
                {
                    outputBuffer[1] = USB_PACKET_MIDDLE;

                    for (int i = 0; i < USB_PACKET_MAX_SEGMENT_LENGTH; i++)
                    {
                        outputBuffer[2 + i] = PacketBuffer[dataPointer++];
                    }

                    sendDataToControlBoard(outputBuffer);

                    if (isUsbResponseOK(USB_TRANSMIT_SEGMENT_DONE, USB_TRANSMIT_SEGMENT_FAILED))
                    {
                        remaindPacketLength = PacketBuffer.Length - dataPointer;
                    }
                    else
                        return false;
                }
            }
        }
        private bool isUsbResponseOK(byte successFlag, byte failedFlag)
        {
            Byte[] inputBuffer = new Byte[USB_BUFFER_LENGTH];
            inputBuffer = readDataFromControlBoard();

            if (inputBuffer[1] == successFlag)
                return true;

            if (inputBuffer[1] == failedFlag)
                return false;

            throw new Exception(invalidRespone);
        }
        private bool isUsbResponse(byte responseFlag)
        {
            Byte[] inputBuffer = new Byte[USB_BUFFER_LENGTH];
            inputBuffer = readDataFromControlBoard();

            if (inputBuffer[1] == responseFlag)
                return true;
            return false;
        }

        /// <summary>
        /// Send data to the control board. 
        /// If the data length is larger than USB_BUFFER_LENGTH or an error
        /// occurs then an exception is thrown.
        /// </summary>
        /// <param name="data">Data to be sent to the control board</param>
        private void sendDataToControlBoard(byte[] data)
        {
            if (data.Length > USB_BUFFER_LENGTH) throw new Exception(numberOfDataOutOfRange);

            Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
            for (uint i = 0; i < data.Length; i++)
            {
                outputBuffer[i] = data[i];
            }

            bool success = writeRawReportToDevice(outputBuffer);
            if (!success) throw new Exception(failedToSendData);
        }

        /// <summary>
        /// Read data from the control board.
        /// Thrown an exception if an error occurs.
        /// </summary>
        /// <returns>The received data</returns>
        private byte[] readDataFromControlBoard()
        {
            Byte[] inputBuffer = new Byte[USB_BUFFER_LENGTH];

            bool success = readSingleReportFromDevice(ref inputBuffer);
            if (!success) throw new Exception(failedToReadData);
            return inputBuffer;
        }
        #endregion
    }
}
