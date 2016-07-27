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

        private string _deviceGUID;
        private TunMode _tunmode;
        private IPAddress _interfaceIP;
        private IPAddress _networkAddress;
        private IPAddress _netmask;
        private uint _remotetun; // FIXME: support any

        private TunTapDevice _device = null;

        public SshVpn(ConnectionInfo connectionInfo, string deviceGUID, TunMode tunmode, uint remotetun, string interfaceIP, string networkAddress, string netmask)
            : base(connectionInfo, false)
        {
            _deviceGUID = deviceGUID;
            _tunmode = tunmode;
            _remotetun = remotetun;
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

            _device = new TunTapDevice(_deviceGUID);
            _device.DataReceived += Device_DataReceived;
            _device.Initialize(_tunmode, _interfaceIP, _networkAddress, _netmask);
            _device.Start();
        }
        
        protected override void OnDisconnected()
        {
            _channel.Close();

            if (_device != null)
            {
                _device.Stop();
                _device.Uninitialize();
            }

 	        base.OnDisconnected();
        }

        private void CreateChannel()
        {
            this._channel = Session.CreateChannelSshVpn(_tunmode, _remotetun);
            this._channel.ChannelDataReceived += Channel_DataReceived;

            // TODO: make sure buffers are empty
        }

        void Device_DataReceived(object sender, TunTapDeviceEventArgs e)
        {
            byte[] buf = new byte[e.Data.Length + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(_channel.LocalChannelNumber), 0, buf, 0, 4);
            Buffer.BlockCopy(e.Data, 0, buf, 4, e.Data.Length);
            _channel.SendData(buf);
        }

        void Channel_DataReceived(object sender, ChannelDataEventArgs e)
        {
            _device.WriteData(e.Data);
        }
    }
}
