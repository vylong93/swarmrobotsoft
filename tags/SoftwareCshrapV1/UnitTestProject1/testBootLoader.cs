using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwarmRobotControlAndCommunication;
using SwarmRobotControlAndCommunication.CustomInterfaces;

namespace TestSwarmRobotControlAndCommunication
{
    [TestClass]
    public class TestBootLoader
    {
        BootLoaderInterface bootLoader;

        UInt32 actualNumberOfLines;

        string testHexFilesPath;
        string hexFile83lines;
        string hexFile40lines;
        string hexFileNoEoF;
        string hexFileWrongSyntax;
        string hexFileBootLoaderAddress;

        [TestInitialize]
        public void initSetup()
        {
            ControlBoardInterface controlBoard = new TM4C123ControlBoard(0x04D8, 0x003F);
            bootLoader = new TivaBootLoader(controlBoard);
            testHexFilesPath = @"..\..\TestHexFile\";
            hexFile83lines = testHexFilesPath + "MainControl83Lines.hex";
            hexFile40lines = testHexFilesPath + "MainControl40Lines.hex";
            hexFileNoEoF = testHexFilesPath + "NoEndOfFile.hex";
            hexFileWrongSyntax = testHexFilesPath + "WrongSyntax.hex";
            hexFileBootLoaderAddress = testHexFilesPath + "BootLoaderAddress.hex";
        }

        [TestMethod]
        public void testGetNumberOfLineAndCheckHexFile()
        {
            actualNumberOfLines = 0;

            actualNumberOfLines = bootLoader.getNumberOfLineAndCheckHexFile(hexFile83lines);
            Assert.AreEqual((UInt32)83, actualNumberOfLines, "Wrong number of lines!");

            actualNumberOfLines = bootLoader.getNumberOfLineAndCheckHexFile(hexFile40lines);
            Assert.AreEqual((UInt32)40, actualNumberOfLines, "Wrong number of lines!");
        }

        [TestMethod]
        public void testGetNumberOfLineAndCheckHexFileExceptions()
        {
            try
            {
                bootLoader.getNumberOfLineAndCheckHexFile(hexFileNoEoF);
                Assert.Fail("No exception was thrown.");
            }
            catch (Exception ex)
            {
                StringAssert.Contains(ex.Message, "No End of File");
            }

            try
            {
                bootLoader.getNumberOfLineAndCheckHexFile(hexFileWrongSyntax);
                Assert.Fail("No exception was thrown.");
            }
            catch (Exception ex)
            {
                StringAssert.Contains(ex.Message, "Wrong Syntax");
            }

            try
            {
                bootLoader.getNumberOfLineAndCheckHexFile(hexFileBootLoaderAddress);
                Assert.Fail("No exception was thrown.");
            }
            catch (Exception ex)
            {
                StringAssert.Contains(ex.Message, "Invalid HEX file");
            }
        }


    }
}
