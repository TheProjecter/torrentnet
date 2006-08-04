using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using torrent.libtorrent;

namespace TrackerProbe
{
    public partial class TrackerProbe : Form
    {
        private Torrent torrentFile;
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public TrackerProbe()
        {
            InitializeComponent();
        }

        private void go_Click(object sender, EventArgs e)
        {
            if(socket.Connected)
            {
                socket.Disconnect(true);
            }
            connectionStatus.Text = "Connecting...";
            Uri trackerAddress = new Uri(trackerUrl.Text);
            
            socket.BeginConnect(trackerAddress.Host, trackerAddress.Port, delegate(IAsyncResult ar)
                {
                    MethodInvoker f = delegate
                        {
                            connectionStatus.Text = "Connected";
                            request.Enabled = true;
                            send.Enabled = true;
                        };
                    Invoke(f);
                    socket.EndConnect(ar);
                }, null);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                torrentFile = new Torrent(BenDecoder.Decode(dialog.FileName));
                trackerUrl.Text = torrentFile.AnnounceUri.ToString();
            }
        }

        private void send_Click(object sender, EventArgs e)
        {
            SendRequest();
        }

        private void ReceiveResponse()
        {
            MethodInvoker g = delegate()
                {
                    connectionStatus.Text = "Receiving...";
                };
            Invoke(g);
            Receive(delegate(string serverResponse)
                {
                    MethodInvoker f = delegate()
                        {
                            connectionStatus.Text = "Received";
                            request.Enabled = true;
                            send.Enabled = true;
                            response.Text = serverResponse;
                        };
                    Invoke(f);
                });
        }

        private void SendRequest()
        {
            Send(request.Text.Trim() + "\r\n", delegate(IAsyncResult ar)
            {
                MethodInvoker f = delegate()
                    {
                        connectionStatus.Text = "Sending...";
                        request.Enabled = false;
                        send.Enabled = false;
                    };
                Invoke(f);
                ReceiveResponse();
            });
        }
        
        private void Send(string data, AsyncCallback callback)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, delegate(IAsyncResult ar)
                {
                    callback(ar);
                    socket.EndSend(ar);
                }, null);
        }

        private delegate void ReceiveCallBack(string receivedData);
        private void Receive(ReceiveCallBack callback)
        {
            byte[] buffer = new byte[2 * 1024];
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, delegate(IAsyncResult ar)
                {
                    callback(new string(Encoding.ASCII.GetChars(buffer)));
                    socket.EndReceive(ar);
                }, null);
        }
    }
}