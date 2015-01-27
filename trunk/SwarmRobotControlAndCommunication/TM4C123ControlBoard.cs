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
        private const byte TRANSMIT_DATA_TO_ROBOT = 0x10;
        private const byte TRANSMIT_DATA_TO_ROBOT_ACK = 0x17;
        private const byte RECEIVE_DATA_FROM_ROBOT = 0x11;
        private const byte RECEIVE_DATA_FROM_ROBOT_WITH_COMMAND = 0x12;
        private const byte CONFIGURE_RF = 0x13;
        private const byte CONFIGURE_SPI = 0x14;
        private const byte CONFIGURE_BOOTLOAD_PROTOCOL = 0x15;
        private const byte CONFIGURE_NORMAL_PROTOCOL = 0x16;
        //----------------------------------------//Commands definitions

        //----------------------ACK definitions----------------------//
        private const byte CONFIGURE_RF_OK = 0x12;
        private const byte CONFIGURE_SPI_OK = 0x13;
        private const byte CONFIGURE_BOOTLOAD_PROTOCOL_OK = 0x14;
        private const byte CONFIGURE_NORMAL_PROTOCOL_OK = 0x15;
        private const byte TRANSMIT_DATA_TO_ROBOT_DONE = 0xAA;
        private const byte TRANSMIT_DATA_TO_ROBOT_FAILED = 0xFA;
        private const byte DELAY_AFTER_MAX_DATA_TRANSMITTED = 1;
        private const byte MAX_NUM_BYTE_TRANSMITTED = 32;
        private const byte RECEIVE_DATA_FROM_ROBOT_ERROR = 0xEE;
        private const byte RECEIVE_DATA_FROM_ROBOT_CONTINUE = 0xAE;
        private const byte MAX_NUM_BYTE_RECEIVED = 32;

        public string failedToSendData = "Can't send data to the control board";
        public string failedToReadData = "No respone from the control board";
        public string numberOfDataOutOfRange = "The number of sent data is too high";
        public string invalidRespone = "Invalid response from the control board";
        //--------------------------------------------//ACK definitions
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

        /// <summary>
        /// Command to configure the communication protocol the control board
        /// </summary>
        public void configureBootloadProtocol()
        {
            try
            {
                Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
                outputBuffer[0] = 0;
                outputBuffer[1] = CONFIGURE_BOOTLOAD_PROTOCOL;

                sendDataToControlBoard(outputBuffer);

                isOperationFinishOk(CONFIGURE_BOOTLOAD_PROTOCOL_OK);
            }
            catch (Exception ex)
            {
                throw new Exception("Control Board configure bootload protocol: " + ex.Message);
            }
        }

        /// <summary>
        /// Command to configure the communication protocol the control board
        /// </summary>
        public void configureNormalProtocol()
        {
            try
            {
                Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
                outputBuffer[0] = 0;
                outputBuffer[1] = CONFIGURE_NORMAL_PROTOCOL;

                sendDataToControlBoard(outputBuffer);

                isOperationFinishOk(CONFIGURE_NORMAL_PROTOCOL_OK);
            }
            catch (Exception ex)
            {
                throw new Exception("Control Board configure normal protocol: " + ex.Message);
            }
        }

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

        /// <summary>
        /// Not implemented. Reserved for future use
        /// </summary>
        /// <param name="transmissionModeSelected"></param>
        public void setTransmissionMode(byte transmissionModeSelected)
        {
            throw new Exception("Set transmission mode function has not been implemented yet");
        }

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

        public void transmitBytesToRobotWithACK(byte[] transmittedData, UInt32 numberOfTransmittedBytes, byte delayTimeBeforeSendResponeToPC)
        {
           //TODO: implement
        }

        /// <summary>
        /// An overload function that used to transmit only one byte
        /// </summary>
        /// <param name="transmittedData">The trasmitted Data</param>
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

        /// <summary>
        /// Receive data from target through the control board.
        /// The received data length is limited to 2^32.
        /// </summary>
        /// <return> An exception if any errors occur or the number of received data is not enough</return>
        /// <param name="command">The command to specify what data should the target transmits to the PC</param>
        /// <param name="dataLength">The length of the expected data (also transmitted to the target)</param>
        /// <param name="data">The buffer to hold the received data</param>
        /// <param name="waitTime">The waiting time for a packet to be received</param>
        public void receiveBytesFromRobot(byte command, UInt32 dataLength, ref byte[] data, UInt32 waitTime)
        {
            try
            {
                if (dataLength < 1)
                    throw new Exception("Data length must be larger than 1");

                setupControlBoardBeforeReceivingData(command, RECEIVE_DATA_FROM_ROBOT_WITH_COMMAND, dataLength, waitTime);
                startReceivingData(ref dataLength, ref data);
            }
            catch (Exception ex)
            {
                throw new Exception("Received data: " + ex.Message + "\n" +
                                    "Number of data left: " + dataLength.ToString());
            }
        }
        private void setupControlBoardBeforeReceivingData(byte command, byte receiveMode, UInt32 numberOfReceivedBytes, UInt32 waitTime)
        {
            const int COMMAND_HEADER_LENGTH = 6;
            SwarmMessageHeader header = new SwarmMessageHeader(e_MessageType.MESSAGE_TYPE_HOST_COMMAND, command);

            byte[] byteBuffer = new byte[4];

            byteBuffer[0] = (byte)((numberOfReceivedBytes >> 24) & 0x0FF);
            byteBuffer[1] = (byte)((numberOfReceivedBytes >> 16) & 0x0FF);
            byteBuffer[2] = (byte)((numberOfReceivedBytes >> 8) & 0x0FF);
            byteBuffer[3] = (byte)(numberOfReceivedBytes & 0x0FF);

            SwarmMessage message = new SwarmMessage(header, byteBuffer);

            byte[] messageByte = message.toByteArray();

            if (message.getSize() > (65 - COMMAND_HEADER_LENGTH))
            {
                throw new Exception("setupControlBoardBeforeReceivingData() message size to large!!!");
            }

            byte[] outputBuffer = new Byte[65];

            outputBuffer[0] = 0;
            outputBuffer[1] = receiveMode;

            outputBuffer[2] = (byte)((waitTime >> 24) & 0x0FF);
            outputBuffer[3] = (byte)((waitTime >> 16) & 0x0FF);
            outputBuffer[4] = (byte)((waitTime >> 8) & 0x0FF);
            outputBuffer[5] = (byte)(waitTime & 0x0FF);

            for (int i = 0; i < messageByte.Length; i++)
            {
                outputBuffer[i + COMMAND_HEADER_LENGTH] = messageByte[i];
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

            //TODO: fix - DUMMY read
            inputBuffer = readDataFromControlBoard();
            checkIfReceivedDataFromRobot(inputBuffer);

            while (true)
            {
                inputBuffer = readDataFromControlBoard();

                checkIfReceivedDataFromRobot(inputBuffer);

                if (numberOfReceivedBytes > MAX_NUM_BYTE_RECEIVED)
                    length = MAX_NUM_BYTE_RECEIVED;
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
            if (inputBuffer[33] == RECEIVE_DATA_FROM_ROBOT_ERROR)
                throw new Exception("Did not receive data from robot");
        }

        /// <summary>
        /// Receive data from target through the control board 
        /// without transmitting command and data length to other devices.
        /// The received data length is limited to 2^32.
        /// </summary>
        /// <return> An exception if any errors occur or the number of received data is not enough</return>
        /// <param name="dataLength">The length of the expected data (NOT transmitted to the target)</param>
        /// <param name="data">The buffer to hold the received data</param>
        /// <param name="waitTime">The waiting time for a packet to be received</param>
        public void receiveBytesFromRobot(UInt32 dataLength, ref byte[] data, UInt32 waitTime)
        {
            try
            {
                if (dataLength < 1)
                    throw new Exception("Data length must be larger than 1");

                setupControlBoardBeforeReceivingData(0, RECEIVE_DATA_FROM_ROBOT, dataLength, waitTime);
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

                setupControlBoardBeforeReceivingData(0, RECEIVE_DATA_FROM_ROBOT, dataLength, waitTime);

                //TODO: fix this DUMMY read
                inputBuffer = readDataFromControlBoard();
                if (inputBuffer[33] == RECEIVE_DATA_FROM_ROBOT_ERROR)
                    return false;

                inputBuffer = readDataFromControlBoard();
                if (inputBuffer[33] == RECEIVE_DATA_FROM_ROBOT_ERROR)
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

    }
}
