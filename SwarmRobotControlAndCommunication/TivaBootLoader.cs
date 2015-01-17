///-----------------------------------------------------------------------------
///	Swarm Robot Control & Communication Software developed by Le Binh Son:
///		Email: lebinhson90@gmail.com
///-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using SwarmRobotControlAndCommunication.CustomInterfaces;
using System.Windows;
using System.Diagnostics;

namespace SwarmRobotControlAndCommunication
{
    /// <summary>
    /// This class implements BootLoaderInterface to use only for PIC18F with 64KB Flash
    /// </summary>
    public class TivaBootLoader: BootLoaderInterface
    {
        #region Definitions for bootloader commands
            
            /// <summary>
            /// The bootloader start address is located at the start of
            /// the flash (from 0x00 -> EndAddress)
            /// It is not allowed to be written in this area
            /// </summary>
            private const uint BOOTLOADER_END_ADDRESS = 0x3FFF;

            /// <summary>
            /// Used to check the first address of the hex file to see
            /// if the user has relocated the app starting address to the
            /// same as in the bootloader or not.
            /// </summary>
            private const uint APP_START_ADDRESS = 0x4000; 

            /// <summary>
            /// The size of flash memory that will be reased at one time
            /// </summary>
            private const uint SIZE_ONE_ERASED_BLOCK = 1024;

            /// <summary>
            /// Command used to signal a robot to start update its
            /// program using the bootloader
            /// </summary>
            private const byte BOOTLOADER_START_COMMAND = 0xAA;

            /// <summary>
            /// The end of a hex file is detected when a hex line 
            /// do not contain any data (byteCount = 0)
            /// </summary>
            private const byte END_HEX_FILE = 0x00;

            /// <summary>
            ///  The opcode of NOP command in byte
            /// </summary>
            private const byte NOP = 0x00;

            /// <summary>
            /// The record type for data to program according to INTEL HEX
            /// </summary>
            private const byte RECORD_DATA = 0x00;

            /// <summary>
            /// The extended address record type according to INTEL HEX
            /// </summary>
            private const byte RECORD_EXTENDED_SEGMENT_ADDRESS = 0x02;

            /// <summary>
            /// The extended address record type according to INTEL HEX
            /// </summary>
            private const byte RECORD_EXTENDED_LINEAR_ADDRESS = 0x04;

            /// <summary>
            /// Used to create the buffer to store one line of data
            /// Should be larger or equal to the maximum data of one line
            /// </summary>
            private const byte MAX_LINE_DATA_LENGTH = 64;

            /// <summary>
            /// This is multiplied with transfer size in KB to determine
            /// the real wait time for mass erasing flash memory
            /// </summary>
            private const byte WAIT_FOR_MASS_FLASH_ERASE = 45; // 45 for entire flash, 5 for per kb

            /// <summary>
            /// Wait time between two packets of a data frame
            /// </summary>
            private const byte WAIT_BETWEEN_PACKETS = 1;

            /// <summary>
            /// The size of one block of bytes that will be written
            /// into the  flash memory in each programming frame
            /// </summary>
            private const byte SIZE_ONE_PROGRAM_BLOCK = 32; // default (16 - 2 - 25000), (32 - 2 - 15000)

            /// <summary>
            /// The waitting time for a NACK signal to be received (unit: ms).
            /// </summary>
            private const byte DATA_FRAME_NACK_WAIT_TIME = 2; //2 unit = 1ms, default 12
            private const UInt32 ROBOT_NEXT_FRAME_WAIT_TIME = 15000; //25000 default 150000
        #endregion

