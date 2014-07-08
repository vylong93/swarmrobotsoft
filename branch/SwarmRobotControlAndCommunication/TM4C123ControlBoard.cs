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
        private const byte RECEIVE_DATA_FROM_ROBOT = 0x11;
        private const byte CONFIGURE_RF = 0x12;
        private const byte CONFIGURE_SPI = 0x13;
        //----------------------------------------//Commands definitions

        //----------------------ACK definitions----------------------//
        private const byte CONFIGURE_RF_OK = 0x12;
        private const byte CONFIGURE_SPI_OK = 0x13;
        private const byte TRANSMIT_DATA_TO_ROBOT_DONE = 0xAA;
        private const byte TRANSMIT_DATA_TO_ROBOT_FAILED = 0xFA;
        private const byte DELAY_AFTER_MAX_DATA_TRANSMITTED = 1;
        private const byte MAX_NUM_BYTE_TRANSMITTED = 32;
        private const byte RECEIVE_DATA_FROM_ROBOT_ERROR = 0xEE;
        private const byte RECEIVE_DATA_FROM_ROBOT_CONTINUE = 0xAE;
        private const byte MAX_NUM_BYTE_RECEIVED = 32;

        public string failedToSendData = "Can't send data to the control board";
        public string failedToreadData = "No respone from the control board";
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
        /// Configure the SPI module of the control board.
        /// Thrown an exception if an error occurs
        /// </summary>
        /// <param name="setupData">The data frame corresponding to the SPI data frame
        /// of the control board to configure the SPI module</param>
        public void configureSPI(byte[] setupData)
        {
            try
            {
                Byte[] outputBuffer = new Byte[USB_BUFFER_LENGTH];
                outputBuffer[0] = 0;
                outputBuffer[1] = CONFIGURE_SPI;
                for (int i = 0; i < setupData.Length; i++)
                {
                    outputBuffer[i + 2] = setupData[i];
                }

                sendDataToControlBoard(outputBuffer);

                isOperationFinishOk(CONFIGURE_SPI_OK);
            }
            catch (Exception ex)
            {
                throw new Exception("Control Board configure SPI: " + ex.Message);
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
        /// Only data is transmitted. Data length and delay time are not sent to the robot.
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

                    if(isEndOfSentData(numberOfTransmittedBytes))
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

            if(inputBuffer[1] == noResponeFlag)
                throw new Exception("No respone from robot");

            if(inputBuffer[1] == invalidFlag)
                throw new Exception("Invalid respone from robot");

            throw new Exception(invalidRespone);
        }

        /// <summary>
        /// Receive data from target through the control board.
        /// The received data length is limited to 2^32.
        /// </summary>
        /// <param name="command">The command to specify what data should the target transmits to the PC</param>
        /// <param name="numberOfReceivedData">The length of the expected data</param>
        /// <param name="data">The buffer to hold the received data</param>
        public void receiveBytesFromRobot(byte command, UInt32 numberOfReceivedBytes, ref byte[] data)
        {
                byte[] outputBuffer = new Byte[65];
                byte[] inputBuffer = new Byte[65];
                UInt32 length = 0;
                UInt32 dataLength = numberOfReceivedBytes;
                UInt32 pointer = 0;
            try
            {
                if(numberOfReceivedBytes < 1)
                    throw new Exception("Data length must be larger than 1");

                outputBuffer[0] = 0;
                outputBuffer[1] = RECEIVE_DATA_FROM_ROBOT;
                outputBuffer[2] = command;
                outputBuffer[3] = (byte)((numberOfReceivedBytes >> 24) & 0x0FF);
                outputBuffer[4] = (byte)((numberOfReceivedBytes >> 16) & 0x0FF);
                outputBuffer[5] = (byte)((numberOfReceivedBytes >> 8) & 0x0FF);
                outputBuffer[6] = (byte)(numberOfReceivedBytes & 0x0FF);
                    
                sendDataToControlBoard(outputBuffer);
                outputBuffer[1] = RECEIVE_DATA_FROM_ROBOT_CONTINUE;
                while (true)
                {
                    inputBuffer = readDataFromControlBoard();
                        
                    checkIfReceivedDataFromRobot(inputBuffer);

                    if (dataLength > MAX_NUM_BYTE_RECEIVED)
                        length = MAX_NUM_BYTE_RECEIVED;
                    else
                        length = dataLength;

                    for (UInt32 j = 1; j <= length; j++)
                    {
                        data[pointer] = inputBuffer[j];
                        pointer++;
                    }

                    dataLength -= length;
                    if (dataLength <= 0)
                        break;

                    sendDataToControlBoard(outputBuffer);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Received data: " + ex.Message + "\n" +
                                    "Number of data left: " + dataLength.ToString());
            }
        }
        void checkIfReceivedDataFromRobot(byte[] inputBuffer)
        {
            if (inputBuffer[33] == RECEIVE_DATA_FROM_ROBOT_ERROR)
                throw new Exception("Did not receive data from robot");
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
            if (!success) throw new Exception(failedToreadData);
            return inputBuffer;
        }

    }
}
