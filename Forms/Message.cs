using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FruitBowlBot_v2.Forms
{
    public partial class Message : Form
    {
        public Message(TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            InitializeComponent();
            textBox1.Text = e.ChatMessage.Username + Environment.NewLine +  e.ChatMessage.Message;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