        #region Variables for bootloader commands
            private UInt32 extendedSegmentAddress;
            private UInt32 extendedLinearAddress;
            private UInt32 endLineAddess;
            private UInt32 endLineByteCount;
            private bool notAllOfNextLineDataIsSentFlag;
            private UInt32 currentHexLinePointer;
            private UInt32 startAddressCurrentProgramBlock;
            private UInt32 startAddressNextProgramBlock;
            private byte[] toSendData;
            private byte dataPointer;
            private UInt32 startLocationLeftOverData;
            private byte lengthOfLeftOverData;
            private UInt32 transferSize;
            private UInt32 numberOfLines;
            FileStream hexFile;
            private struct IntelHexFormat
            {
                public byte byteCount;
                public UInt32 address;
                public byte recordType;
                public byte checkSum;
                public byte[] data;
            }
            private IntelHexFormat nextHexLine;
            private struct DataFrameFormat
            {
                public byte byteCount;
                public UInt32 startAddress;
                public byte[] data;
            }
            private DataFrameFormat[] arrayDataFrame;
            private UInt32 maxNumberOfDataFrame;
            private UInt32 currentDataFramePointer;
            private ControlBoardInterface theControlBoard;
        #endregion

        public TivaBootLoader(ControlBoardInterface controlBoard, UInt32 flashSizeInKB)
        {
            extendedSegmentAddress = 0;
            extendedLinearAddress = 0;
            notAllOfNextLineDataIsSentFlag = false;
            currentHexLinePointer = 0;
            startAddressCurrentProgramBlock = APP_START_ADDRESS;
            startAddressNextProgramBlock = startAddressCurrentProgramBlock + SIZE_ONE_PROGRAM_BLOCK;
            toSendData = new byte[SIZE_ONE_PROGRAM_BLOCK];
            dataPointer = 0;
            startLocationLeftOverData = 0;
            lengthOfLeftOverData = 0;
            nextHexLine = new IntelHexFormat();
            maxNumberOfDataFrame = flashSizeInKB*1024/SIZE_ONE_PROGRAM_BLOCK;
            arrayDataFrame = new DataFrameFormat[maxNumberOfDataFrame];
            numberOfLines = 0;
            theControlBoard = controlBoard;
        }

        public event programmingProgressUpdate currentProgrammingPercentEvent;

