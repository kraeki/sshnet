using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Common
{
    internal class TunTapDeviceEventArgs: EventArgs
    {
        /// <summary>
        /// Gets device data.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TunTapDeviceEventArgs"/> class.
        /// </summary>
        /// <param name="data">Received device data.</param>
        public TunTapDeviceEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
