///-----------------------------------------------------------------------------
///	Swarm Robot Control & Communication Software developed by Le Binh Son:
///		Email: lebinhson90@gmail.com
///-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SwarmRobotControlAndCommunication.CustomInterfaces;

namespace SwarmRobotControlAndCommunication.CustomInterfaces
{
    public delegate void programmingProgressUpdate(UInt32 value);

    public interface BootLoaderInterface
    {
        /// <summary>
        /// An event used to notify the programming process to the main program
        /// </summary>
        event programmingProgressUpdate currentProgrammingPercentEvent;

        /// <summary>
        /// Verify the format and calculate the number of lines of a HEX file.
        /// !!! The main program must call this function right before each time
        /// !!! the function startProgamming is invoked, because this function
        /// !!! will also update internal variables used to control the programming
        /// procedure.
        /// </summary>
        /// <param name="fileName">The patch to the HEX file</param>
        /// <returns>The number of line</returns>
        UInt32 getNumberOfLineAndCheckHexFile(string fileName);

        /// <summary>
        /// Start the programming process, a recommended programming procedure should has:
        /// 1) Send go into boot loader mode command
        /// 2) reset boot loader mode
        /// 3) Program hex file to robots
        /// </summary>
        /// <param name="fileName">The patch to the HEX file</param>
        /// <param name="cts">A cancel token to stop the programming process</param>
        void startProgramming(string fileName, CancellationTokenSource cts);

    }
}