        /// <summary>
        /// Verifye the format and calculate the number of lines of a HEX file.
        /// !!! This function also updates internal variables of the bootloader
        /// !!! used to control the programming process
        /// </summary>
        /// <param name="fileName">The patch to the HEX file</param>
        /// <returns>The number of line</returns>
        public UInt32 getNumberOfLineAndCheckHexFile(string fileName)
        {
            resetBootLoaderVariables();

            FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            numberOfLines = 0;

            IntelHexFormat currentLine = new IntelHexFormat();
            currentLine.data = new Byte[MAX_LINE_DATA_LENGTH];            
            int byteRead = 0;
            try
            {
                checkFirstByte(ref file);
                currentLine = readOneLineOfHexFile(ref file);
                checkAppStartAddress(currentLine.address);

                while (true)
                {
                    //Detect End of File (EoF) Signature
                    if (currentLine.byteCount == END_HEX_FILE) 
                        return numberOfLines;

                    checkRecordType(currentLine.recordType);
                    if (currentLine.recordType == RECORD_DATA)
                    {
                        checkNotBootLoaderAddress(currentLine.address);
                        verifyCheckSum(currentLine);

                        endLineAddess = currentLine.address;
                        endLineByteCount = currentLine.byteCount;
                    }

                    numberOfLines++;

                    //Go to the start ':' of next line
                    while (true)
                    {
                        if(isEndOfFile(byteRead) == true)
                            throw new Exception("No End of File (EoF) Detection");

                        if (isNewLineStart(byteRead) == true)
                        {
                            byteRead = 0;
                            break;
                        }
                        byteRead = file.ReadByte();
                    }

                    currentLine = readOneLineOfHexFile(ref file);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                file.Close();
            }
        }
        #region get number of hex line private functions
        /// <summary>
        /// Read one line of hex file according to the Intel HEX format.
        /// </summary>
        /// <returns>
        /// All fields that are specified in Intel HEX format by using reference.
        /// Pointer location is also advanced to the next line in HEX File.
        /// </returns>
        private IntelHexFormat readOneLineOfHexFile(ref FileStream intelHexFile)
        {
            try
            {
                while (true)
                {
                    IntelHexFormat currentLine = new IntelHexFormat();

                    UInt32[] lineAddress = new UInt32[2];

                    currentLine.byteCount = getOneByte(ref intelHexFile);

                    lineAddress[0] = getOneByte(ref intelHexFile);
                    lineAddress[1] = getOneByte(ref intelHexFile);

                    currentLine.recordType = getOneByte(ref intelHexFile);

                    currentLine.data = new byte[MAX_LINE_DATA_LENGTH];
                    for (byte i = 0; i < currentLine.byteCount; i++)
                    {
                        currentLine.data[i] = getOneByte(ref intelHexFile);
                    }

                    currentLine.checkSum = getOneByte(ref intelHexFile);

                    if (currentLine.recordType == RECORD_EXTENDED_SEGMENT_ADDRESS)
                    {
                        extendedSegmentAddress = currentLine.data[0];
                        extendedSegmentAddress <<= 8;
                        extendedSegmentAddress |= currentLine.data[1];
                        // Make sure the address is only contained in the 4 highest bits
                        if ((extendedSegmentAddress < 0x1000) || (extendedSegmentAddress > 0xF000))
                        {
                            String msg = String.Format("Invalid extended segment address: 0x{0}", extendedSegmentAddress.ToString("X4"));
                            throw new Exception(msg);
                        }
                        extendedSegmentAddress <<= 4;

                        // Move to the next line since this line
                        // only contains the upper 16 bits.
                        while (intelHexFile.ReadByte() != ':') ;
                        continue;
                    }

                    if (currentLine.recordType == RECORD_EXTENDED_LINEAR_ADDRESS)
                    {
                        extendedLinearAddress = currentLine.data[0];
                        extendedLinearAddress <<= 8;
                        extendedLinearAddress |= currentLine.data[1];
                        extendedLinearAddress <<= 16;
                        // Move to the next line since this line
                        // only contains the upper 16 bits.
                        while (intelHexFile.ReadByte() != ':') ;
                        continue;
                    }

                    //Find the real address
                    currentLine.address = lineAddress[0];
                    currentLine.address <<= 8;
                    currentLine.address |= lineAddress[1];
                    currentLine.address |= extendedLinearAddress;
                    currentLine.address += extendedSegmentAddress;
                    return currentLine;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Read one line of HEX file: " + ex.Message + "/n" + ex.StackTrace);
            }
        }
        private byte getOneByte(ref FileStream intelHexFile)
        {
            return convertHexToByte((Char)(intelHexFile.ReadByte()), 
                                    (Char)(intelHexFile.ReadByte()));
        }
        private void checkFirstByte(ref FileStream file)
        {
            if (file.ReadByte() != ':')
            {
                throw new Exception("Wrong Syntax: First line does not start with ':' ");
            }
        }
        private void checkRecordType(uint recordType)
        {
            if ((recordType == RECORD_DATA) || (recordType == 0x01)
                || (recordType == 0x03) || (recordType == 0x05) )
                return;

            string errorMessage = String.Format("Unknown record type\n" +
                            "Record type: 0x0{0}", recordType);
            throw new Exception(errorMessage);
        }
        private void checkNotBootLoaderAddress(UInt32 address)
        {
            if (isBootLoaderAddress(address) == true)
            {
                string errorMessage = String.Format("Invalid HEX file\n" +
                                "Bootloader protected address: 0x0000 -> 0x{0} \n" +
                                "Application address: 0x{1}", BOOTLOADER_END_ADDRESS.ToString("X4"), address.ToString("X4"));
                throw new Exception(errorMessage);
            }
        }
        private void checkAppStartAddress(UInt32 address)
        {
            if (address != APP_START_ADDRESS)
            {
                string errorMessage = String.Format("Invalid HEX file\n" +
                                "Application start address must be: 0x{0} \n" +
                                "Application address: 0x{1}", APP_START_ADDRESS.ToString("X4"), address.ToString("X4"));
                throw new Exception(errorMessage);
            }
        }
        private bool isEndOfFile(int byteRead)
        {
            if (byteRead == -1)
                return true;
            else
                return false;
        }
        private bool isBootLoaderAddress(UInt32 address)
        {
            if (address <= BOOTLOADER_END_ADDRESS)
            {
                return true;
            }
            return false;
        }
        private bool isNewLineStart(int byteRead)
        {
            if (byteRead == ':')
                return true;
            else
                return false;
        }
        private void verifyCheckSum(IntelHexFormat currentLine)
        {
            uint checkSumVerify = 1;

            checkSumVerify = currentLine.byteCount + ((currentLine.address >> 8) & 0xFF) + (currentLine.address & 0xFF) + currentLine.recordType;
            for (int i = 0; i < currentLine.byteCount; i++)
            {
                checkSumVerify = checkSumVerify + currentLine.data[i];
            }
            checkSumVerify = ((checkSumVerify & 0xFF) + currentLine.checkSum) & 0xFF;
            if (checkSumVerify != 0)
            {
                throw new Exception(String.Format("Check sum error:{0}", checkSumVerify));
            }
        }
        #endregion

        /// <summary>
        /// Start the programming process:
        /// 1) Send go into boot loader mode command
        /// 2) reset boot loader mode
        /// 3) Program hex file to robots
        /// </summary>
        /// <param name="fileName">The patch to the HEX file</param>
        /// <param name="cts">A cancel token to stop the programming process</param>
        public void startProgramming(string fileName, CancellationTokenSource cts)
        {
            //sendGoIntoBootLoaderModeCommand();
            resetBootLoaderVariables();
            programHexFileToRobots(fileName,cts);
        }

        /// <summary>
        /// Send a command to signal devices to go into bootloader mode
        /// </summary>
        private void sendGoIntoBootLoaderModeCommand()
        {
            try
            {
                theControlBoard.transmitBytesToRobot(BOOTLOADER_START_COMMAND);
            }
            catch (Exception ex)
            {
                throw new Exception("Go into BootLoader mode: " + ex.Message);
            }
        }

        /// <summary>
        /// Reset variables to their default state before starting a new programming procedure
        /// </summary>
        private void resetBootLoaderVariables()
        {
            extendedLinearAddress = 0;
            extendedSegmentAddress = 0;
            notAllOfNextLineDataIsSentFlag = false;
            currentHexLinePointer = 0;
            startAddressCurrentProgramBlock = APP_START_ADDRESS;
            startAddressNextProgramBlock = startAddressCurrentProgramBlock + SIZE_ONE_PROGRAM_BLOCK;
            toSendData = new byte[SIZE_ONE_PROGRAM_BLOCK];
            dataPointer = 0;
            startLocationLeftOverData = 0;
            lengthOfLeftOverData = 0;
            nextHexLine = new IntelHexFormat();
            arrayDataFrame = new DataFrameFormat[maxNumberOfDataFrame];
            currentDataFramePointer = 0;
        }

        /// <summary>
        /// Program the entire HEX file to robot unless a cacellation token is issued
        /// </summary>
        /// <param name="fileName">The patch to the HEX file</param>
        /// <param name="cts">The cancellation token</param>
        private void programHexFileToRobots(string fileName, CancellationTokenSource cts)
        {
            hexFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            nextHexLine.data = new byte[SIZE_ONE_PROGRAM_BLOCK];
            try
            {
                //prepareBootLoader();
                sendStartBootloaderPacket();
                startProgrammingUsingBootLoader(cts); 
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException();
            }
            catch (Exception ex)
            {
                throw new Exception("Programming Hex file to Robots: " + ex.Message + ex.StackTrace);
            }
            finally
            {
                hexFile.Close();
            }
        }
        #region program hex file to robots private functions
        // Long Dang, 31 Jul 2014, add 
        private void sendStartBootloaderPacket()
        {
            currentProgrammingPercentEvent(0);

            //Discard the start symbol of the first line
            hexFile.ReadByte();

            nextHexLine = readOneLineOfHexFile(ref hexFile);

            byte checkSum = 0;
            byte[] transmitData = new byte[1 + 4 + 4 + 1]; //<0xAA><tranferSize><robotWaitTime><checksum>

            transmitData[0] = (byte)(BOOTLOADER_START_COMMAND);

            transferSize = endLineAddess - APP_START_ADDRESS + endLineByteCount;
            transmitData[1] = (byte)((transferSize >> 24) & 0xFF);
            transmitData[2] = (byte)((transferSize >> 16) & 0xFF);
            transmitData[3] = (byte)((transferSize >> 8) & 0xFF);
            transmitData[4] = (byte)(transferSize & 0xFF);

            transmitData[5] = (byte)((ROBOT_NEXT_FRAME_WAIT_TIME >> 24) & 0xFF);
            transmitData[6] = (byte)((ROBOT_NEXT_FRAME_WAIT_TIME >> 16) & 0xFF);
            transmitData[7] = (byte)((ROBOT_NEXT_FRAME_WAIT_TIME >> 8) & 0xFF);
            transmitData[8] = (byte)(ROBOT_NEXT_FRAME_WAIT_TIME & 0xFF);

            checkSum = (byte)(~(transmitData[0] + transmitData[1] 
                + transmitData[2] + transmitData[3] + transmitData[4]
                + transmitData[5] + transmitData[6] + transmitData[7] 
                + transmitData[8]) + 1);

            transmitData[9] = checkSum;

            theControlBoard.transmitBytesToRobot(transmitData, 10, 1);

            //System.Threading.Thread.Sleep((int)(WAIT_FOR_MASS_FLASH_ERASE * transferSize / 1024));
            System.Threading.Thread.Sleep(WAIT_FOR_MASS_FLASH_ERASE);

            currentHexLinePointer++;
        }        
        private void prepareBootLoader()
        {
            currentProgrammingPercentEvent(0);

            //Discard the start symbol of the first line
            hexFile.ReadByte();

            nextHexLine = readOneLineOfHexFile(ref hexFile);
                
            byte[] transmitData = new byte[4];
            transferSize = endLineAddess - APP_START_ADDRESS + endLineByteCount;
            transmitData[0] = (byte)((transferSize >> 24) & 0xFF);
            transmitData[1] = (byte)((transferSize >> 16) & 0xFF);
            transmitData[2] = (byte)((transferSize >> 8) & 0xFF);
            transmitData[3] = (byte)(transferSize & 0xFF);
            theControlBoard.transmitBytesToRobot(transmitData, 4, 1);
            //System.Threading.Thread.Sleep((int)(WAIT_FOR_MASS_FLASH_ERASE*transferSize/1024));
            System.Threading.Thread.Sleep(WAIT_FOR_MASS_FLASH_ERASE);
            //MessageBox.Show(transferSize.ToString());
            currentHexLinePointer++;
        }
        private void movePointerToTheNextLine()
        {
            while (hexFile.ReadByte() != ':') ;
        }           
        
        private void startProgrammingUsingBootLoader(CancellationTokenSource cancelToken)
        {
            bool isSkipTheRest = false;
            while (isCanceledByUser(cancelToken) == false)
            {
                updateCurrentProgrammingEvent();

                isSkipTheRest = fillOneByteFrame();

                if (isSkipTheRest == true)
                {
                    programOneByteFrameToFlash(SIZE_ONE_PROGRAM_BLOCK, startAddressCurrentProgramBlock, toSendData, cancelToken);
                    updateAddressesAndDataPointer();
                    continue;
                }
                if (isOneByteFrameReady() == true)
                {
                    programOneByteFrameToFlash(SIZE_ONE_PROGRAM_BLOCK, startAddressCurrentProgramBlock, toSendData, cancelToken);
                    updateAddressesAndDataPointer();
                }
                //Update all next* variables if only part of nextHexLineData is sent
                if (notAllOfNextLineDataIsSentFlag == true)
                {
                    notAllOfNextLineDataIsSentFlag = false;

                    fillSentDataWithLeftOverData();
                    updateNextLineAddressAndByteCount();
                    continue;
                }

                while (true)
                {
                    if (nextHexLine.byteCount == END_HEX_FILE)
                    {
                        // If there is still data left then send it before exiting the bootloader
                        if (dataPointer != 0)
                            programOneByteFrameToFlash(dataPointer, startAddressCurrentProgramBlock, toSendData, cancelToken);
                        return;
                    }
                    movePointerToTheNextLine();
                    nextHexLine = readOneLineOfHexFile(ref hexFile);
                    currentHexLinePointer++;

                    // Record 0x02 and 0x04 is processed in readOneLineOfHexFile()
                    // This condition is to skip 0x03 and 0x05 record types
                    if (nextHexLine.recordType == RECORD_DATA)
                        break;
                }
            }
        }
        private void updateCurrentProgrammingEvent()
        {
            currentProgrammingPercentEvent((UInt32)(Math.Round((currentHexLinePointer - 1) * 100.0 / numberOfLines)));
        }
        private bool isCanceledByUser(CancellationTokenSource cancelToken)
        {
            if (cancelToken.IsCancellationRequested == true)
                return true;
            else
                return false;
        }
        private bool fillOneByteFrame()
        {
            if (isNextAddressInsideCurrentBlock() == false)
            {
                fillNopCommandFromCurrentDataPointerToNextProgrammedBlock(ref toSendData, dataPointer, SIZE_ONE_PROGRAM_BLOCK);
                return true;
            }
            else if (isNextAddressAndAllOfItsDataInsideCurrentBlock() == true)
            {
                    fillNopCommandFromCurrentDataPointerToNextAddress(ref toSendData, ref dataPointer, nextHexLine.address);
                    fillSentDataWithAllOfNextLineData(ref toSendData, ref dataPointer, nextHexLine.byteCount, nextHexLine.data);
                    return false;
            }
            else //The next address is inside of the current write block but not all of its data
            {
                    startLocationLeftOverData = (uint)(startAddressNextProgramBlock - nextHexLine.address);
                    lengthOfLeftOverData = (byte)(nextHexLine.byteCount - startLocationLeftOverData);

                    fillNopCommandFromCurrentDataPointerToNextAddress(ref toSendData, ref dataPointer, nextHexLine.address);
                    fillSentDataWithPartOfNextLineData(ref toSendData, ref dataPointer, startLocationLeftOverData, nextHexLine.data);

                    notAllOfNextLineDataIsSentFlag = true;
                    return false;
            }
        }
        private bool isNextAddressInsideCurrentBlock()
        {
            if (nextHexLine.address - startAddressCurrentProgramBlock >= SIZE_ONE_PROGRAM_BLOCK)
                return false;
            else
                return true;
        }
        private void updateAddressesAndDataPointer()
        {
            startAddressCurrentProgramBlock += SIZE_ONE_PROGRAM_BLOCK;
            startAddressNextProgramBlock += SIZE_ONE_PROGRAM_BLOCK;
            dataPointer = 0;
        }
        private bool isNextAddressAndAllOfItsDataInsideCurrentBlock()
        {
            if (nextHexLine.address - startAddressCurrentProgramBlock + nextHexLine.byteCount <= SIZE_ONE_PROGRAM_BLOCK)
                return true;
            else
                return false;
        }
        private bool isOneByteFrameReady()
        {
            if (dataPointer == SIZE_ONE_PROGRAM_BLOCK)
                return true;
            else
                return false;
        }
        private void fillSentDataWithLeftOverData()
        {
            for (int i = 0; i < lengthOfLeftOverData; i++)
            {
                nextHexLine.data[i] = nextHexLine.data[startLocationLeftOverData + i];
            }
        }
        private void updateNextLineAddressAndByteCount()
        {
            nextHexLine.address = startAddressCurrentProgramBlock;
            nextHexLine.byteCount = lengthOfLeftOverData;
        }
        private void fillNopCommandFromCurrentDataPointerToNextAddress(ref byte[] programData, ref byte dataPointer, uint nextAddress)
        {
            uint fillDataLength = nextAddress - startAddressCurrentProgramBlock - dataPointer;
            for (int i = 0; i < fillDataLength; i++)
            {
                programData[dataPointer + i] = NOP;
            }

            dataPointer = (byte)(dataPointer + fillDataLength);

            return;
        }
        private void fillNopCommandFromCurrentDataPointerToNextProgrammedBlock(ref byte[] programData, byte dataPointer, uint SIZE_ONE_PROGRAM_BLOCK)
        {
            for (int i = 0; i < (SIZE_ONE_PROGRAM_BLOCK - dataPointer); i++)
            {
                programData[dataPointer + i] = NOP;
            }
            return;
        }
        private void fillSentDataWithAllOfNextLineData(ref byte[] programData, ref byte dataPointer, byte nextLineByteCount, byte[] nextLineData)
        {

            for (int i = 0; i < nextLineByteCount; i++)
            {
                programData[dataPointer + i] = nextLineData[i];
            }
            dataPointer = (byte)(dataPointer + nextLineByteCount);
            return;
        }
        private void fillSentDataWithPartOfNextLineData(ref byte[] programData, ref byte dataPointer, uint dataLength, byte[] nextLineData)
        {

            for (int i = 0; i < dataLength; i++)
            {
                programData[dataPointer + i] = nextLineData[i];
            }
            dataPointer = (byte)(dataPointer + dataLength);
            return;
        }
        /// <summary>
        /// Send one frame of bytes contains Intel HEX fields with the following format: 
        /// 1) Byte count + checksum + start address
        /// 2) A block of program data, with the length is specified by the byte count
        /// </summary>
        /// <returns>
        /// Return true if success. Otherwise throw an exception
        /// </returns>
        private bool programOneByteFrameToFlash(byte byteCount, uint startAddress, byte[] programData, CancellationTokenSource cancelToken)
        {
            try
            {
                UInt16 checkSum = generateCheckSum(byteCount, startAddress, programData);
                bool isReceivedSignal = false;
                byte[] nackSignal = new byte[3];

                byte programPacketLength = (byte)(4 + 1 + byteCount + 2); //  <start address><byte count><data[0]...data[byte count - 1]><checksum>

                byte[] transmitBuffer = new byte[programPacketLength]; 

                arrayDataFrame[currentDataFramePointer].byteCount = byteCount;
                arrayDataFrame[currentDataFramePointer].startAddress = startAddress;
                arrayDataFrame[currentDataFramePointer].data = new byte[programPacketLength];

                transmitBuffer[0] = (byte)(((startAddress >> 24) & 0xFF));
                transmitBuffer[1] = (byte)(((startAddress >> 16) & 0xFF));
                transmitBuffer[2] = (byte)(((startAddress >> 8) & 0xFF));
                transmitBuffer[3] = (byte)(startAddress & 0xFF);
                transmitBuffer[4] = (byte)byteCount;
                for (int i = 0; i < byteCount; i++)
                {
                    transmitBuffer[i + 5] = programData[i];
                    arrayDataFrame[currentDataFramePointer].data[i] = programData[i];
                }
                transmitBuffer[programPacketLength - 2] = (byte)((checkSum >> 8) & 0xFF);
                transmitBuffer[programPacketLength - 1] = (byte)(checkSum & 0xFF);


                while (isCanceledByUser(cancelToken) == false)
                {
                    theControlBoard.transmitBytesToRobot(transmitBuffer, programPacketLength, 0);

                    if ((endLineAddess + endLineByteCount) == (startAddress + byteCount))
                        isReceivedSignal = theControlBoard.tryReceiveBytesFromRobot(1, ref nackSignal, 255);
                    else
                        isReceivedSignal = theControlBoard.tryReceiveBytesFromRobot(1, ref nackSignal, DATA_FRAME_NACK_WAIT_TIME);

                    if (isReceivedSignal)
                    {
                        if (currentDataFramePointer != 0)
                        {// Re-written the previous data frame before writing this data frame
                            // -> keep going back to previous data frames until a successfull write occurs
                            // or we reach the first data frame. 
                            // This is a roundabout way so we don't need "error" devices to send back 
                            // their current flash address.
                            
                            currentDataFramePointer--;
                            //if (currentDataFramePointer < PROGRAM_RESERVED_PACKET_STEP)   
                            //    currentDataFramePointer = 0;
                            //else
                            //    currentDataFramePointer = currentDataFramePointer - PROGRAM_RESERVED_PACKET_STEP;

                            programOneByteFrameToFlash(arrayDataFrame[currentDataFramePointer].byteCount,
                                                    arrayDataFrame[currentDataFramePointer].startAddress,
                                                    arrayDataFrame[currentDataFramePointer].data,
                                                    cancelToken);
                        }
                    }
                    else
                    {
                        currentDataFramePointer++;
                        return true;
                    }
               }
               return false;
            }
            catch (Exception ex)
            {
                String mssg = String.Format("Byte count: {0} ", byteCount);
                throw new Exception("Program one byte Frame: " + mssg + ex.Message);
            }
        }
        private UInt16 generateCheckSum(byte byteCount, UInt32 startAddress, byte[] programData)
        {
            uint checkSum = 0;
            UInt16 generatedChecksum = 0;

            checkSum = byteCount + ((startAddress >> 24) & 0xFF) + ((startAddress >> 16) & 0xFF) 
                                 + ((startAddress >> 8)  & 0xFF) + (startAddress & 0xFF);

            for (int i = 0; i < byteCount; i++)
            {
                checkSum = checkSum + programData[i];
            }

            generatedChecksum = (UInt16)((~checkSum + 1) & 0xFFFF);

            if (((generatedChecksum + checkSum) & 0xFFFF) == 0)
            {
                return generatedChecksum;
            }
            else
            {
                throw new Exception("The generated check sum is wrong");
            }
        }
        public static byte convertCharToHex(char readByte)
        {
            try
            {
                readByte = char.ToUpper(readByte);

                if (char.IsLetter(readByte))
                {
                    return (Byte)(readByte - 'A' + 10);
                }
                else if (char.IsDigit(readByte))
                {
                    return (Byte)(readByte - '0');
                }
                else
                {
                    throw new Exception(String.Format("{0}", readByte));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("convertCharToHex Error: " + ex.Message);
            }
        }
        public static byte convertHexToByte(char upperBits, char lowerBits)
        {
            int finalResults = 0;
            try
            {
                finalResults = (convertCharToHex(upperBits) << 4) | (convertCharToHex(lowerBits));
            }
            catch
            {
                string errorMessage = String.Format("convertHexToByte Error: " +
                                                    "UpperBits:{0} LowerBits:{1}", upperBits, lowerBits);
                throw new Exception(errorMessage);
            }
            return (Byte)(finalResults);
        }
        private char convertByteToHex(int data)
        {
            switch (data)
            {
                case 0:
                    return '0';
                case 1:
                    return '1';
                case 2:
                    return '2';
                case 3:
                    return '3';
                case 4:
                    return '4';
                case 5:
                    return '5';
                case 6:
                    return '6';
                case 7:
                    return '7';
                case 8:
                    return '8';
                case 9:
                    return '9';
                case 10:
                    return 'A';
                case 11:
                    return 'B';
                case 12:
                    return 'C';
                case 13:
                    return 'D';
                case 14:
                    return 'E';
                case 15:
                    return 'F';
                default:
                    throw new Exception("Convert Byte to Hex: Data is out of range!");
            }
        }
        #endregion

    }
}
