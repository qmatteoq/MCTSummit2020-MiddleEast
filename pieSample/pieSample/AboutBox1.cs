using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pieSample
{
    partial class AboutBox1 : Form
    {
        public AboutBox1()
        {
            InitializeComponent();

            var descriptionText = new StringBuilder();
            var process = Process.GetCurrentProcess();
            foreach(ProcessModule module in process.Modules)
            {
                descriptionText.AppendLine(module.FileName);
            }
            this.textBoxDescription.Text = descriptionText.ToString();
        }
    }
}
