﻿using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace MibbleBrowser
{
   public partial class frmMain : Form
   {
      public frmMain()
      {
         InitializeComponent();
         mibTreeBuilder = new MibTreeBuilder(treeMibs);
         mibTreeBuilder.LoadMibFile("RFC1213-MIB");
         mibTreeBuilder.LoadMibFile("HOST-RESOURCES-MIB");
      }

      private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
      {

      }

      private void fIleToolStripMenuItem_Click(object sender, EventArgs e)
      {

      }

      private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
      {

      }

      private void loadMIBToolStripMenuItem_Click(object sender, EventArgs e)
      {
         DialogResult result = openFileDialogMain.ShowDialog();
         if (result == DialogResult.OK) // Test result.
         {
            string file = openFileDialogMain.FileName;
            mibTreeBuilder.LoadMibFile(file);
         }
      }

      private void treeMibs_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
      {
         MibNode n = e.Node as MibNode;
         if (n == null)
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
