using Renci.SshNet.Common;
using Renci.SshNet.Messages.Connection;
using Renci.SshNet.Messages.Connection.ChannelOpen;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace Renci.SshNet.Channels
{
    internal class ChannelSshVpn : ClientChannel, IChannelSshVpn
    {
        public event EventHandler<ChannelDataEventArgs> ChannelDataReceived;

        /// <summary>
        /// Counts failed channel open attempts
        /// </summary>
        private int _failedOpenAttempts;

        /// <summary>
        /// Holds a value indicating whether the session semaphore has been obtained by the current
        /// channel.
        /// </summary>
        /// <value>
        /// <c>0</c> when the session semaphore has not been obtained or has already been released,
        /// and <c>1</c> when the session has been obtained and still needs to be released.
        /// </value>
        private int _sessionSemaphoreObtained;

        /// <summary>
        /// Wait handle to signal when response was received to open the channel
        /// </summary>
        private EventWaitHandle _channelOpenResponseWaitHandle = new AutoResetEvent(false);

        private EventWaitHandle _channelRequestResponse = new ManualResetEvent(false);

        public override ChannelTypes ChannelType
        {
            get { return ChannelTypes.SshVpn; }
        }

        private TunMode tunmode;
        private uint remote_tun;

        public ChannelSshVpn(ISession session, uint localChannelNumber, uint localWindowSize, uint localPacketSize, TunMode tunmode, uint remotetun)
            : base(session, localChannelNumber, localWindowSize, localPacketSize)
        {
            this.tunmode = tunmode;
            this.remote_tun = remotetun;
            this.DataReceived += ChannelSshVpn_DataReceived;
        }

        void ChannelSshVpn_DataReceived(object sender, ChannelDataEventArgs e)
        {
            byte[] payload = new byte[e.Data.Length - 4];
            Buffer.BlockCopy(e.Data, 4, payload, 0, e.Data.Length-4); // skip remote channel number
            ChannelDataReceived(sender, new ChannelDataEventArgs(LocalChannelNumber, payload));
        }

        /// <summary>
        /// Opens the channel.
        /// </summary>
        public virtual void Open()
        {
            if (!IsOpen)
            {
                //  Try to open channel several times
                while (!IsOpen && _failedOpenAttempts < ConnectionInfo.RetryAttempts)
                {
                    SendChannelOpenMessage();
                    try
                    {
                        WaitOnHandle(_channelOpenResponseWaitHandle);
                    }
                    catch (Exception)
                    {
                        // avoid leaking session semaphore
                        ReleaseSemaphore();
                        throw;
                    }
                }

                if (!IsOpen)
                    throw new SshException(string.Format(CultureInfo.CurrentCulture, "Failed to open a channel after {0} attempts.", _failedOpenAttempts));
            }
        }

        public void SendData(byte[] data)
        {
            try
            {
                base.SendData(data);
            }
            catch (SshConnectionException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Sends the channel open message.
        /// </summary>
        protected void SendChannelOpenMessage()
        {
            // do not allow open to be ChannelOpenMessage to be sent again until we've
            // had a response on the previous attempt for the current channel
            if (Interlocked.CompareExchange(ref _sessionSemaphoreObtained, 1, 0) == 0)
            {
                SessionSemaphore.Wait();
                SendMessage(
                    new ChannelOpenMessage(
                        LocalChannelNumber,
                        LocalWindowSize,
                        LocalPacketSize,
                        new SshVpnChannelOpenInfo(tunmode, remote_tun)));
            }
        }

	/// not working
        ///public void SendDataMessage(byte[] data)
        ///{
            ///ChannelDataMessage a = new ChannelDataMessage(LocalChannelNumber, data);
            ///SendMessage(new ChannelDataMessage(LocalChannelNumber, data));
        ///}

        /// <summary>
        /// Called when channel is opened by the server.
        /// </summary>
        /// <param name="remoteChannelNumber">The remote channel number.</param>
        /// <param name="initialWindowSize">Initial size of the window.</param>
        /// <param name="maximumPacketSize">Maximum size of the packet.</param>
        protected override void OnOpenConfirmation(uint remoteChannelNumber, uint initialWindowSize, uint maximumPacketSize)
        {
            base.OnOpenConfirmation(remoteChannelNumber, initialWindowSize, maximumPacketSize);
            _channelOpenResponseWaitHandle.Set();
        }


        /// <summary>
        /// Releases the session semaphore.
        /// </summary>
        /// <remarks>
        /// When the session semaphore has already been released, or was never obtained by
        /// this instance, then this method does nothing.
        /// </remarks>
        private void ReleaseSemaphore()
        {
            if (Interlocked.CompareExchange(ref _sessionSemaphoreObtained, 0, 1) == 1)
                SessionSemaphore.Release();
        }
    }
}
