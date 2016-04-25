using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Channels
{
    interface IChannelSshVpn : IChannel
    {        
        /// <summary>
        /// Opens the channel.
        /// </summary>
        void Open();

        /// void SendDataMessage(byte[] data); // FIXME: does not work


        /// <summary>
        /// This event fires on incoming data from the channel.
        /// </summary>
        event EventHandler<ChannelDataEventArgs> ChannelDataReceived;

    }
}
