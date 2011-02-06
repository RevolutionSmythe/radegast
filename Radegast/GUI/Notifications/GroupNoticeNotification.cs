﻿// 
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
// $Id: GroupInvitationNotification.cs 118 2009-07-20 00:39:00Z latifer $
//
using System.Windows.Forms;
using OpenMetaverse;

namespace Radegast
{
    public partial class ntfGroupNotice : Notification
    {
        private RadegastInstance instance;
        private GridClient client { get { return instance.Client; } }
        private InstantMessage msg;
        private AssetType type = AssetType.Unknown;
        private UUID destinationFolderID;


        public ntfGroupNotice(RadegastInstance instance, InstantMessage msg)
            : base(NotificationType.GroupNotice)
        {
            InitializeComponent();

            this.instance = instance;
            this.msg = msg;

            if (msg.BinaryBucket.Length > 18 && msg.BinaryBucket[0] != 0)
            {
                type = (AssetType)msg.BinaryBucket[1];
                destinationFolderID = client.Inventory.FindFolderForType(type);
                int icoIndx = InventoryConsole.GetItemImageIndex(type.ToString().ToLower());
                if (icoIndx >= 0)
                {
                    icnItem.Image = frmMain.ResourceImages.Images[icoIndx];
                    icnItem.Visible = true;
                }
                txtItemName.Text = Utils.BytesToString(msg.BinaryBucket, 18, msg.BinaryBucket.Length - 19);
                btnSave.Enabled = true;
                btnSave.Visible = icnItem.Visible = txtItemName.Visible = true;
            }

            string group = string.Empty;
            string text = msg.Message.Replace("\n", System.Environment.NewLine);
            int pos = msg.Message.IndexOf('|');
            string title = msg.Message.Substring(0, pos);
            text = text.Remove(0, pos + 1);

            if (instance.Groups.ContainsKey(msg.FromAgentID))
            {
                group = instance.Groups[msg.FromAgentID].Name;
                if (instance.Groups[msg.FromAgentID].InsigniaID != UUID.Zero)
                {
                    imgGroup.Init(instance, instance.Groups[msg.FromAgentID].InsigniaID, string.Empty);
                }
            }

            lblTitle.Text = title;
            lblSentBy.Text = string.Format("Sent by {0}, {1}", msg.FromAgentName, group);
            txtNotice.Text = text;

            // Fire off event
            NotificationEventArgs args = new NotificationEventArgs(instance);
            args.Text = string.Format("{0}{1}{2}{3}{4}",
                lblTitle.Text, System.Environment.NewLine,
                lblSentBy.Text, System.Environment.NewLine,
                txtNotice.Text
                );
            if (btnSave.Visible == true)
            {
                args.Buttons.Add(btnSave);
                args.Text += string.Format("{0}Attachment: {1}", System.Environment.NewLine, txtItemName.Text);
            }
            args.Buttons.Add(btnOK);
            FireNotificationCallback(args);
        }

        private void SendReply(InstantMessageDialog dialog, byte[] bucket)
        {
            client.Self.InstantMessage(client.Self.Name, msg.FromAgentID, string.Empty, msg.IMSessionID, dialog, InstantMessageOnline.Offline, client.Self.SimPosition, client.Network.CurrentSim.RegionID, bucket);
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            instance.MainForm.RemoveNotification(this);
        }

        private void btnSave_Click(object sender, System.EventArgs e)
        {
            SendReply(InstantMessageDialog.GroupNoticeInventoryAccepted, destinationFolderID.GetBytes());
            btnSave.Enabled = false;
            btnOK.Focus();
        }
    }
}
