namespace BlueSerial
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tb_recv = new System.Windows.Forms.TextBox();
            this.tb_send = new System.Windows.Forms.TextBox();
            this.cb_com_list = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cb_baud_list = new System.Windows.Forms.ComboBox();
            this.cb_stop_list = new System.Windows.Forms.ComboBox();
            this.cb_data_list = new System.Windows.Forms.ComboBox();
            this.cb_parity_list = new System.Windows.Forms.ComboBox();
            this.btn_open_com = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btn_send = new System.Windows.Forms.Button();
            this.btn_clean_send = new System.Windows.Forms.Button();
            this.btn_clean_recv = new System.Windows.Forms.Button();
            this.cbox_hex_display = new System.Windows.Forms.CheckBox();
            this.cbox_send_hex = new System.Windows.Forms.CheckBox();
            this.cbox_send_line = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cbox_timer_send = new System.Windows.Forms.CheckBox();
            this.tb_period_send_time_ms = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tb_recv
            // 
            this.tb_recv.BackColor = System.Drawing.Color.Black;
            this.tb_recv.ForeColor = System.Drawing.Color.Lime;
            this.tb_recv.Location = new System.Drawing.Point(3, 3);
            this.tb_recv.Multiline = true;
            this.tb_recv.Name = "tb_recv";
            this.tb_recv.ReadOnly = true;
            this.tb_recv.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tb_recv.Size = new System.Drawing.Size(480, 313);
            this.tb_recv.TabIndex = 0;
            // 
            // tb_send
            // 
            this.tb_send.Location = new System.Drawing.Point(3, 347);
            this.tb_send.Multiline = true;
            this.tb_send.Name = "tb_send";
            this.tb_send.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tb_send.Size = new System.Drawing.Size(480, 100);
            this.tb_send.TabIndex = 1;
            // 
            // cb_com_list
            // 
            this.cb_com_list.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_com_list.FormattingEnabled = true;
            this.cb_com_list.Location = new System.Drawing.Point(510, 29);
            this.cb_com_list.Name = "cb_com_list";
            this.cb_com_list.Size = new System.Drawing.Size(196, 20);
            this.cb_com_list.TabIndex = 2;
            this.cb_com_list.SelectedIndexChanged += new System.EventHandler(this.Cb_com_list_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(508, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "串口选择";
            // 
            // cb_baud_list
            // 
            this.cb_baud_list.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_baud_list.FormattingEnabled = true;
            this.cb_baud_list.Location = new System.Drawing.Point(584, 69);
            this.cb_baud_list.Name = "cb_baud_list";
            this.cb_baud_list.Size = new System.Drawing.Size(121, 20);
            this.cb_baud_list.TabIndex = 4;
            this.cb_baud_list.SelectedIndexChanged += new System.EventHandler(this.Cb_baud_list_SelectedIndexChanged);
            // 
            // cb_stop_list
            // 
            this.cb_stop_list.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_stop_list.FormattingEnabled = true;
            this.cb_stop_list.Location = new System.Drawing.Point(584, 107);
            this.cb_stop_list.Name = "cb_stop_list";
            this.cb_stop_list.Size = new System.Drawing.Size(121, 20);
            this.cb_stop_list.TabIndex = 5;
            this.cb_stop_list.SelectedIndexChanged += new System.EventHandler(this.Cb_stop_list_SelectedIndexChanged);
            // 
            // cb_data_list
            // 
            this.cb_data_list.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_data_list.FormattingEnabled = true;
            this.cb_data_list.Location = new System.Drawing.Point(584, 146);
            this.cb_data_list.Name = "cb_data_list";
            this.cb_data_list.Size = new System.Drawing.Size(121, 20);
            this.cb_data_list.TabIndex = 6;
            this.cb_data_list.SelectedIndexChanged += new System.EventHandler(this.Cb_data_list_SelectedIndexChanged);
            // 
            // cb_parity_list
            // 
            this.cb_parity_list.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_parity_list.FormattingEnabled = true;
            this.cb_parity_list.Location = new System.Drawing.Point(584, 184);
            this.cb_parity_list.Name = "cb_parity_list";
            this.cb_parity_list.Size = new System.Drawing.Size(121, 20);
            this.cb_parity_list.TabIndex = 7;
            this.cb_parity_list.SelectedIndexChanged += new System.EventHandler(this.Cb_parity_list_SelectedIndexChanged);
            // 
            // btn_open_com
            // 
            this.btn_open_com.Location = new System.Drawing.Point(584, 220);
            this.btn_open_com.Name = "btn_open_com";
            this.btn_open_com.Size = new System.Drawing.Size(121, 23);
            this.btn_open_com.TabIndex = 8;
            this.btn_open_com.Text = "打开串口";
            this.btn_open_com.UseVisualStyleBackColor = true;
            this.btn_open_com.Click += new System.EventHandler(this.Btn_open_com_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(508, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 12);
            this.label2.TabIndex = 9;
            this.label2.Text = "波特率";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(508, 110);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 12);
            this.label3.TabIndex = 10;
            this.label3.Text = "停止位";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(508, 149);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 11;
            this.label4.Text = "数据位";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(508, 187);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 12);
            this.label5.TabIndex = 12;
            this.label5.Text = "奇偶校验";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(508, 225);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(53, 12);
            this.label6.TabIndex = 13;
            this.label6.Text = "串口操作";
            // 
            // btn_send
            // 
            this.btn_send.Location = new System.Drawing.Point(500, 347);
            this.btn_send.Name = "btn_send";
            this.btn_send.Size = new System.Drawing.Size(75, 24);
            this.btn_send.TabIndex = 14;
            this.btn_send.Text = "发送";
            this.btn_send.UseVisualStyleBackColor = true;
            this.btn_send.Click += new System.EventHandler(this.Btn_send_Click);
            // 
            // btn_clean_send
            // 
            this.btn_clean_send.Location = new System.Drawing.Point(500, 423);
            this.btn_clean_send.Name = "btn_clean_send";
            this.btn_clean_send.Size = new System.Drawing.Size(75, 24);
            this.btn_clean_send.TabIndex = 15;
            this.btn_clean_send.Text = "清除发送";
            this.btn_clean_send.UseVisualStyleBackColor = true;
            this.btn_clean_send.Click += new System.EventHandler(this.Btn_clean_send_Click);
            // 
            // btn_clean_recv
            // 
            this.btn_clean_recv.Location = new System.Drawing.Point(584, 255);
            this.btn_clean_recv.Name = "btn_clean_recv";
            this.btn_clean_recv.Size = new System.Drawing.Size(121, 23);
            this.btn_clean_recv.TabIndex = 16;
            this.btn_clean_recv.Text = "清除接收";
            this.btn_clean_recv.UseVisualStyleBackColor = true;
            this.btn_clean_recv.Click += new System.EventHandler(this.Btn_clean_recv_Click);
            // 
            // cbox_hex_display
            // 
            this.cbox_hex_display.AutoSize = true;
            this.cbox_hex_display.Location = new System.Drawing.Point(584, 300);
            this.cbox_hex_display.Name = "cbox_hex_display";
            this.cbox_hex_display.Size = new System.Drawing.Size(84, 16);
            this.cbox_hex_display.TabIndex = 17;
            this.cbox_hex_display.Text = "16进制显示";
            this.cbox_hex_display.UseVisualStyleBackColor = true;
            this.cbox_hex_display.CheckedChanged += new System.EventHandler(this.Cbox_hex_display_CheckedChanged);
            // 
            // cbox_send_hex
            // 
            this.cbox_send_hex.AutoSize = true;
            this.cbox_send_hex.Location = new System.Drawing.Point(3, 454);
            this.cbox_send_hex.Name = "cbox_send_hex";
            this.cbox_send_hex.Size = new System.Drawing.Size(84, 16);
            this.cbox_send_hex.TabIndex = 18;
            this.cbox_send_hex.Text = "16进制发送";
            this.cbox_send_hex.UseVisualStyleBackColor = true;
            // 
            // cbox_send_line
            // 
            this.cbox_send_line.AutoSize = true;
            this.cbox_send_line.Location = new System.Drawing.Point(93, 454);
            this.cbox_send_line.Name = "cbox_send_line";
            this.cbox_send_line.Size = new System.Drawing.Size(72, 16);
            this.cbox_send_line.TabIndex = 19;
            this.cbox_send_line.Text = "发送新行";
            this.cbox_send_line.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(628, 470);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(77, 12);
            this.label7.TabIndex = 20;
            this.label7.Text = "串口调试助手";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // cbox_timer_send
            // 
            this.cbox_timer_send.AutoSize = true;
            this.cbox_timer_send.Location = new System.Drawing.Point(171, 454);
            this.cbox_timer_send.Name = "cbox_timer_send";
            this.cbox_timer_send.Size = new System.Drawing.Size(72, 16);
            this.cbox_timer_send.TabIndex = 21;
            this.cbox_timer_send.Text = "定时发送";
            this.cbox_timer_send.UseVisualStyleBackColor = true;
            this.cbox_timer_send.CheckedChanged += new System.EventHandler(this.Cbox_timer_send_CheckedChanged);
            // 
            // tb_period_send_time_ms
            // 
            this.tb_period_send_time_ms.Location = new System.Drawing.Point(249, 452);
            this.tb_period_send_time_ms.Name = "tb_period_send_time_ms";
            this.tb_period_send_time_ms.Size = new System.Drawing.Size(50, 21);
            this.tb_period_send_time_ms.TabIndex = 22;
            this.tb_period_send_time_ms.Text = "1000";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(306, 457);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(17, 12);
            this.label8.TabIndex = 23;
            this.label8.Text = "ms";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(718, 491);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tb_period_send_time_ms);
            this.Controls.Add(this.cbox_timer_send);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cbox_send_line);
            this.Controls.Add(this.cbox_send_hex);
            this.Controls.Add(this.cbox_hex_display);
            this.Controls.Add(this.btn_clean_recv);
            this.Controls.Add(this.btn_clean_send);
            this.Controls.Add(this.btn_send);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btn_open_com);
            this.Controls.Add(this.cb_parity_list);
            this.Controls.Add(this.cb_data_list);
            this.Controls.Add(this.cb_stop_list);
            this.Controls.Add(this.cb_baud_list);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cb_com_list);
            this.Controls.Add(this.tb_send);
            this.Controls.Add(this.tb_recv);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "BCom";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox tb_send;
        private System.Windows.Forms.ComboBox cb_com_list;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cb_baud_list;
        private System.Windows.Forms.ComboBox cb_stop_list;
        private System.Windows.Forms.ComboBox cb_data_list;
        private System.Windows.Forms.ComboBox cb_parity_list;
        private System.Windows.Forms.Button btn_open_com;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btn_send;
        private System.Windows.Forms.Button btn_clean_send;
        private System.Windows.Forms.Button btn_clean_recv;
        private System.Windows.Forms.CheckBox cbox_hex_display;
        private System.Windows.Forms.TextBox tb_recv;
        private System.Windows.Forms.CheckBox cbox_send_hex;
        private System.Windows.Forms.CheckBox cbox_send_line;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox cbox_timer_send;
        private System.Windows.Forms.TextBox tb_period_send_time_ms;
        private System.Windows.Forms.Label label8;
    }
}

