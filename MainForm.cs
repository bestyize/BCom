using BCom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BlueSerial.Utils.Utils;


namespace BlueSerial
{
    public partial class MainForm : Form
    {
        private ArrayList comList = new ArrayList();//串口列表
        private ArrayList scanComList = new ArrayList();//串口列表
        private static SerialPort mSerialPort;
        private Thread receiveThread=null,scanComThread=null,periodSendThread;
        private delegate void SafeCallDelegate(string text);
        private int sendByteCount = 0;
        public MainForm()
        {
            InitializeComponent();
            initView();
            serialPortListInit();

            scanComThread = new Thread(new ThreadStart(scanComTask));
            scanComThread.Start();
            

        }


        private void initView()
        {
            cb_baud_list.DataSource = baudRateList;
            cb_data_list.DataSource = dataBitList;
            cb_parity_list.DataSource = parityList;
            cb_stop_list.DataSource = stopBitList;
            cb_baud_list.SelectedIndex = 3;
            
        }

        private void scanComTask()
        {
            while (true)
            {
                scanComList.Clear();
                foreach (string portName in SerialPort.GetPortNames())
                {
                    scanComList.Add(portName);
                }
                if (comList.Count != scanComList.Count)
                {
                    cb_com_list.DataSource = scanComList;
                }
                Thread.Sleep(2000);
            }
        }

        private void serialPortListInit()
        {
            if (comList.Count > 0)
            {
                comList.Clear();
            }
            foreach (string portName in SerialPort.GetPortNames())
            {
                comList.Add(portName);
            }

            if (comList.Count == 0)
            {
                MessageBox.Show("没有搜索到串口");
                cb_com_list.DataSource = comList;
                return;
            }
            cb_com_list.DataSource = comList;

            if (cb_com_list.SelectedIndex > comList.Count || cb_com_list.SelectedIndex < 0)
            {
                //MessageBox.Show("串口丢失");
                return;
            }
            mSerialPort = new SerialPort(comList[cb_com_list.SelectedIndex].ToString());
            if (!mSerialPort.IsOpen)
            {
                try
                {
                    mSerialPort.Open();
                    btn_open_com.Text = "关闭串口";
                    btn_open_com.ForeColor = Color.Red;
                }
                catch (Exception)
                {
                    MessageBox.Show("串口"+mSerialPort.PortName+"被占用，无法打开，请关闭占用串口的程序");
                }
                
            }
            

            mSerialPort.BaudRate = baudRateList[cb_baud_list.SelectedIndex];
            mSerialPort.DataBits = dataBitList[cb_data_list.SelectedIndex];
            mSerialPort.Parity = parityList[cb_parity_list.SelectedIndex];
            mSerialPort.StopBits = stopBitList[cb_stop_list.SelectedIndex];
            mSerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }


        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            receiveThread = new Thread(new ParameterizedThreadStart(displayReceiveData));
            receiveThread.Start(indata);
        }

        private void displayReceiveData(object obj)
        {
            if (tb_recv.InvokeRequired)
            {
                var d = new SafeCallDelegate(displayReceiveData);
                Invoke(d, new object[] { obj });
            }
            else {
                if (cbox_hex_display.Checked)
                {
                    
                    tb_recv.AppendText(" " + convertToHexString((string)obj));
                    label_recv_count.Text = "R:" + (tb_recv.Text.Length + 1) / 3;
                }
                else {
                    
                    tb_recv.AppendText((string)obj);
                    label_recv_count.Text = "R:" + tb_recv.Text.Length;

                }
                
            }
            
        }

        private void Btn_open_com_Click(object sender, EventArgs e)
        {
            if (mSerialPort != null)
            {
                if (mSerialPort.IsOpen)
                {
                    try
                    {
                        mSerialPort.Close();
                        btn_open_com.Text = "打开串口";
                        btn_open_com.ForeColor = Color.Black;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("串口关闭失败");
                    }
                }
                else
                {
                    try
                    {
                        mSerialPort.Open();
                        btn_open_com.Text = "关闭串口";
                        btn_open_com.ForeColor = Color.Red;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("串口" + mSerialPort.PortName + "被占用，无法打开，请关闭占用串口的程序");
                        btn_open_com.Text = "打开串口";
                        btn_open_com.ForeColor = Color.Black;
                    }
                }

                
            }
            else
            {
                serialPortListInit();
            }

        }

