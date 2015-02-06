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

        //--------------------Commands definitions--------------------//
        private const byte BOOTLOADER_BROADCAST_PACKET = 0x10;
        private const byte BOOTLOADER_SCAN_JAMMING = 0x11;

        private const byte TRANSMIT_DATA_TO_ROBOT = 0x12;
        private const byte TRANSMIT_DATA_TO_ROBOT_ACK = 0x13;
        private const byte RECEIVE_DATA_FROM_ROBOT = 0x14;
        private const byte RECEIVE_DATA_FROM_ROBOT_WITH_COMMAND = 0x15;

        private const byte CONFIGURE_RF = 0x16;
        private const byte CONFIGURE_SPI = 0x17;
        //----------------------------------------//Commands definitions

        //----------------------ACK definitions----------------------//
        private const byte BOOTLOADER_BROADCAST_PACKET_DONE = 0x20;
        private const byte BOOTLOADER_BROADCAST_PACKET_FAILED = 0x21;
        private const byte BOOTLOADER_SCAN_JAMMING_CLEAR = 0x22;
        private const byte BOOTLOADER_SCAN_JAMMING_ASSERT = 0x23;

        private const byte CONFIGURE_RF_OK = 0x24;
        private const byte CONFIGURE_SPI_OK = 0x25;

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
                Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
                outputBuffer[0] = 0;
                outputBuffer[1] = CONFIGURE_RF;
                for (int i = 0; i < setupData.Length; i++)
                {
                    outputBuffer[2 + i] = setupData[i];
                }
                sendDataToControlBoard(outputBuffer);

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
        public void sendCommandToRobot(byte cmd)
        {
            SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, cmd);
            SwarmMessage message = new SwarmMessage(header);

            transmitBytesToRobot(message.toByteArray(), message.getSize(), 0);
        }
        #endregion

        #region Low Level Link API: handle robot transmission
        
        /// <summary>
        /// Transfer a number of bytes from the control board to targets.
        /// Only data is transmitted. numberOfTransmittedBytes and delay time are not sent to the robot.
        /// </summary>
        /// <param name="transmittedData">The transmitted data</param>
        /// <param name="numberOfTransmittedBytes">Data length (1 - 2^32)</param>
        /// <param name="delayTimeBeforeSendResponeToPC">The delay time (ms) before the control board send the ok respone to PC.
        /// This parameter can be used when we need to wait for the target to finish a certain action before sending the next command or data.
        /// </param>
        public void transmitBytesToRobot(byte[] transmittedData, UInt32 numberOfTransmittedBytes, byte delayTimeBeforeSendResponeToPC)
        {
            try
            {
                if (numberOfTransmittedBytes < 1)
                    throw new Exception("Data lengh must be larger than 1");

                Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
                UInt32 dataPointer = 0;
                byte bufferPointer = 0;
                byte dataLength = 0;
                byte delayTime = DELAY_AFTER_MAX_DATA_TRANSMITTED;

                outputBuffer[0] = 0;
                outputBuffer[1] = TRANSMIT_DATA_TO_ROBOT;

                while (true)
                {
                    calculateRemainedDataLength(ref numberOfTransmittedBytes, ref dataLength);

                    if (isEndOfSentData(numberOfTransmittedBytes))
                        delayTime = delayTimeBeforeSendResponeToPC;

                    outputBuffer[2] = dataLength;
                    outputBuffer[3] = delayTime;

                    for (bufferPointer = 0; bufferPointer < dataLength; bufferPointer++)
                    {
                        outputBuffer[4 + bufferPointer] = transmittedData[dataPointer];
                        dataPointer++;
                    }

                    sendDataToControlBoard(outputBuffer);

                    isOperationFinishOk(TRANSMIT_DATA_TO_ROBOT_DONE, TRANSMIT_DATA_TO_ROBOT_FAILED);

                    if (isEndOfSentData(numberOfTransmittedBytes))
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Control Board: Transmit {0} bytes to device: \n", numberOfTransmittedBytes) + ex.Message);
            }
        }
        public void transmitBytesToRobot(byte transmittedData)
        {
            byte numberOfTransmittedBytes = 1;
            byte delayTimeBeforeSendResponeToPC = 0;

            try
            {
                Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];

                outputBuffer[0] = 0;
                outputBuffer[1] = TRANSMIT_DATA_TO_ROBOT;
                outputBuffer[2] = numberOfTransmittedBytes;
                outputBuffer[3] = delayTimeBeforeSendResponeToPC;
                outputBuffer[4] = transmittedData;

                sendDataToControlBoard(outputBuffer);

                isOperationFinishOk(TRANSMIT_DATA_TO_ROBOT_DONE);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Control Board: Transmit {0} bytes to device: \n", numberOfTransmittedBytes) + ex.Message);
            }
        }

        /// <summary>
        /// Transfer a number of bytes from the control board to targets with require ack response
        /// Only data is transmitted. numberOfTransmittedBytes and delay time are not sent to the robot.
        /// </summary>
        /// <param name="transmittedData">The transmitted data</param>
        /// <param name="numberOfTransmittedBytes">Data length (1 - 2^32)</param>
        /// <param name="delayTimeBeforeSendResponeToPC">The delay time (ms) before the control board send the ok respone to PC.
        /// This parameter can be used when we need to wait for the target to finish a certain action before sending the next command or data.
        /// </param>
        public void transmitBytesToRobotWithACK(byte[] transmittedData, UInt32 numberOfTransmittedBytes, byte delayTimeBeforeSendResponeToPC)
        {
            //TODO: implement
        }

        /// <summary>
        /// Receive data from target through the control board.
        /// The received data length is limited to 2^32.
        /// </summary>
        /// <return> An exception if any errors occur or the number of received data is not enough</return>
        /// <param name="command">The command to specify what data should the target transmits to the PC</param>
        /// <param name="dataLength">The length of the expected data (also transmitted to the target)</param>
        /// <param name="data">The buffer to hold the received data</param>
        /// <param name="waitTime">The waiting time for a packet to be received</param>
        public void receiveBytesFromRobot(byte command, byte[] commandContent, UInt32 dataLength, ref byte[] data, UInt32 waitTime)
        {
            try
            {
                if (dataLength < 1)
                    throw new Exception("Data length must be larger than 1");

                setupControlBoardBeforeReceivingData(command, commandContent, RECEIVE_DATA_FROM_ROBOT_WITH_COMMAND, dataLength, waitTime);
                startReceivingData(ref dataLength, ref data);
            }
            catch (Exception ex)
            {
                throw new Exception("Received data: " + ex.Message + "\n" +
                                    "Number of data left: " + dataLength.ToString());
            }
        }
        public void receiveBytesFromRobot(UInt32 dataLength, ref byte[] data, UInt32 waitTime)
        {
            try
            {
                if (dataLength < 1)
                    throw new Exception("Data length must be larger than 1");

                setupControlBoardBeforeReceivingData(0, null, RECEIVE_DATA_FROM_ROBOT, dataLength, waitTime);
                startReceivingData(ref dataLength, ref data);
            }
            catch (Exception ex)
            {
                throw new Exception("Received data: " + ex.Message + "\n" +
                                    "Number of data left: " + dataLength.ToString());
            }
        }

        /// <summary>
        /// Try receiving data from target through the control board 
        /// without transmitting command and data length to other devices.
        /// The received data length is limited to 32 bytes.
        /// </summary>
        /// <return> False if no data is received. Otherwise, return True </return>
        /// <param name="dataLength">The length of the expected data (NOT transmitted to the target)</param>
        /// <param name="data">The buffer to hold the received data</param>
        /// <param name="waitTime">The waiting time for a packet to be received</param>
        public bool tryReceiveBytesFromRobot(UInt32 dataLength, ref byte[] data, UInt32 waitTime)
        {
            UInt32 length = 0;
            byte[] outputBuffer = new Byte[65];
            byte[] inputBuffer = new Byte[65];
            UInt32 pointer = 0;

            try
            {
                if (dataLength < 1)
                    throw new Exception("Data length must be larger than 1");
                if (dataLength > MAX_NUM_BYTE_RECEIVED)
                {
                    String message = String.Format("Data length must be smaller than {0}", MAX_NUM_BYTE_RECEIVED);
                    throw new Exception(message);
                }

                setupControlBoardBeforeReceivingData(0, null, RECEIVE_DATA_FROM_ROBOT, dataLength, waitTime);

                inputBuffer = readDataFromControlBoard();
                if (inputBuffer[USB_MAXIMUM_TRANSMISSION_LENGTH + 1] == RECEIVE_DATA_FROM_ROBOT_ERROR)
                    return false;

                for (UInt32 j = 1; j <= length; j++)
                {
                    data[pointer++] = inputBuffer[j];
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Try receiving data: " + ex.Message);
            }
        }

        void calculateRemainedDataLength(ref UInt32 numberOfTransmittedBytes, ref byte dataLength)
        {
            if (numberOfTransmittedBytes <= MAX_NUM_BYTE_TRANSMITTED)
            {
                dataLength = (byte)(numberOfTransmittedBytes & 0xFF);
                numberOfTransmittedBytes = 0;
            }
            else
            {
                dataLength = MAX_NUM_BYTE_TRANSMITTED;
                numberOfTransmittedBytes -= dataLength;
            }
        }
        bool isEndOfSentData(UInt32 remainedDataLength)
        {
            if (remainedDataLength == 0)
                return true;

            return false;
        }
        private void isOperationFinishOk(byte successFlag)
        {
            Byte[] inputBuffer = new Byte[USB_BUFFER_LENGTH];
            inputBuffer = readDataFromControlBoard();

            if (inputBuffer[1] != successFlag) throw new Exception(invalidRespone);
        }
        private void isOperationFinishOk(byte successFlag, byte failedFlag)
        {
            Byte[] inputBuffer = new Byte[USB_BUFFER_LENGTH];
            inputBuffer = readDataFromControlBoard();

            if (inputBuffer[1] == successFlag)
                return;

            if (inputBuffer[1] == failedFlag)
                throw new Exception("Operation Failed");

            throw new Exception(invalidRespone);
        }
        private void isOperationFinishOk(byte successFlag, byte noResponeFlag, byte invalidFlag)
        {
            Byte[] inputBuffer = new Byte[USB_BUFFER_LENGTH];
            inputBuffer = readDataFromControlBoard();

            if (inputBuffer[1] == successFlag)
                return;

            if (inputBuffer[1] == noResponeFlag)
                throw new Exception("No respone from robot");

            if (inputBuffer[1] == invalidFlag)
                throw new Exception("Invalid respone from robot");

            throw new Exception(invalidRespone);
        }
        private void setupControlBoardBeforeReceivingData(byte command, byte[] commandContent, byte receiveMode, UInt32 numberOfReceivedBytes, UInt32 waitTime)
        {
            const int COMMAND_HEADER_LENGTH = 11;
            SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, command);   
            SwarmMessage message = new SwarmMessage(header, commandContent);

            if (message.getSize() > (65 - COMMAND_HEADER_LENGTH))
            {
                throw new Exception("setupControlBoardBeforeReceivingData() message size to large!!!");
            }

            byte[] outputBuffer = new Byte[65];

            outputBuffer[0] = 0;
            outputBuffer[1] = receiveMode;

            outputBuffer[2] = (byte)((numberOfReceivedBytes >> 24) & 0x0FF);
            outputBuffer[3] = (byte)((numberOfReceivedBytes >> 16) & 0x0FF);
            outputBuffer[4] = (byte)((numberOfReceivedBytes >> 8) & 0x0FF);
            outputBuffer[5] = (byte)(numberOfReceivedBytes & 0x0FF);

            outputBuffer[6] = (byte)((waitTime >> 24) & 0x0FF);
            outputBuffer[7] = (byte)((waitTime >> 16) & 0x0FF);
            outputBuffer[8] = (byte)((waitTime >> 8) & 0x0FF);
            outputBuffer[9] = (byte)(waitTime & 0x0FF);

            outputBuffer[10] = (byte)(message.getSize());

            byte[] messageInByte = message.toByteArray();
            for (int i = 0; i < messageInByte.Length; i++)
            {
                outputBuffer[i + COMMAND_HEADER_LENGTH] = messageInByte[i];
            }
            
            sendDataToControlBoard(outputBuffer);
        }
        private void startReceivingData(ref UInt32 numberOfReceivedBytes, ref byte[] data)
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
                    data[pointer] = inputBuffer[j];
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
