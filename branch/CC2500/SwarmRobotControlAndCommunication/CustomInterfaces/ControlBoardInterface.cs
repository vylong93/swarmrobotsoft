///-----------------------------------------------------------------------------
///	Swarm Robot Control & Communication Software developed by Le Binh Son:
///		Email: lebinhson90@gmail.com
///-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmRobotControlAndCommunication.CustomInterfaces
{
    public delegate void usbDeviceChangeEventsHandler(object sender, EventArgs e);

    public interface ControlBoardInterface
    {
        event usbDeviceChangeEventsHandler usbDeviceChangeEvent;

        bool isDeviceAttached { get; }

        /// <summary>
        /// Check if the control board is connected if a USB event occurs
        /// </summary>
        void findTargetDevice(); 

        /// <summary>
        /// Release all resources
        /// </summary>
        void Dispose();

        /// <summary>
        /// Command to configure the SPI module of the control board
        /// </summary>
        void configureSPI(byte[] setupData);

        /// <summary>
        /// Command to configure the RF module of the control board
        /// </summary>
        void configureRF(byte[] setupData);

        /// <summary>
        /// Command to configure the PWM module of the control board
        /// </summary>
        void configurePWM(byte[] setupData);

        /// <summary>
        /// Command to configure the UART module of the control board
        /// </summary>
        void configureUART(byte[] setupData);

        /// <summary>
        /// Command to configure the communication protocol the control board
        /// </summary>
        void configureBootloadProtocol();

        /// <summary>
        /// Command to configure the communication protocol the control board
        /// </summary>
        void configureNormalProtocol();

        /// <summary>
        /// Command to configure the transmission mode of the control board
        /// </summary>
        void setTransmissionMode(byte transmissionModeSelected);

        /// <summary>
        /// Transfer a number of bytes from the control board to targets.
        /// Only data is transmitted. numberOfTransmittedBytes and delay time are not sent to the robot.
        /// </summary>
        /// <param name="transmittedData">The transmitted data</param>
        /// <param name="numberOfTransmittedBytes">Data length (1 - 2^32)</param>
        /// <param name="delayTimeBeforeSendResponeToPC">The delay time (ms) before the control board send the ok respone to PC.
        /// This parameter can be used when we need to wait for the target to finish a certain action before sending the next command or data.
        /// </param>
        void transmitBytesToRobot(byte[] transmittedData, UInt32 numberOfTransmittedBytes, byte delayTimeBeforeSendResponeToPC);
        void transmitBslPacketToRobot(byte[] transmittedData, UInt32 numberOfTransmittedBytes, byte delayTimeBeforeSendResponeToPC);
        void transmitBytesToRobotWithACK(byte[] transmittedData, UInt32 numberOfTransmittedBytes, byte delayTimeBeforeSendResponeToPC);
        void sendCommandToRobot(byte cmd);

        /// <summary>
        /// Overload function that used to transmit only one byte
        /// </summary>
        /// <param name="transmittedData">The trasmitted Data</param>
        void transmitBytesToRobot(byte transmittedData);

        /// <summary>
        /// Receive data from target through the control board.
        /// The received data length is limited to 2^32.
        /// </summary>
        /// <return> An exception if any errors occur or the number of received data is not enough</return>
        /// <param name="command">The command to specify what data should the target transmits to the PC</param>
        /// <param name="dataLength">The length of the expected data (also transmitted to the target)</param>
        /// <param name="data">The buffer to hold the received data</param>
        /// <param name="waitTime">The waiting time for a packet to be received</param>
        void receiveBytesFromRobot(byte command, byte[] commandContent, UInt32 dataLength, ref byte[] data, UInt32 waitTime);

        /// <summary>
        /// Receive data from target through the control board 
        /// without transmitting command and data length to other devices.
        /// The received data length is limited to 2^32.
        /// </summary>
        /// <return> An exception if any errors occur or the number of received data is not enough</return>
        /// <param name="dataLength">The length of the expected data (NOT transmitted to the target)</param>
        /// <param name="data">The buffer to hold the received data</param>
        /// <param name="waitTime">The waiting time for a packet to be received</param>
        void receiveBytesFromRobot(UInt32 dataLength, ref byte[] data, UInt32 waitTime);

        /// <summary>
        /// Try receiving data from target through the control board 
        /// without transmitting command and data length to other devices.
        /// The received data length is limited to 32 bytes.
        /// </summary>
        /// <return> False if no data is received. Otherwise, return True </return>
        /// <param name="dataLength">The length of the expected data (NOT transmitted to the target)</param>
        /// <param name="data">The buffer to hold the received data</param>
        /// <param name="waitTime">The waiting time for a packet to be received</param>
        bool tryReceiveBytesFromRobot(UInt32 dataLength, ref byte[] data, UInt32 waitTime);
    }
}
