using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Common
{
    /// <summary>
    /// Tun mode specified by openssh in which the device will operate.
    /// </summary>
    public  enum TunMode {
        /// <summary>
        /// Constant for Point-To-Point tun mode (layer 3)
        /// </summary>
        POINTOPOINT = 0x01,  // layer 3
        /// <summary>
        /// Constant for Ethernet tun mode (layer 2)
        /// </summary>
        ETHERNET  =  0x02,   // layer 2
    }
}
