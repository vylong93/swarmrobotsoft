using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwarmRobotControlAndCommunication;

namespace TestSwarmRobotControlAndCommunication
{
    [TestClass]
    public class TestPicControlBoard
    {
        TM4C123ControlBoard theControlBoard;

        [TestInitialize]
        public void initSetup()
        {
           theControlBoard = new TM4C123ControlBoard(0x04D8, 0x003F);
        }

        #region Unit Test Examples
        [TestMethod]
        public void testGetT2CONValue()
        {
            // arrange
            byte[] TMR2Prescale = {1, 4, 16};
            
            
            byte[] expectedT2CONValue = {0x04, 0x05, 0x06};
            byte[] actualT2CONValue = new byte[3];
            
            for (uint i = 0; i < 3; i++)
            {
                // act
                actualT2CONValue[i] = TM4C123ControlBoard.getT2CONValue(TMR2Prescale[i]);

                // assert
                Assert.AreEqual(expectedT2CONValue[i], actualT2CONValue[i], "Wrong returned T2CONValue");
            }
        }

        [TestMethod]
        public void testGetT2CONValueWithExceptionThrowing()
        {
            // arrange
            byte TMR2Prescale = 2;

            byte actualT2CONValue;

            try
            {
              //act
                actualT2CONValue = TM4C123ControlBoard.getT2CONValue(TMR2Prescale);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                StringAssert.Contains(ex.Message, "Invalid Prescale value");
                return;
            }
            Assert.Fail("No exception was thrown.");
        }
        #endregion
    }

     
}
