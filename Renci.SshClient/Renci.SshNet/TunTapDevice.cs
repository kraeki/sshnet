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
    internal class TunTapDevice
    {
        private static EventWaitHandle WaitObjectRead = new EventWaitHandle(false, EventResetMode.AutoReset);
        private static EventWaitHandle WaitObjectWrite = new EventWaitHandle(false, EventResetMode.AutoReset);

        private string _deviceGUID;

        public event EventHandler<TunTapDeviceEventArgs> DataReceived;
        private bool _up = false;
        private Thread readerThread = null;

        public TunTapDevice(string deviceGUID)
        {
            _deviceGUID = deviceGUID;
        }

        public void Initialize(TunMode tunmode, IPAddress interfaceIP, IPAddress networkAddress, IPAddress netmask)
        {
            const string UsermodeDeviceSpace = "\\\\.\\Global\\";
            ptr = CreateFile(UsermodeDeviceSpace + _deviceGUID + ".tap", FileAccess.ReadWrite,
                FileShare.ReadWrite, 0, FileMode.Open, FILE_ATTRIBUTE_SYSTEM | FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            
            int len;
            IntPtr pstatus = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(pstatus, 1);
            DeviceIoControl(ptr, TAP_CONTROL_CODE(6, METHOD_BUFFERED) /* TAP_IOCTL_SET_MEDIA_STATUS */, pstatus, 4,
                pstatus, 4, out len, IntPtr.Zero);
            
            IntPtr ptun = Marshal.AllocHGlobal(12);
            Marshal.WriteInt32(ptun, 0, BitConverter.ToInt32(interfaceIP.GetAddressBytes(), 0));  // interface 162.132.242.250 // FIXME: use IP setting of adapter
            Marshal.WriteInt32(ptun, 4, BitConverter.ToInt32(networkAddress.GetAddressBytes(), 0));  // network 162.132.242.0
            Marshal.WriteInt32(ptun, 8, BitConverter.ToInt32(netmask.GetAddressBytes(), 0)); // mask 255.255.255.0
            DeviceIoControl(ptr, TAP_CONTROL_CODE(10, METHOD_BUFFERED) /* TAP_IOCTL_CONFIG_TUN */, ptun, 12,
                ptun, 12, out len, IntPtr.Zero);

            Tap = new FileStream(ptr, FileAccess.ReadWrite, true, 10000, true);
            Tap.Flush();
        }

        public void Uninitialize()
        {
            Tap.Close();

            try
            {
                CloseHandle(ptr);
            }
            catch(Exception)
            {
            }
        }

        public void Start()
        {
            if( _up )
            {
                // TODO: Log already started
                return;
            }

            _up = true;
            readerThread = new Thread(Reader);
            readerThread.Start();
        }

        public void Stop()
        {
            _up = false;
            WaitObjectRead.Set();
            readerThread.Join();
        }

        private void Reader()
        {
            AsyncCallback readCallback = new AsyncCallback(ReadDataCallback);
            byte[] buf = new byte[10000];

            IAsyncResult res;
            object state = new int();

            while (_up)
            {
                res = Tap.BeginRead(buf, 0, 10000, readCallback, state);
                WaitObjectRead.WaitOne();
                byte[] tmp = new byte[BytesRead];
                Buffer.BlockCopy(buf, 0, tmp, 0, BytesRead);
                DataReceived(this, new TunTapDeviceEventArgs(tmp));
            }
        }

        public void WriteData(byte[] data)
        {
            // FIXME: check if ready to send

            AsyncCallback writeCallback = new AsyncCallback(WriteDataCallback);
            object state = new int();
            IAsyncResult res;

            res = Tap.BeginWrite(data, 0, data.Length, writeCallback, state);
            Tap.Flush();
            WaitObjectWrite.WaitOne();
        }

        private static void WriteDataCallback(IAsyncResult asyncResult)
        {
            Tap.EndWrite(asyncResult);
            WaitObjectWrite.Set();
        }

        private static void ReadDataCallback(IAsyncResult asyncResult)
        {
            try
            {
                BytesRead = Tap.EndRead(asyncResult);
                WaitObjectRead.Set();
            }
            catch(Exception)
            {
            }
        }

        #region diver stuff
        private static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
        {
            return ((DeviceType << 16) | (Access << 14) | (Function << 2) | Method);
        }

        static uint TAP_CONTROL_CODE(uint request, uint method)
        {
            return CTL_CODE(FILE_DEVICE_UNKNOWN, request, method, FILE_ANY_ACCESS);
        }

        private const uint METHOD_BUFFERED = 0;
        private const uint FILE_ANY_ACCESS = 0;
        private const uint FILE_DEVICE_UNKNOWN = 0x00000022;

        IntPtr ptr;
        static FileStream Tap;
        static int BytesRead;

        [DllImport("Kernel32.dll", /* ExactSpelling = true, */ SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateFile(
            string filename,
            [MarshalAs(UnmanagedType.U4)]FileAccess fileaccess,
            [MarshalAs(UnmanagedType.U4)]FileShare fileshare,
            int securityattributes,
            [MarshalAs(UnmanagedType.U4)]FileMode creationdisposition,
            int flags,
            IntPtr template);
        const int FILE_ATTRIBUTE_SYSTEM = 0x4;
        const int FILE_FLAG_OVERLAPPED = 0x40000000;

        [DllImport("Kernel32.dll")]
        static extern Boolean CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
            IntPtr lpInBuffer, uint nInBufferSize,
            IntPtr lpOutBuffer, uint nOutBufferSize,
            out int lpBytesReturned, IntPtr lpOverlapped);
        #endregion

    }
}
