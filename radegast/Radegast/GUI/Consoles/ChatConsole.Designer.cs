// 
// Radegast Metaverse Client
// Copyright (c) 2009-2011, Radegast Development Team
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the application "Radegast", nor the names of its
//       contributors may be used to endorse or promote products derived from
//       this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// $Id$
//
using System.Windows.Forms;

namespace Radegast
{
    partial class ChatConsole
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChatConsole));
            this.btnSay = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.rtbChat = new System.Windows.Forms.RichTextBox();
            this.lvwObjects = new Radegast.ListViewNoFlicker();
            this.avatarContext = new Radegast.RadegastContextMenuStrip(this.components);
            this.ctxProfile = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPay = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxStartIM = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxFollow = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTextures = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAttach = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMaster = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAnim = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPoint = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxSource = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxOfferTP = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTeleportTo = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxEject = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxBan = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxEstateEject = new System.Windows.Forms.ToolStripMenuItem();
            this.muteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tbtnStartIM = new System.Windows.Forms.ToolStripButton();
            this.tbtnProfile = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tbtnFollow = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tbtnTextures = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tbtnAttach = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.tbtnMaster = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.tbtnAnim = new System.Windows.Forms.ToolStripButton();
            this.pnlMovement = new System.Windows.Forms.Panel();
            this.btnMoveBack = new System.Windows.Forms.Button();
            this.btnFwd = new System.Windows.Forms.Button();
            this.btnTurnRight = new System.Windows.Forms.Button();
            this.btnTurnLeft = new System.Windows.Forms.Button();
            this.pnlChatInput = new System.Windows.Forms.Panel();
            this.cbChatType = new System.Windows.Forms.ComboBox();
            this.cbxInput = new Radegast.ChatInputBox();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.avatarContext.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.pnlMovement.SuspendLayout();
            this.pnlChatInput.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSay
            // 
            this.btnSay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSay.Enabled = false;
            this.btnSay.Location = new System.Drawing.Point(418, 0);
            this.btnSay.Name = "btnSay";
            this.btnSay.Size = new System.Drawing.Size(76, 24);
            this.btnSay.TabIndex = 10;
            this.btnSay.Text = "Say";
            this.btnSay.Click += new System.EventHandler(this.btnSay_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.rtbChat);
            this.splitContainer1.Panel1.SizeChanged += new System.EventHandler(this.splitContainer1_Panel1_SizeChanged);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.lvwObjects);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip1);
            this.splitContainer1.Panel2.Controls.Add(this.pnlMovement);
            this.splitContainer1.Size = new System.Drawing.Size(576, 354);
            this.splitContainer1.SplitterDistance = 445;
            this.splitContainer1.TabIndex = 1;
            this.splitContainer1.TabStop = false;
            // 
            // rtbChat
            // 
            this.rtbChat.AccessibleName = "Chat history";
            this.rtbChat.BackColor = System.Drawing.SystemColors.Window;
            this.rtbChat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbChat.HideSelection = false;
            this.rtbChat.Location = new System.Drawing.Point(0, 0);
            this.rtbChat.Name = "rtbChat";
            this.rtbChat.ReadOnly = true;
            this.rtbChat.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbChat.Size = new System.Drawing.Size(445, 354);
            this.rtbChat.TabIndex = 4;
            this.rtbChat.Text = "";
            this.rtbChat.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.rtbChat_LinkClicked);
            // 
            // lvwObjects
            // 
            this.lvwObjects.AccessibleName = "Nearby people";
            this.lvwObjects.AllowDrop = true;
            this.lvwObjects.ContextMenuStrip = this.avatarContext;
            this.lvwObjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvwObjects.FullRowSelect = true;
            this.lvwObjects.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvwObjects.HideSelection = false;
            this.lvwObjects.LabelWrap = false;
            this.lvwObjects.Location = new System.Drawing.Point(0, 0);
            this.lvwObjects.MultiSelect = false;
            this.lvwObjects.Name = "lvwObjects";
            this.lvwObjects.Size = new System.Drawing.Size(86, 317);
            this.lvwObjects.TabIndex = 0;
            this.lvwObjects.UseCompatibleStateImageBehavior = false;
            this.lvwObjects.View = System.Windows.Forms.View.List;
            this.lvwObjects.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvwObjects_MouseDoubleClick);
            this.lvwObjects.SelectedIndexChanged += new System.EventHandler(this.lvwObjects_SelectedIndexChanged);
            this.lvwObjects.DragDrop += new System.Windows.Forms.DragEventHandler(this.lvwObjects_DragDrop);
            this.lvwObjects.DragOver += new System.Windows.Forms.DragEventHandler(this.lvwObjects_DragOver);
            // 
            // avatarContext
            // 
            this.avatarContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxProfile,
            this.ctxPay,
            this.ctxStartIM,
            this.ctxFollow,
            this.ctxTextures,
            this.ctxAttach,
            this.ctxMaster,
            this.ctxAnim,
            this.ctxPoint,
            this.ctxSource,
            this.ctxOfferTP,
            this.ctxTeleportTo,
            this.ctxEject,
            this.ctxBan,
            this.ctxEstateEject,
            this.muteToolStripMenuItem});
            this.avatarContext.Name = "avatarContext";
            this.avatarContext.Size = new System.Drawing.Size(163, 378);
            this.avatarContext.Opening += new System.ComponentModel.CancelEventHandler(this.avatarContext_Opening);
            // 
            // ctxProfile
            // 
            this.ctxProfile.Name = "ctxProfile";
            this.ctxProfile.Size = new System.Drawing.Size(162, 22);
            this.ctxProfile.Text = "Profile";
            this.ctxProfile.Click += new System.EventHandler(this.tbtnProfile_Click);
            // 
            // ctxPay
            // 
            this.ctxPay.Enabled = false;
            this.ctxPay.Name = "ctxPay";
            this.ctxPay.Size = new System.Drawing.Size(162, 22);
            this.ctxPay.Text = "Pay";
            this.ctxPay.Click += new System.EventHandler(this.ctxPay_Click);
            // 
            // ctxStartIM
            // 
            this.ctxStartIM.Name = "ctxStartIM";
            this.ctxStartIM.Size = new System.Drawing.Size(162, 22);
            this.ctxStartIM.Text = "Start IM";
            this.ctxStartIM.Click += new System.EventHandler(this.tbtnStartIM_Click);
            // 
            // ctxFollow
            // 
            this.ctxFollow.Name = "ctxFollow";
            this.ctxFollow.Size = new System.Drawing.Size(162, 22);
            this.ctxFollow.Text = "Follow";
            this.ctxFollow.Click += new System.EventHandler(this.tbtnFollow_Click);
            // 
            // ctxTextures
            // 
            this.ctxTextures.Name = "ctxTextures";
            this.ctxTextures.Size = new System.Drawing.Size(162, 22);
            this.ctxTextures.Text = "Textures";
            this.ctxTextures.Click += new System.EventHandler(this.dumpOufitBtn_Click);
            // 
            // ctxAttach
            // 
            this.ctxAttach.Name = "ctxAttach";
            this.ctxAttach.Size = new System.Drawing.Size(162, 22);
            this.ctxAttach.Text = "Attachments";
            this.ctxAttach.Click += new System.EventHandler(this.tbtnAttach_Click);
            // 
            // ctxMaster
            // 
            this.ctxMaster.Name = "ctxMaster";
            this.ctxMaster.Size = new System.Drawing.Size(162, 22);
            this.ctxMaster.Text = "Master controls";
            this.ctxMaster.Click += new System.EventHandler(this.tbtnMaster_Click);
            // 
            // ctxAnim
            // 
            this.ctxAnim.Name = "ctxAnim";
            this.ctxAnim.Size = new System.Drawing.Size(162, 22);
            this.ctxAnim.Text = "Animations";
            this.ctxAnim.Click += new System.EventHandler(this.tbtnAnim_Click);
            // 
            // ctxPoint
            // 
            this.ctxPoint.Name = "ctxPoint";
            this.ctxPoint.Size = new System.Drawing.Size(162, 22);
            this.ctxPoint.Text = "Point at";
            this.ctxPoint.Click += new System.EventHandler(this.ctxPoint_Click);
            // 
            // ctxSource
            // 
            this.ctxSource.Name = "ctxSource";
            this.ctxSource.Size = new System.Drawing.Size(162, 22);
            this.ctxSource.Text = "Set as source";
            this.ctxSource.ToolTipText = " Set as source ";
            this.ctxSource.Click += new System.EventHandler(this.ctxSource_Click);
            // 
            // ctxOfferTP
            // 
            this.ctxOfferTP.Name = "ctxOfferTP";
            this.ctxOfferTP.Size = new System.Drawing.Size(162, 22);
            this.ctxOfferTP.Text = "Offer Teleport";
            this.ctxOfferTP.ToolTipText = " Offer Teleport ";
            this.ctxOfferTP.Click += new System.EventHandler(this.ctxOfferTP_Click);
            // 
            // ctxTeleportTo
            // 
            this.ctxTeleportTo.Name = "ctxTeleportTo";
            this.ctxTeleportTo.Size = new System.Drawing.Size(162, 22);
            this.ctxTeleportTo.Text = "Teleport To";
            this.ctxTeleportTo.ToolTipText = " Teleport To ";
            this.ctxTeleportTo.Click += new System.EventHandler(this.ctxTeleportTo_Click);
            // 
            // ctxEject
            // 
            this.ctxEject.Name = "ctxEject";
            this.ctxEject.Size = new System.Drawing.Size(162, 22);
            this.ctxEject.Text = "Eject";
            this.ctxEject.ToolTipText = " Eject ";
            this.ctxEject.Click += new System.EventHandler(this.ctxEject_Click);
            // 
            // ctxBan
            // 
            this.ctxBan.Name = "ctxBan";
            this.ctxBan.Size = new System.Drawing.Size(162, 22);
            this.ctxBan.Text = "Ban";
            this.ctxBan.ToolTipText = " Ban ";
            this.ctxBan.Click += new System.EventHandler(this.ctxBan_Click);
            // 
            // ctxEstateEject
            // 
            this.ctxEstateEject.Name = "ctxEstateEject";
            this.ctxEstateEject.Size = new System.Drawing.Size(162, 22);
            this.ctxEstateEject.Text = "Eject from estate";
            this.ctxEstateEject.ToolTipText = " Eject from estate ";
            this.ctxEstateEject.Click += new System.EventHandler(this.ctxEstateEject_Click);
            // 
            // muteToolStripMenuItem
            // 
            this.muteToolStripMenuItem.Name = "muteToolStripMenuItem";
            this.muteToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.muteToolStripMenuItem.Text = "Mute";
            this.muteToolStripMenuItem.ToolTipText = " Mute ";
            this.muteToolStripMenuItem.Click += new System.EventHandler(this.muteToolStripMenuItem_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Right;
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbtnStartIM,
            this.tbtnProfile,
            this.toolStripSeparator1,
            this.tbtnFollow,
            this.toolStripSeparator2,
            this.tbtnTextures,
            this.toolStripSeparator3,
            this.tbtnAttach,
            this.toolStripSeparator4,
            this.tbtnMaster,
            this.toolStripSeparator5,
            this.tbtnAnim});
            this.toolStrip1.Location = new System.Drawing.Point(86, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(41, 317);
            this.toolStrip1.TabIndex = 20;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tbtnStartIM
            // 
            this.tbtnStartIM.AccessibleName = "Start IM";
            this.tbtnStartIM.AutoToolTip = false;
            this.tbtnStartIM.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbtnStartIM.Enabled = false;
            this.tbtnStartIM.Image = global::Radegast.Properties.Resources.computer_16;
            this.tbtnStartIM.Name = "tbtnStartIM";
            this.tbtnStartIM.Size = new System.Drawing.Size(38, 20);
            this.tbtnStartIM.ToolTipText = "Start IM";
            this.tbtnStartIM.Click += new System.EventHandler(this.tbtnStartIM_Click);
            // 
            // tbtnProfile
            // 
            this.tbtnProfile.AccessibleName = "View profile";
            this.tbtnProfile.AutoToolTip = false;
            this.tbtnProfile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbtnProfile.Enabled = false;
            this.tbtnProfile.Image = global::Radegast.Properties.Resources.applications_16;
            this.tbtnProfile.Name = "tbtnProfile";
            this.tbtnProfile.Size = new System.Drawing.Size(38, 20);
            this.tbtnProfile.ToolTipText = "View Profile";
            this.tbtnProfile.Click += new System.EventHandler(this.tbtnProfile_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(38, 6);
            // 
            // tbtnFollow
            // 
            this.tbtnFollow.AccessibleName = "Follow";
            this.tbtnFollow.AutoToolTip = false;
            this.tbtnFollow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbtnFollow.Enabled = false;
            this.tbtnFollow.Image = global::Radegast.Properties.Resources.arrow_forward_16;
            this.tbtnFollow.Name = "tbtnFollow";
            this.tbtnFollow.Size = new System.Drawing.Size(38, 20);
            this.tbtnFollow.ToolTipText = "Follow";
            this.tbtnFollow.Click += new System.EventHandler(this.tbtnFollow_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(38, 6);
            // 
            // tbtnTextures
            // 
            this.tbtnTextures.AccessibleName = "Textures";
            this.tbtnTextures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbtnTextures.Enabled = false;
            this.tbtnTextures.Image = ((System.Drawing.Image)(resources.GetObject("tbtnTextures.Image")));
            this.tbtnTextures.Name = "tbtnTextures";
            this.tbtnTextures.Size = new System.Drawing.Size(38, 19);
            this.tbtnTextures.Text = "Txtr";
            this.tbtnTextures.Click += new System.EventHandler(this.dumpOufitBtn_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(38, 6);
            // 
            // tbtnAttach
            // 
            this.tbtnAttach.AccessibleName = "Attachments";
            this.tbtnAttach.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbtnAttach.Enabled = false;
            this.tbtnAttach.Image = ((System.Drawing.Image)(resources.GetObject("tbtnAttach.Image")));
            this.tbtnAttach.Name = "tbtnAttach";
            this.tbtnAttach.Size = new System.Drawing.Size(38, 19);
            this.tbtnAttach.Text = "Attn";
            this.tbtnAttach.ToolTipText = "List avatar attachments";
            this.tbtnAttach.Click += new System.EventHandler(this.tbtnAttach_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(38, 6);
            // 
            // tbtnMaster
            // 
            this.tbtnMaster.AccessibleName = "Master";
            this.tbtnMaster.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbtnMaster.Enabled = false;
            this.tbtnMaster.Image = ((System.Drawing.Image)(resources.GetObject("tbtnMaster.Image")));
            this.tbtnMaster.Name = "tbtnMaster";
            this.tbtnMaster.Size = new System.Drawing.Size(38, 19);
            this.tbtnMaster.Text = "Mstr";
            this.tbtnMaster.Click += new System.EventHandler(this.tbtnMaster_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(38, 6);
            // 
            // tbtnAnim
            // 
            this.tbtnAnim.AccessibleName = "Animations";
            this.tbtnAnim.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbtnAnim.Enabled = false;
            this.tbtnAnim.Image = ((System.Drawing.Image)(resources.GetObject("tbtnAnim.Image")));
            this.tbtnAnim.Name = "tbtnAnim";
            this.tbtnAnim.Size = new System.Drawing.Size(38, 19);
            this.tbtnAnim.Text = "Anim";
            this.tbtnAnim.ToolTipText = "List Avatar Animatoions";
            this.tbtnAnim.Click += new System.EventHandler(this.tbtnAnim_Click);
            // 
            // pnlMovement
            // 
            this.pnlMovement.Controls.Add(this.btnMoveBack);
            this.pnlMovement.Controls.Add(this.btnFwd);
            this.pnlMovement.Controls.Add(this.btnTurnRight);
            this.pnlMovement.Controls.Add(this.btnTurnLeft);
            this.pnlMovement.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlMovement.Location = new System.Drawing.Point(0, 317);
            this.pnlMovement.Name = "pnlMovement";
            this.pnlMovement.Size = new System.Drawing.Size(127, 37);
            this.pnlMovement.TabIndex = 11;
            this.pnlMovement.Click += new System.EventHandler(this.pnlMovement_Click);
            // 
            // btnMoveBack
            // 
            this.btnMoveBack.AccessibleName = "Walk backwards";
            this.btnMoveBack.Font = new System.Drawing.Font("Tahoma", 6F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnMoveBack.Location = new System.Drawing.Point(36, 15);
            this.btnMoveBack.Margin = new System.Windows.Forms.Padding(0);
            this.btnMoveBack.Name = "btnMoveBack";
            this.btnMoveBack.Size = new System.Drawing.Size(31, 19);
            this.btnMoveBack.TabIndex = 2;
            this.btnMoveBack.TabStop = false;
            this.btnMoveBack.Text = "R";
            this.btnMoveBack.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnMoveBack_MouseDown);
            this.btnMoveBack.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnMoveBack_MouseUp);
            // 
            // btnFwd
            // 
            this.btnFwd.AccessibleName = "Walk forward";
            this.btnFwd.Font = new System.Drawing.Font("Tahoma", 6F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFwd.Location = new System.Drawing.Point(36, 0);
            this.btnFwd.Margin = new System.Windows.Forms.Padding(0);
            this.btnFwd.Name = "btnFwd";
            this.btnFwd.Size = new System.Drawing.Size(31, 19);
            this.btnFwd.TabIndex = 1;
            this.btnFwd.TabStop = false;
            this.btnFwd.Text = "^";
            this.btnFwd.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnFwd_MouseDown);
            this.btnFwd.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnFwd_MouseUp);
            // 
            // btnTurnRight
            // 
            this.btnTurnRight.AccessibleName = "Turn right";
            this.btnTurnRight.Font = new System.Drawing.Font("Tahoma", 6F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTurnRight.Location = new System.Drawing.Point(67, 15);
            this.btnTurnRight.Margin = new System.Windows.Forms.Padding(0);
            this.btnTurnRight.Name = "btnTurnRight";
            this.btnTurnRight.Size = new System.Drawing.Size(31, 19);
            this.btnTurnRight.TabIndex = 4;
            this.btnTurnRight.TabStop = false;
            this.btnTurnRight.Text = ">>";
            this.btnTurnRight.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnTurnRight_MouseDown);
            this.btnTurnRight.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnTurnRight_MouseUp);
            // 
            // btnTurnLeft
            // 
            this.btnTurnLeft.AccessibleName = "Turn left";
            this.btnTurnLeft.Font = new System.Drawing.Font("Tahoma", 6F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTurnLeft.Location = new System.Drawing.Point(5, 15);
            this.btnTurnLeft.Margin = new System.Windows.Forms.Padding(0);
            this.btnTurnLeft.Name = "btnTurnLeft";
            this.btnTurnLeft.Size = new System.Drawing.Size(31, 19);
            this.btnTurnLeft.TabIndex = 3;
            this.btnTurnLeft.TabStop = false;
            this.btnTurnLeft.Text = "<<";
            this.btnTurnLeft.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnTurnLeft_MouseDown);
            this.btnTurnLeft.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnTurnLeft_MouseUp);
            // 
            // pnlChatInput
            // 
            this.pnlChatInput.Controls.Add(this.cbChatType);
            this.pnlChatInput.Controls.Add(this.cbxInput);
            this.pnlChatInput.Controls.Add(this.btnSay);
            this.pnlChatInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlChatInput.Location = new System.Drawing.Point(0, 354);
            this.pnlChatInput.Name = "pnlChatInput";
            this.pnlChatInput.Size = new System.Drawing.Size(576, 24);
            this.pnlChatInput.TabIndex = 0;
            // 
            // cbChatType
            // 
            this.cbChatType.AccessibleName = "Chat type";
            this.cbChatType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cbChatType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbChatType.Enabled = false;
            this.cbChatType.FormattingEnabled = true;
            this.cbChatType.Items.AddRange(new object[] {
            "Whisper",
            "Normal",
            "Shout"});
            this.cbChatType.Location = new System.Drawing.Point(498, 1);
            this.cbChatType.Name = "cbChatType";
            this.cbChatType.Size = new System.Drawing.Size(73, 21);
            this.cbChatType.TabIndex = 11;
            // 
            // cbxInput
            // 
            this.cbxInput.AccessibleName = "Chat input";
            this.cbxInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxInput.Enabled = false;
            this.cbxInput.Location = new System.Drawing.Point(0, 0);
            this.cbxInput.Name = "cbxInput";
            this.cbxInput.Size = new System.Drawing.Size(412, 21);
            this.cbxInput.TabIndex = 0;
            this.cbxInput.TextChanged += new System.EventHandler(this.cbxInput_TextChanged);
            this.cbxInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbxInput_KeyDown);
            this.cbxInput.SizeChanged += new System.EventHandler(this.cbxInput_SizeChanged);
            // 
            // ChatConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.pnlChatInput);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "ChatConsole";
            this.Size = new System.Drawing.Size(576, 378);
            this.VisibleChanged += new System.EventHandler(this.ChatConsole_VisibleChanged);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.avatarContext.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.pnlMovement.ResumeLayout(false);
            this.pnlChatInput.ResumeLayout(false);
            this.pnlChatInput.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public ListViewNoFlicker lvwObjects;
        public ChatInputBox cbxInput;
        public Button btnSay;
        public SplitContainer splitContainer1;
        public Panel pnlChatInput;
        public ToolStrip toolStrip1;
        public ToolStripButton tbtnStartIM;
        public ToolStripButton tbtnFollow;
        public ToolStripSeparator toolStripSeparator1;
        public ToolStripButton tbtnProfile;
        public ToolStripButton tbtnTextures;
        public ToolStripSeparator toolStripSeparator2;
        public ToolStripSeparator toolStripSeparator3;
        public ToolStripButton tbtnMaster;
        public ToolStripButton tbtnAttach;
        public ToolStripSeparator toolStripSeparator4;
        public Panel pnlMovement;
        public Button btnTurnLeft;
        public Button btnTurnRight;
        public Button btnFwd;
        public Button btnMoveBack;
        public ToolStripButton tbtnAnim;
        public ToolStripSeparator toolStripSeparator5;
        public RadegastContextMenuStrip avatarContext;
        public ToolStripMenuItem ctxProfile;
        public ToolStripMenuItem ctxStartIM;
        public ToolStripMenuItem ctxFollow;
        public ToolStripMenuItem ctxTextures;
        public ToolStripMenuItem ctxAttach;
        public ToolStripMenuItem ctxMaster;
        public ToolStripMenuItem ctxAnim;
        public ToolStripMenuItem ctxPoint;
        public ToolStripMenuItem ctxSource;
        public ToolStripMenuItem ctxPay;
        public ComboBox cbChatType;
        public RichTextBox rtbChat;
        private ToolStripMenuItem ctxOfferTP;
        private ToolStripMenuItem ctxTeleportTo;
        private ToolStripMenuItem ctxEject;
        private ToolStripMenuItem ctxBan;
        private ToolStripMenuItem ctxEstateEject;
        private ToolStripMenuItem muteToolStripMenuItem;
    }
}