        private void Btn_clean_send_Click(object sender, EventArgs e)
        {
            tb_send.Text = "";
            label_send_count.Text = "S:0";
        }

        private void Btn_send_Click(object sender, EventArgs e)
        {
            sendData();
        }

        private void sendData()
        {
            if (mSerialPort != null && mSerialPort.IsOpen)
            {
                if ((cbox_send_line.Checked == true) && (cbox_send_hex.Checked == true))
                {
                    //发送16进制新行  
                    byte[] bytes = convertHexStringToBytes(tb_send.Text);
                    if (bytes != null)
                    {
                        mSerialPort.Write(bytes, 0, bytes.Length);
                        byte[] newLine = { 0xd, 0xa };
                        mSerialPort.Write(newLine, 0, newLine.Length);
                        sendByteCount += (bytes.Length + newLine.Length);
                    }
                }
                else if ((cbox_send_line.Checked == true) && (cbox_send_hex.Checked == false))
                {
                    //发送普通字符串新行
                    mSerialPort.Write(tb_send.Text + "\r\n");
                    sendByteCount += (tb_send.Text.Length + 2);
                }
                else if ((cbox_send_line.Checked == false) && (cbox_send_hex.Checked == false))
                {
                    //发送普通字符串
                    mSerialPort.Write(tb_send.Text);
                    sendByteCount += tb_send.Text.Length;
                }
                else if ((cbox_send_line.Checked == false) && (cbox_send_hex.Checked == true))
                {
                    //发送16进制
                    byte[] bytes = convertHexStringToBytes(tb_send.Text);
                    if (bytes != null)
                    {
                        mSerialPort.Write(bytes, 0, bytes.Length);
                        sendByteCount += bytes.Length;
                    }
                    
                }
                else
                {
                    MessageBox.Show("不支持的操作！");
                }
                label_send_count.Text = "S:" + sendByteCount;

            }
            else
            {
                MessageBox.Show("尚未打开串口，请打开后再发送");
            }
        }

