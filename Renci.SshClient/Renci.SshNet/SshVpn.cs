using Renci.SshNet.Channels;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Renci.SshNet
{
    public class SshVpn : BaseClient
    {
        private IChannelSshVpn _channel;
        private TunMode _tunmode;

        private IPAddress _interfaceIP;
        private IPAddress _networkAddress;
        private IPAddress _netmask;

        private TunTapDevice _device = null;

        public SshVpn(ConnectionInfo connectionInfo, TunMode tunmode, string interfaceIP, string networkAddress, string netmask)
            : base(connectionInfo, false)
        {
            this._tunmode = tunmode;
            if( !IPAddress.TryParse(interfaceIP, out _interfaceIP))
            {
                throw new FormatException(String.Format("interfaceIP {0} is not in right format. Please specify in x.x.x.x format.", interfaceIP));
            }
            if( !IPAddress.TryParse(networkAddress, out _networkAddress))
            {
                throw new FormatException(String.Format("networkAddress {0} is not in right format. Please specify in x.x.x.x format.", networkAddress));
            }
            if( !IPAddress.TryParse(netmask, out _netmask))
            {
                throw new FormatException(String.Format("netmask {0} is not in right format. Please specify in x.x.x.x format.", netmask));
            }
        }

        /// <summary>
        /// Called when client is connected to the server.
        /// </summary>
        protected override void OnConnected()
        {
            base.OnConnected();
            CreateChannel();
            _channel.Open();

            if(_device != null)
            {
                return;
            }

            _device = new TunTapDevice();
            _device.DataReceived += Device_DataReceived;
            _device.Initialize(_tunmode, _interfaceIP, _networkAddress, _netmask);
            _device.Start();
        }

        private void CreateChannel()
        {
            uint tun_mode = 1;
            uint remote_tun = 1;

            this._channel = Session.CreateChannelSshVpn(tun_mode, remote_tun); // FIXME: Use TunMode
            this._channel.ChannelDataReceived += Channel_DataReceived;

            // TODO: make sure buffers are empty
        }

        void Device_DataReceived(object sender, TunTapDeviceEventArgs e)
        {
            byte[] buf = new byte[e.Data.Length + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(_channel.LocalChannelNumber), 0, buf, 0, 4);
            Buffer.BlockCopy(e.Data, 0, buf, 4, e.Data.Length);
            Console.WriteLine(String.Format("Forward into channel")); // FIXME: remove debug logs
            _channel.SendData(buf);
        }

        void Channel_DataReceived(object sender, ChannelDataEventArgs e)
        {
            Console.WriteLine(String.Format("Received data from channel {0}: {1} bytes: {2}", e.ChannelNumber, e.Data.Length, BitConverter.ToString(e.Data)));
            _device.WriteData(e.Data);
            Console.WriteLine(String.Format("Forwared to adapter"));
        }
    }
}
