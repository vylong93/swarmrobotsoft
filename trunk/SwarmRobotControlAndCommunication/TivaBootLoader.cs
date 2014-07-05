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
            /// is not allowed to be written in this area
            /// </summary>
            private const uint BootLoaderEndAddress         = 0x1FFF;

            /// <summary>
            /// Used to check the first address of the hex file to see
            /// if the user has relocated the app starting address to the
            /// same as in the bootloader or not.
            /// </summary>
            private const uint ApplicationStartAddress      = 0x2000; 

            /// <summary>
            /// The size of one block of bytes that will be written
            /// into the  flash memory in each programming frame
            /// </summary>
            private const byte SizeOfOneProgramBlock        = 32;

            /// <summary>
            /// The size of flash memory that will be reased at one time
            /// </summary>
            private const uint SizeOfOneErasedBlock         = 1024;

            /// <summary>
            /// Command used to signal a robot to start update its
            /// program using the bootloader
            /// </summary>
            private const byte GotoIntoBootLoaderCommand    = 0xAA;

            /// <summary>
            /// The end of a hex file is detected when a hex line 
            /// do not contain any data (byteCount = 0)
            /// </summary>
            private const byte EndOfHexFile                 = 0x00;

            /// <summary>
            /// The wait time between each programming frame so that 
            /// a robot finish writting a programm block to its flash
            /// memory. Unit [ms] 
            /// </summary>
            private const byte WaitTimeBetweenEachProgammingBlock = 10;

            /// <summary>
            ///  The opcode of NOP command in byte
            /// </summary>
            private const byte NOP = 0x00;

            /// <summary>
            /// The record type for data to program according to INTEL HEX
            /// </summary>
            private const byte DataRecord = 0x00;

            /// <summary>
            /// The extended address record type according to INTEL HEX
            /// </summary>
            private const byte ExtendedAddressRecord = 0x04;

            /// <summary>
            /// Used to create the buffer to store one line of data
            /// Should be larger or equal to the maximum data of one line
            /// </summary>
            private const byte MaxLineDataLength = 64;

            /// <summary>
            /// This is multiplied with transfersize in KB to determine
            /// the real wait time for mass erasing flash memory
            /// </summary>
            private const byte WaitForMassFlassErase = 25;
        #endregion

        #region Variables for bootloader commands
            private byte extendedAddress;
            private uint startAddress;
            private uint endLineAddess;
            private uint endLineByteCount;
            private bool notAllOfNextLineDataIsSentFlag;
            private uint currentHexLinePointer;
            private uint startAddressCurrentProgramBlock;
            private uint startAddressNextProgramBlock;
            private byte[] toSendData;
            private byte dataPointer;
            private uint startLocationLeftOverData;
            private byte lengthOfLeftOverData;
            private UInt32 transferSize;
            private struct IntelHexFormat
            {
                public byte byteCount;
                public uint address;
                public byte recordType;
                public byte checkSum;
                public byte[] data;
            }
            private IntelHexFormat firstHexLine;
            private IntelHexFormat nextHexLine;
            private UInt32 numberOfLines;

            FileStream hexFile;

            private ControlBoardInterface theControlBoard;
        #endregion

        public TivaBootLoader(ControlBoardInterface controlBoard)
        {
            extendedAddress = 0;
            notAllOfNextLineDataIsSentFlag = false;
            currentHexLinePointer = 0;
            startAddressCurrentProgramBlock = ApplicationStartAddress;
            startAddressNextProgramBlock = startAddressCurrentProgramBlock + SizeOfOneProgramBlock;
            toSendData = new byte[SizeOfOneProgramBlock];
            dataPointer = 0;
            startLocationLeftOverData = 0;
            lengthOfLeftOverData = 0;
            firstHexLine = new IntelHexFormat();
            nextHexLine = new IntelHexFormat();

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
            currentLine.data = new Byte[MaxLineDataLength];            
            int byteRead = 0;
            try
            {
                checkFirstByte(ref file);
                currentLine = readOneLineOfHexFile(ref file);
                checkAppStartAddress(currentLine.address);

                while (true)
                {
                    //Detect End of File (EoF) Signature
                    if (currentLine.byteCount == EndOfHexFile) 
                        return numberOfLines;

                    checkRecordType(currentLine.recordType);
                    if (currentLine.recordType == DataRecord)
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
                IntelHexFormat currentLine = new IntelHexFormat();

                UInt32[] lineAddress = new UInt32[2];

                currentLine.byteCount = getOneByte(ref intelHexFile);

                lineAddress[0] = getOneByte(ref intelHexFile);
                lineAddress[1] = getOneByte(ref intelHexFile);

                currentLine.recordType = getOneByte(ref intelHexFile);

                currentLine.data = new byte[MaxLineDataLength];
                for (byte i = 0; i < currentLine.byteCount; i++)
                {
                    currentLine.data[i] = getOneByte(ref intelHexFile);
                }

                currentLine.checkSum = getOneByte(ref intelHexFile);

                if (currentLine.recordType == ExtendedAddressRecord)
                        extendedAddress = currentLine.data[currentLine.byteCount - 1];

                //Find the real address
                currentLine.address = (uint)((extendedAddress << 16) + (lineAddress[0] << 8) + lineAddress[1]);

                return currentLine;
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
            if ( (recordType == DataRecord) || (recordType == 0x01) ||
                 (recordType == ExtendedAddressRecord) || (recordType == 0x03)
               )
                return;

            string errorMessage = String.Format("Unknown record type\n" +
                            "Record type: 0x0{0}", recordType);
            throw new Exception(errorMessage);
        }
        private void checkNotBootLoaderAddress(uint address)
        {
            if (isBootLoaderAddress(address) == true)
            {
                string errorMessage = String.Format("Invalid HEX file\n" +
                                "Bootloader protected address: 0x0000 -> 0x{0} \n" +
                                "Application address: 0x{1}", BootLoaderEndAddress.ToString("X4"), address.ToString("X4"));
                throw new Exception(errorMessage);
            }
        }
        private void checkAppStartAddress(uint address)
        {
            if (address != ApplicationStartAddress)
            {
                string errorMessage = String.Format("Invalid HEX file\n" +
                                "Application start address must be: 0x{0} \n" +
                                "Application address: 0x{1}", ApplicationStartAddress.ToString("X4"), address.ToString("X4"));
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
        private bool isBootLoaderAddress(uint address)
        {
            if (address <= BootLoaderEndAddress)
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
            sendGoIntoBootLoaderModeCommand();
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
                theControlBoard.transmitBytesToRobot(GotoIntoBootLoaderCommand);
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
            extendedAddress = 0;
            notAllOfNextLineDataIsSentFlag = false;
            currentHexLinePointer = 0;
            startAddressCurrentProgramBlock = ApplicationStartAddress;
            startAddressNextProgramBlock = startAddressCurrentProgramBlock + SizeOfOneProgramBlock;
            toSendData = new byte[SizeOfOneProgramBlock];
            dataPointer = 0;
            startLocationLeftOverData = 0;
            lengthOfLeftOverData = 0;
            firstHexLine = new IntelHexFormat();
            nextHexLine = new IntelHexFormat();
        }

        /// <summary>
        /// Program the entire HEX file to robot unless a cacellation token is issued
        /// </summary>
        /// <param name="fileName">The patch to the HEX file</param>
        /// <param name="cts">The cancellation token</param>
        private void programHexFileToRobots(string fileName, CancellationTokenSource cts)
        {
            hexFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            firstHexLine.data = new byte[SizeOfOneProgramBlock];
            nextHexLine.data = new byte[SizeOfOneProgramBlock];

            try
            {
                prepareBootLoader();
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
        private void prepareBootLoader()
        {
            currentProgrammingPercentEvent(0);

            //Discard the start symbol of the first line
            hexFile.ReadByte();

            nextHexLine = readOneLineOfHexFile(ref hexFile);
                
            startAddress = nextHexLine.address;
            byte[] transmitData = new byte[8];
            transmitData[0] = (byte)((startAddress >> 24) & 0xFF);
            transmitData[1] = (byte)((startAddress >> 16) & 0xFF);
            transmitData[2] = (byte)((startAddress >> 8) & 0xFF);
            transmitData[3] = (byte)(startAddress & 0xFF);

            transferSize = endLineAddess - startAddress + endLineByteCount;
            transmitData[4] = (byte)((transferSize >> 24) & 0xFF);
            transmitData[5] = (byte)((transferSize >> 16) & 0xFF);
            transmitData[6] = (byte)((transferSize >> 8) & 0xFF);
            transmitData[7] = (byte)(transferSize & 0xFF);
            theControlBoard.transmitBytesToRobot(transmitData, 8, 1);
            System.Threading.Thread.Sleep((int)(WaitForMassFlassErase*transferSize/1024));
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
                    programOneByteFrameToFlash(SizeOfOneProgramBlock, startAddressCurrentProgramBlock, toSendData);
                    updateAddressesAndDataPointer();
                    continue;
                }
                if (isOneByteFrameReady() == true)
                {
                    programOneByteFrameToFlash(SizeOfOneProgramBlock, startAddressCurrentProgramBlock, toSendData);
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
                    if (nextHexLine.byteCount == EndOfHexFile)
                    {
                        // If there is still data left then send it before exiting the bootloader
                        if (dataPointer != 0)
                            programOneByteFrameToFlash(dataPointer, startAddressCurrentProgramBlock, toSendData);
                        return;
                    }
                    movePointerToTheNextLine();
                    nextHexLine = readOneLineOfHexFile(ref hexFile);
                    currentHexLinePointer++;
                    
                    if (nextHexLine.recordType == DataRecord)
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
                fillNopCommandFromCurrentDataPointerToNextProgrammedBlock(ref toSendData, dataPointer, SizeOfOneProgramBlock);
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
                
            //if (isNextAddressInsideCurrentBlock())
            //{
            //    if (isNextAddressAndAllOfItsDataInsideCurrentBlock() == true)
            //    {
            //        fillNopCommandFromCurrentDataPointerToNextAddress(ref toSendData, ref dataPointer, nextHexLine.address);
            //        fillSentDataWithAllOfNextLineData(ref toSendData, ref dataPointer, nextHexLine.byteCount, nextHexLine.data);
            //        return false;
            //    }
            //    else //The next address is inside of the current write and erase block but not all of its data
            //    {
            //        startLocationLeftOverData = (uint)(startAddressNextProgramBlock - nextHexLine.address);
            //        lengthOfLeftOverData = (byte)(nextHexLine.byteCount - startLocationLeftOverData);

            //        fillNopCommandFromCurrentDataPointerToNextAddress(ref toSendData, ref dataPointer, nextHexLine.address);
            //        fillSentDataWithPartOfNextLineData(ref toSendData, ref dataPointer, startLocationLeftOverData, nextHexLine.data);

            //        notAllOfNextLineDataIsSentFlag = true;
            //        return false;
            //    }
            //}
            //else
            //{
            //    fillNopCommandFromCurrentDataPointerToNextProgrammedBlock(ref toSendData, dataPointer, SizeOfOneProgramBlock);
            //    return true;

            //}
        }
        private bool isNextAddressInsideCurrentBlock()
        {
            if (nextHexLine.address - startAddressCurrentProgramBlock >= SizeOfOneProgramBlock)
                return false;
            else
                return true;
        }
        private void updateAddressesAndDataPointer()
        {
            startAddressCurrentProgramBlock += SizeOfOneProgramBlock;
            startAddressNextProgramBlock += SizeOfOneProgramBlock;
            dataPointer = 0;
        }
        private bool isNextAddressAndAllOfItsDataInsideCurrentBlock()
        {
            if (nextHexLine.address - startAddressCurrentProgramBlock + nextHexLine.byteCount <= SizeOfOneProgramBlock)
                return true;
            else
                return false;
        }
        private bool isOneByteFrameReady()
        {
            if (dataPointer == SizeOfOneProgramBlock)
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
        private void fillNopCommandFromCurrentDataPointerToNextProgrammedBlock(ref byte[] programData, byte dataPointer, uint SizeOfOneProgramBlock)
        {
            for (int i = 0; i < (SizeOfOneProgramBlock - dataPointer); i++)
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
        private bool programOneByteFrameToFlash(byte byteCount, uint startAddress, byte[] programData)
        {
            try
            {
                UInt16 checkSum = generateCheckSum(byteCount, startAddress, programData);
                sendByteCountCheckSumAddress(byteCount, checkSum, startAddress);
                theControlBoard.transmitBytesToRobot(programData, byteCount, WaitTimeBetweenEachProgammingBlock);
                transferSize -= byteCount;
            }
            catch (Exception ex)
            {
                throw new Exception("Program one byte Frame: " + ex.Message);
            }
            return true;
        }
        private void sendByteCountCheckSumAddress(byte byteCount, UInt16 checkSum, uint startAddress)
        {
            byte length = 6;
            byte[] setupData = new byte[length];
            setupData[0] = byteCount;
            setupData[1] = (byte)((checkSum >> 8) & 0xFF);
            setupData[2] = (byte)(checkSum  & 0xFF);
            setupData[3] = (byte)(((startAddress >> 16) & 0xFF));
            setupData[4] = (byte)(((startAddress >> 8) & 0xFF));
            setupData[5] = (byte)(startAddress & 0xFF);
            theControlBoard.transmitBytesToRobot(setupData, length, WaitTimeBetweenEachProgammingBlock);
        }
        private UInt16 generateCheckSum(byte byteCount, uint startAddress, byte[] programData)
        {
            uint checkSum = 0;
            UInt16 generatedChecksum = 0;

            checkSum = byteCount + ((startAddress >> 16) & 0xFF) + ((startAddress >> 8) & 0xFF) + (startAddress & 0xFF);
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
