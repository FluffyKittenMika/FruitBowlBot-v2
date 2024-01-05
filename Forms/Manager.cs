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

        private void button1_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();
            try
            {
                foreach (var item in Commands.TextToWeird.MissingWords)
                {
                    TreeNode n = new TreeNode();
                    n.Text = item.Key;
                    n.Nodes.Add(new TreeNode("Sum:" + item.Value));
                    treeView1.Nodes.Add(n);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
           

        }
    }
}
