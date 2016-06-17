using System.Windows.Forms;

namespace trspainter
{
    partial class AboutBox1 : Form
    {
        public AboutBox1()
        {
            InitializeComponent();
            Text = @"About";
            labelProductName.Text = @"TRSPainty";
            labelVersion.Text = @"Version 1.0";
            labelCopyright.Text = @"2016 Sir Morris";
        }

        private void okButton_Click(object sender, System.EventArgs e)
        {
            Close();
        }
    }
}