        private void Cb_com_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            if (mSerialPort != null)
            {
                if (mSerialPort.IsOpen)
                {
                    mSerialPort.Close();
                }
                btn_open_com.Text = "打开串口";
                btn_open_com.ForeColor = Color.Black;
                mSerialPort = null;
                serialPortListInit();
                
            }
            


        }

        private void Cb_baud_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mSerialPort != null)
            {
                mSerialPort.BaudRate = baudRateList[cb_baud_list.SelectedIndex];
            }
        }

        private void Cb_stop_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mSerialPort != null)
            {
                mSerialPort.StopBits = stopBitList[cb_stop_list.SelectedIndex];
                //mSerialPort.StopBits = stopBitList[cb_stop_list.SelectedIndex];

            }
        }

        private void Cb_data_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mSerialPort != null)
            {
                mSerialPort.DataBits = dataBitList[cb_data_list.SelectedIndex];
            }
        }

        private void Cb_parity_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mSerialPort != null)
            {
                mSerialPort.Parity = parityList[cb_parity_list.SelectedIndex];
            }
        }

        private void Cbox_hex_display_CheckedChanged(object sender, EventArgs e)
        {
            if (cbox_hex_display.Checked)
            {
                tb_recv.Text = convertToHexString(tb_recv.Text);
            }
            else {
                tb_recv.Text = convertHexStringToCommonString(tb_recv.Text);
            }
        }

        private void Cbox_timer_send_CheckedChanged(object sender, EventArgs e)
        {
            
            if (cbox_timer_send.Checked)
            {
                try
                {
                    Convert.ToInt32(tb_period_send_time_ms.Text);
                    periodSendThread = new Thread(new ThreadStart(periodSendTask));
                    periodSendThread.Start();
                }
                catch (Exception) {
                    MessageBox.Show("周期格式不正确");
                    cbox_timer_send.Checked = false;
                    return;
                }
                 
                
            }
            else
            {
                if (periodSendThread != null)
                {
                    try
                    {
                        periodSendThread.Abort();
                    } catch (Exception)
                    {
                        MessageBox.Show("周期发送结束失败，请重试！");
                    }
                    
                }
                
            }
            
        }

        private void Ts_menu_about_Click(object sender, EventArgs e)
        {
            MessageBox.Show("BCom是一款免费开源的串口调试助手","关于 BCom2.0");
        }

        private void Ts_menu_weixin_open_Click(object sender, EventArgs e)
        {
            WxForm wxForm = new WxForm();
            wxForm.Show();
        }

        private void Ts_menu_git_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/bestyize/BCom");
        }

        private void Ts_menu_calc_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("calc");
        }

        private void Ts_menu_nodepad_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("notepad");
        }

        private void Ts_menu_cmd_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("cmd");
        }

        private void Ts_menu_regedit_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("regedit");
        }

        private void Ts_menu_screenshot_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("snippingtool");
            }
            catch (Exception)
            {
                MessageBox.Show("您的系统不支持");
            }
        }

        private void Ts_menu_mspaint_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("mspaint");
        }

        private void Ts_menu_screenshot_tool_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("snippingtool");
        }

        private void Ts_menu_mind_paint_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.freedraw.xyz/");
        }

        private void Ts_menu_mode_Click(object sender, EventArgs e)
        {
            if (this.Width == 400)
            {
                this.Width = 730;
                this.Height = 530;
                ts_menu_mode.Text = "精简模式";
            }
            else
            {
                this.Width = 400;
                this.Height = 60;
                ts_menu_mode.Text = "高级模式";
            }

        }

        private void Ts_menu_notepad_tool_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("notepad");
        }

        private void Ts_menu_save_as_Click(object sender, EventArgs e)
        {
            Stream mStream;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = "d:\\";
            saveFileDialog.Filter = "ext files (*.txt)|*.txt|All files(*.*)|*>**";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            
            if (saveFileDialog.ShowDialog()==DialogResult.OK&& saveFileDialog.FileName.Length > 0)
            {
                if ((mStream = saveFileDialog.OpenFile())!= null)
                {
                    StreamWriter writer = new StreamWriter(mStream);
                    writer.Write(tb_recv.Text);
                    writer.Flush();
                    writer.Close();
                    mStream.Close();
                    MessageBox.Show("存储文件成功！", "温馨提示");
                }
              
                
            }
        }

        private void Ts_menu_open_file_Click(object sender, EventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
                var fileStream = openFileDialog.OpenFile();
                StreamReader reader = new StreamReader(fileStream);
                fileContent = reader.ReadToEnd();
                reader.Close();
                fileStream.Close();
                tb_send.Text = fileContent;
            }
            
        }

        private void Cbox_rts_CheckedChanged(object sender, EventArgs e)
        {
            if (mSerialPort != null)
            {
                if (cbox_rts.Checked)
                {
                    mSerialPort.RtsEnable = true;
                }
                else
                {
                    mSerialPort.RtsEnable = false;
                }
            }
        }

        private void Cbox_dtr_CheckedChanged(object sender, EventArgs e)
        {
            if (mSerialPort != null)
            {
                if (cbox_dtr.Checked)
                {
                    mSerialPort.DtrEnable = true;
                }
                else 
                {
                    mSerialPort.DtrEnable = false;
                }
                
            }
        }

        private void periodSendTask()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            while (true)
            {
                sendData();
                Thread.Sleep(Convert.ToInt32(tb_period_send_time_ms.Text));
            }
            
        }

        private void Btn_clean_recv_Click(object sender, EventArgs e)
        {
            tb_recv.Text = "";
            label_recv_count.Text = "R:0";
        }
    }
}
