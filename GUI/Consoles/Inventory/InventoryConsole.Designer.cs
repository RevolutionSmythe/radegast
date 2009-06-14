﻿namespace Radegast
{
    partial class InventoryConsole
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.invTree = new System.Windows.Forms.TreeView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.pnlDetail = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnProfile = new System.Windows.Forms.Button();
            this.txtCreator = new System.Windows.Forms.TextBox();
            this.txtAssetID = new System.Windows.Forms.TextBox();
            this.txtItemName = new System.Windows.Forms.TextBox();
            this.lblAsset = new System.Windows.Forms.Label();
            this.lblCreator = new System.Windows.Forms.Label();
            this.lblItemName = new System.Windows.Forms.Label();
            this.folderContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.folderContextTitle = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.folderContextDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.lblCreated = new System.Windows.Forms.Label();
            this.txtCreated = new System.Windows.Forms.TextBox();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.folderContext.SuspendLayout();
            this.SuspendLayout();
            // 
            // invTree
            // 
            this.invTree.AllowDrop = true;
            this.invTree.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(62)))));
            this.invTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.invTree.ForeColor = System.Drawing.Color.White;
            this.invTree.LineColor = System.Drawing.Color.White;
            this.invTree.Location = new System.Drawing.Point(0, 0);
            this.invTree.Name = "invTree";
            this.invTree.Size = new System.Drawing.Size(331, 483);
            this.invTree.TabIndex = 0;
            this.invTree.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.invTree_ItemDrag);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.invTree);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.pnlDetail);
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Size = new System.Drawing.Size(756, 483);
            this.splitContainer1.SplitterDistance = 331;
            this.splitContainer1.TabIndex = 1;
            // 
            // pnlDetail
            // 
            this.pnlDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDetail.Location = new System.Drawing.Point(0, 0);
            this.pnlDetail.Name = "pnlDetail";
            this.pnlDetail.Size = new System.Drawing.Size(421, 336);
            this.pnlDetail.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnProfile);
            this.panel1.Controls.Add(this.txtCreator);
            this.panel1.Controls.Add(this.txtCreated);
            this.panel1.Controls.Add(this.txtAssetID);
            this.panel1.Controls.Add(this.lblCreated);
            this.panel1.Controls.Add(this.txtItemName);
            this.panel1.Controls.Add(this.lblAsset);
            this.panel1.Controls.Add(this.lblCreator);
            this.panel1.Controls.Add(this.lblItemName);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 336);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(421, 147);
            this.panel1.TabIndex = 0;
            // 
            // btnProfile
            // 
            this.btnProfile.AccessibleDescription = "Open profile";
            this.btnProfile.Enabled = false;
            this.btnProfile.Image = global::Radegast.Properties.Resources.applications_16;
            this.btnProfile.Location = new System.Drawing.Point(54, 36);
            this.btnProfile.Name = "btnProfile";
            this.btnProfile.Size = new System.Drawing.Size(26, 23);
            this.btnProfile.TabIndex = 2;
            this.btnProfile.UseVisualStyleBackColor = true;
            this.btnProfile.Click += new System.EventHandler(this.btnProfile_Click);
            // 
            // txtCreator
            // 
            this.txtCreator.BackColor = System.Drawing.SystemColors.Window;
            this.txtCreator.Location = new System.Drawing.Point(80, 36);
            this.txtCreator.Name = "txtCreator";
            this.txtCreator.ReadOnly = true;
            this.txtCreator.Size = new System.Drawing.Size(338, 20);
            this.txtCreator.TabIndex = 1;
            // 
            // txtAssetID
            // 
            this.txtAssetID.Location = new System.Drawing.Point(80, 88);
            this.txtAssetID.Name = "txtAssetID";
            this.txtAssetID.ReadOnly = true;
            this.txtAssetID.Size = new System.Drawing.Size(338, 20);
            this.txtAssetID.TabIndex = 1;
            // 
            // txtItemName
            // 
            this.txtItemName.Location = new System.Drawing.Point(80, 10);
            this.txtItemName.Name = "txtItemName";
            this.txtItemName.Size = new System.Drawing.Size(338, 20);
            this.txtItemName.TabIndex = 1;
            // 
            // lblAsset
            // 
            this.lblAsset.AutoSize = true;
            this.lblAsset.Location = new System.Drawing.Point(3, 88);
            this.lblAsset.Name = "lblAsset";
            this.lblAsset.Size = new System.Drawing.Size(47, 13);
            this.lblAsset.TabIndex = 0;
            this.lblAsset.Text = "Asset ID";
            // 
            // lblCreator
            // 
            this.lblCreator.AutoSize = true;
            this.lblCreator.Location = new System.Drawing.Point(3, 36);
            this.lblCreator.Name = "lblCreator";
            this.lblCreator.Size = new System.Drawing.Size(41, 13);
            this.lblCreator.TabIndex = 0;
            this.lblCreator.Text = "Creator";
            // 
            // lblItemName
            // 
            this.lblItemName.AutoSize = true;
            this.lblItemName.Location = new System.Drawing.Point(3, 10);
            this.lblItemName.Name = "lblItemName";
            this.lblItemName.Size = new System.Drawing.Size(27, 13);
            this.lblItemName.TabIndex = 0;
            this.lblItemName.Text = "Item";
            // 
            // folderContext
            // 
            this.folderContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.folderContextTitle,
            this.toolStripSeparator1,
            this.refreshToolStripMenuItem,
            this.folderContextDelete});
            this.folderContext.Name = "folderContext";
            this.folderContext.ShowImageMargin = false;
            this.folderContext.Size = new System.Drawing.Size(136, 72);
            this.folderContext.Text = "Inventory Folder";
            // 
            // folderContextTitle
            // 
            this.folderContextTitle.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.folderContextTitle.Name = "folderContextTitle";
            this.folderContextTitle.ReadOnly = true;
            this.folderContextTitle.Size = new System.Drawing.Size(100, 16);
            this.folderContextTitle.Text = "Title";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(132, 6);
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.refreshToolStripMenuItem.Text = "Refresh";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // folderContextDelete
            // 
            this.folderContextDelete.Name = "folderContextDelete";
            this.folderContextDelete.Size = new System.Drawing.Size(135, 22);
            this.folderContextDelete.Text = "Delete";
            // 
            // lblCreated
            // 
            this.lblCreated.AutoSize = true;
            this.lblCreated.Location = new System.Drawing.Point(3, 62);
            this.lblCreated.Name = "lblCreated";
            this.lblCreated.Size = new System.Drawing.Size(44, 13);
            this.lblCreated.TabIndex = 0;
            this.lblCreated.Text = "Created";
            // 
            // txtCreated
            // 
            this.txtCreated.Location = new System.Drawing.Point(80, 62);
            this.txtCreated.Name = "txtCreated";
            this.txtCreated.ReadOnly = true;
            this.txtCreated.Size = new System.Drawing.Size(144, 20);
            this.txtCreated.TabIndex = 1;
            // 
            // InventoryConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "InventoryConsole";
            this.Size = new System.Drawing.Size(756, 483);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.folderContext.ResumeLayout(false);
            this.folderContext.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView invTree;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ContextMenuStrip folderContext;
        private System.Windows.Forms.ToolStripMenuItem folderContextDelete;
        private System.Windows.Forms.ToolStripTextBox folderContextTitle;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox txtItemName;
        private System.Windows.Forms.Label lblCreator;
        private System.Windows.Forms.Label lblItemName;
        private System.Windows.Forms.TextBox txtCreator;
        private System.Windows.Forms.TextBox txtAssetID;
        private System.Windows.Forms.Label lblAsset;
        private System.Windows.Forms.Panel pnlDetail;
        private System.Windows.Forms.Button btnProfile;
        private System.Windows.Forms.TextBox txtCreated;
        private System.Windows.Forms.Label lblCreated;
    }
}
