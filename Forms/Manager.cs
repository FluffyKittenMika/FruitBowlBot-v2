using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FruitBowlBot_v2.Forms
{
    public partial class Manager : Form
    {
        public Manager()
        {
            InitializeComponent();
        }

        private void Manager_Load(object sender, EventArgs e)
        {
            foreach (var item in ConnectionSystems.Bot._plugins)
            {
                listBox1.Items.Add(item.PluginName);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = ConnectionSystems.Bot._plugins[listBox1.SelectedIndex];
        }
    }
}
