using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace MibbleBrowser
{
   public partial class FrmMain : Form
   {
      public FrmMain()
      {
         InitializeComponent();
         mibTreeBuilder = new MibTreeBuilder(treeMibs);
         mibTreeBuilder.LoadMibFile("RFC1213-MIB");
         mibTreeBuilder.LoadMibFile("HOST-RESOURCES-MIB");
      }

      private void SplitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
      {

      }

      private void FIleToolStripMenuItem_Click(object sender, EventArgs e)
      {

      }

      private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
      {

      }

      private void LoadMIBToolStripMenuItem_Click(object sender, EventArgs e)
      {
         DialogResult result = openFileDialogMain.ShowDialog();
         if (result == DialogResult.OK) // Test result.
         {
            string file = openFileDialogMain.FileName;
            mibTreeBuilder.LoadMibFile(file);
         }
      }

      private void TreeMibs_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
      {
         if (!(e.Node is MibNode n))
         {
            return;
         }

         string t = string.Join(
             "\r\n",
             n.Description
             .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
             .Select(s => s.Trim()));

         txtNodeInfo.Text = t;
      }
   }
}
