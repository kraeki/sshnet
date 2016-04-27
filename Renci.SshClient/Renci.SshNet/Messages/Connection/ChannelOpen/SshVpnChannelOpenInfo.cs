using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Messages.Connection.ChannelOpen
{
    class SshVpnChannelOpenInfo : ChannelOpenInfo
    {
        /// <summary>
        /// Specifies channel open type
        /// </summary>
        public const string NAME = "tun@openssh.com";

        public TunMode Tunmode { get; private set; }
        public uint Remote_tun { get; private set; }

        public override string ChannelType
        {
            get { return "tun@openssh.com"; }
        }

        public SshVpnChannelOpenInfo(TunMode tunmode, uint remote_tun)
        {
            Tunmode = tunmode;
            Remote_tun = remote_tun;
        }

        public SshVpnChannelOpenInfo(byte[] data)
        {
            Load(data);
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            base.LoadData();

            Tunmode = (TunMode)ReadUInt32();
            Remote_tun = ReadUInt32();
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            base.SaveData();
            Write((UInt32)Tunmode);
            Write(Remote_tun);
        }
    }
}
