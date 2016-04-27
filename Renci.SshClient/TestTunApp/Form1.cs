using Microsoft.Win32;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestTunApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            uint remotetun = 0; // TODO: make configurable

            string host = "localhost";
            string username = "roche";
            string password = "roche";
            int port = 2323;

            Console.WriteLine(String.Format("DEBUG: {0}@{1}:{2} -w :{3}", username, host, port, remotetun));
            ConnectionInfo c = new ConnectionInfo(host, port, username, new PasswordAuthenticationMethod(username, password));

            RegistryKey regVpn = Registry.CurrentUser.CreateSubKey("Software\\sshvpn");
            object guid = regVpn.GetValue("guid");
            if (guid == null)
            {
                Console.WriteLine("SSH VPN / TUN device not configured. Use the SSHLocalConfigurator.");
                return;
            }
            string devGuid = guid.ToString();
            object ip = regVpn.GetValue("IPAddress");
            if (ip == null)
            {
                Console.WriteLine("SSH VPN / TUN device not configured. Use the SSHLocalConfigurator.");
                return;
            }
            string ipAddress = ip.ToString();
            object mask = regVpn.GetValue("Netmask");
            if (mask == null)
            {
                Console.WriteLine("SSH VPN / TUN device not configured. Use the SSHLocalConfigurator.");
                return;
            }
            string subnetMask = mask.ToString();
            object net = regVpn.GetValue("Network");
            if (net == null)
            {
                Console.WriteLine("SSH VPN / TUN device not configured. Use the SSHLocalConfigurator.");
                return;
            }
            string network = net.ToString();

            Console.WriteLine(String.Format("DEBUG: SSH VPN with {0}:{1}:{2}:{3}", devGuid, ipAddress, network, subnetMask));
            SshVpn client = new SshVpn(c, devGuid, TunMode.POINTOPOINT, remotetun, ipAddress, network, subnetMask);
            client.Connect();
        }
    }
}
