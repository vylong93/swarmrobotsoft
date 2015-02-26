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
        void configureRF_TxAddress(byte[] setupData);

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
        /// Command to configure the transmission mode of the control board
        /// </summary>
        void setTransmissionMode(byte transmissionModeSelected);


		/// <summary>
        /// Transmit bootloader program packet to Robot
        /// <param name="transmittedData">The transmitted data</param>
        /// <param name="numberOfTransmittedBytes">Data length (1 - 2^32)</param>
        /// <param name="delayTimeBeforeSendResponeToPC">The delay time (ms) before the control board send the ok respone to PC.
        /// </summary>
        void transmitBslPacketToRobot(byte[] transmittedData, UInt32 numberOfTransmittedBytes, byte delayTimeBeforeSendResponeToPC);
        
		/// <summary>
        /// Scan for any jamming signal
        /// <param name="waitTime">The scaning period in ms</param>
        /// </summary>
		bool tryToDetectJammingSignal(UInt32 waitTime);

		
        /// <summary>
        /// Broadcast a command to the robots
        /// </summary>
        /// <param name="cmd">The command which transmit to the robots</param>
        void broadcastCommandToRobot(byte cmd);
		
        /// <summary>
        /// Broadcast a message to the robots
        /// </summary>
        /// <param name="message">The transmitted message</param>
        void broadcastMessageToRobot(SwarmMessage message);
		void broadcastDataToRobot(byte[] data);
		
        /// <summary>
        /// Transfer a message to robot. This require acknowledgement response from the robot.
        /// </summary>
        /// <param name="message">The transmitted message</param>
        bool sendMessageToRobot(SwarmMessage message);
		bool sendDataToRobot(byte[] data);
		
        /// <summary>
        /// Try receiving data from target through the control board 
        /// without transmitting command and data length to other devices.
        /// The received data length is limited to 32 bytes.
        /// </summary>
        /// <return> False if no data is received. Otherwise, return True </return>
        /// <param name="responseDataBuffer">The buffer to hold the received data</param>
        /// <param name="requestReceivedLength">The length of the expected data (NOT transmitted to the target)</param>
        /// <param name="delayResponse">The waiting time for a packet to be received</param>
        bool tryToReceivedDataFromRobot(byte[] responseDataBuffer, UInt32 requestReceivedLength, UInt32 delayResponse);
		
        /// <summary>
        /// Receive data from target through the control board.
        /// The received data length is limited to 2^32.
        /// </summary>
        /// <return> An exception if any errors occur or the number of received data is not enough</return>
        /// <param name="responseDataBuffer">The buffer to hold the received data</param>
        /// <param name="requestReceivedLength">The length of the expected data (also transmitted to the target)</param>
        /// <param name="delayResponse">The waiting time for a packet to be received</param>
        /// /// <param name="requestMessage">The command to specify what data should the target transmits to the PC</param>
        bool receivedDataFromRobot(byte[] responseDataBuffer, UInt32 requestReceivedLength, UInt32 delayResponse, SwarmMessage requestMessage);
    }
}
