using System;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace TcpingWpf
{
    public partial class MainForm : Form
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string host = txtHost.Text.Trim();
            string portText = txtPort.Text.Trim();

            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("请输入目标主机/IP", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("端口必须是有效的数字(1-65535)", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            txtHost.Enabled = false;
            txtPort.Enabled = false;

            txtResult.Clear();
            AppendResult($"[{DateTime.Now:HH:mm:ss}] 开始检测 {host}:{port}...\r\n");

            ThreadPool.QueueUserWorkItem(_ => CheckPortLoop(host, port, _cancellationTokenSource.Token));
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            txtHost.Enabled = true;
            txtPort.Enabled = true;
            AppendResult($"\r\n[{DateTime.Now:HH:mm:ss}] 已停止检测\r\n");
        }

        private void CheckPortLoop(string host, int port, CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        var result = client.BeginConnect(host, port, null, null);
                        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

                        if (success && client.Connected)
                        {
                            AppendResult($"[{DateTime.Now:HH:mm:ss}] \u2705 端口 {port} 是开放的\r\n");
                        }
                        else
                        {
                            AppendResult($"[{DateTime.Now:HH:mm:ss}] \u274C 端口 {port} 未开放或拒绝连接\r\n");
                        }
                    }
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    if (ex.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound)
                    {
                        AppendResult($"[{DateTime.Now:HH:mm:ss}] \u26A0 无法解析主机名：{host}\r\n");
                        break;
                    }
                    AppendResult($"[{DateTime.Now:HH:mm:ss}] \u26A0 网络错误：{ex.Message}\r\n");
                }
                catch (Exception ex)
                {
                    AppendResult($"[{DateTime.Now:HH:mm:ss}] \u26A0 错误：{ex.Message}\r\n");
                }

                try
                {
                    Thread.Sleep(3000);
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        }

        private void AppendResult(string text)
        {
            if (txtResult.InvokeRequired)
            {
                txtResult.Invoke(new Action<string>(AppendResult), text);
            }
            else
            {
                txtResult.AppendText(text);
                txtResult.SelectionStart = txtResult.Text.Length;
                txtResult.ScrollToCaret();
            }
        }
    }
}
