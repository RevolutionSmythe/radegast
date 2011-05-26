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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Radegast
{
    public partial class FriendsConsole : UserControl
    {
        private RadegastInstance instance;
        private GridClient client { get { return instance.Client; } }
        private FriendInfo selectedFriend;
        private bool settingFriend = false;
        private readonly object lockOneAtaTime = new object();

        public FriendsConsole(RadegastInstance instance)
        {
            InitializeComponent();
            Disposed += new EventHandler(FriendsConsole_Disposed);

            this.instance = instance;

            if (instance.GlobalSettings["show_friends_online_notifications"].Type == OSDType.Unknown)
            {
                instance.GlobalSettings["show_friends_online_notifications"] = OSD.FromBoolean(true);
            }

            if (instance.GlobalSettings["friends_notification_highlight"].Type == OSDType.Unknown)
            {
                instance.GlobalSettings["friends_notification_highlight"] = new OSDBoolean(true);
            }

            // Callbacks
            client.Friends.FriendOffline += new EventHandler<FriendInfoEventArgs>(Friends_FriendOffline);
            client.Friends.FriendOnline += new EventHandler<FriendInfoEventArgs>(Friends_FriendOnline);
            client.Friends.FriendshipTerminated += new EventHandler<FriendshipTerminatedEventArgs>(Friends_FriendshipTerminated);
            client.Friends.FriendshipResponse += new EventHandler<FriendshipResponseEventArgs>(Friends_FriendshipResponse);
            client.Friends.FriendNames += new EventHandler<FriendNamesEventArgs>(Friends_FriendNames);
            Load += new EventHandler(FriendsConsole_Load);
            instance.Names.NameUpdated += new EventHandler<UUIDNameReplyEventArgs>(Names_NameUpdated);
        }

        void Names_NameUpdated(object sender, UUIDNameReplyEventArgs e)
        {
            bool moded = false;

            foreach (var id in e.Names.Keys)
            {
                if (client.Friends.FriendList.ContainsKey(id))
                {
                    moded = true;
                    break;
                }
            }

            if (moded)
            {
                if (InvokeRequired)
                    BeginInvoke(new MethodInvoker(() => listFriends.Invalidate()));
                else
                    listFriends.Invalidate();
            }
        }

        void FriendsConsole_Disposed(object sender, EventArgs e)
        {
            client.Friends.FriendOffline -= new EventHandler<FriendInfoEventArgs>(Friends_FriendOffline);
            client.Friends.FriendOnline -= new EventHandler<FriendInfoEventArgs>(Friends_FriendOnline);
            client.Friends.FriendshipTerminated -= new EventHandler<FriendshipTerminatedEventArgs>(Friends_FriendshipTerminated);
            client.Friends.FriendshipResponse -= new EventHandler<FriendshipResponseEventArgs>(Friends_FriendshipResponse);
            client.Friends.FriendNames -= new EventHandler<FriendNamesEventArgs>(Friends_FriendNames);
        }

        void FriendsConsole_Load(object sender, EventArgs e)
        {
            InitializeFriendsList();
            listFriends.Select();
        }

        private void InitializeFriendsList()
        {
            if (!Monitor.TryEnter(lockOneAtaTime)) return;
            List<FriendInfo> friends = client.Friends.FriendList.FindAll(delegate(FriendInfo f) { return true; });
            
            friends.Sort((fi1, fi2) =>
                {
                    if (fi1.IsOnline && !fi2.IsOnline)
                        return -1;
                    else if (!fi1.IsOnline && fi2.IsOnline)
                        return 1;
                    else
                        return string.Compare(fi1.Name, fi2.Name);
                }
            );

            listFriends.BeginUpdate();
            
            listFriends.Items.Clear();
            foreach (FriendInfo friend in friends)
            {
                listFriends.Items.Add(friend);
            }
            
            listFriends.EndUpdate();
            Monitor.Exit(lockOneAtaTime);
        }

        private void RefreshFriendsList()
        {
            InitializeFriendsList();
            SetControls();
        }

        void Friends_FriendNames(object sender, FriendNamesEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Friends_FriendNames(sender, e)));
                return;
            }

            RefreshFriendsList();
        }

        void Friends_FriendshipResponse(object sender, FriendshipResponseEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Friends_FriendshipResponse(sender, e)));
                return;
            }

            RefreshFriendsList();
        }

        void Friends_FriendOffline(object sender, FriendInfoEventArgs e)
        {
            if (!instance.GlobalSettings["show_friends_online_notifications"]) return;

            ThreadPool.QueueUserWorkItem(sync =>
            {
                string name = instance.Names.Get(e.Friend.UUID, true);
                MethodInvoker display = () =>
                {
                    instance.MainForm.TabConsole.DisplayNotificationInChat(name + " is offline", ChatBufferTextStyle.ObjectChat, instance.GlobalSettings["friends_notification_highlight"]);
                    RefreshFriendsList();
                };

                if (InvokeRequired)
                {
                    BeginInvoke(display);
                }
                else
                {
                    display();
                }
            });
        }

        void Friends_FriendOnline(object sender, FriendInfoEventArgs e)
        {
            if (!instance.GlobalSettings["show_friends_online_notifications"]) return;

            ThreadPool.QueueUserWorkItem(sync =>
            {
                string name = instance.Names.Get(e.Friend.UUID, true);
                MethodInvoker display = () =>
                {
                    instance.MainForm.TabConsole.DisplayNotificationInChat(name + " is online", ChatBufferTextStyle.ObjectChat, instance.GlobalSettings["friends_notification_highlight"]);
                    RefreshFriendsList();
                };

                if (InvokeRequired)
                {
                    BeginInvoke(display);
                }
                else
                {
                    display();
                }
            });
        }

        void Friends_FriendshipTerminated(object sender, FriendshipTerminatedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(sync =>
            {
                string name = instance.Names.Get(e.AgentID, true);
                MethodInvoker display = () =>
                {
                    instance.MainForm.TabConsole.DisplayNotificationInChat(name + " is no longer on your friend list");
                    RefreshFriendsList();
                };

                if (InvokeRequired)
                {
                    BeginInvoke(display);
                }
                else
                {
                    display();
                }
            });
        }

        private void SetControls()
        {
            if (listFriends.SelectedItems.Count == 0)
            {
                pnlActions.Enabled = pnlFriendsRights.Enabled = false;
            }
            else if (listFriends.SelectedItems.Count == 1)
            {
                pnlActions.Enabled = pnlFriendsRights.Enabled = true;
                btnProfile.Enabled = btnIM.Enabled = btnPay.Enabled = btnRemove.Enabled = true;

                FriendInfo friend = (FriendInfo)listFriends.SelectedItems[0];
                lblFriendName.Text = friend.Name + (friend.IsOnline ? " (online)" : " (offline)");

                settingFriend = true;
                chkSeeMeOnline.Checked = friend.CanSeeMeOnline;
                chkSeeMeOnMap.Checked = friend.CanSeeMeOnMap;
                chkModifyMyObjects.Checked = friend.CanModifyMyObjects;
                settingFriend = false;
            }
            else
            {
                btnIM.Enabled = pnlActions.Enabled = true;
                pnlFriendsRights.Enabled = false;
                btnProfile.Enabled = btnPay.Enabled = btnRemove.Enabled = false;
                lblFriendName.Text = "Multiple friends selected";
            }
        }

        private void btnIM_Click(object sender, EventArgs e)
        {
            if (listFriends.SelectedItems.Count == 1)
            {
                selectedFriend = (FriendInfo)listFriends.SelectedItems[0];

                if (instance.TabConsole.TabExists((client.Self.AgentID ^ selectedFriend.UUID).ToString()))
                {
                    instance.TabConsole.SelectTab((client.Self.AgentID ^ selectedFriend.UUID).ToString());
                    return;
                }

                instance.MediaManager.PlayUISound(UISounds.IMWindow);

                instance.TabConsole.AddIMTab(selectedFriend.UUID, client.Self.AgentID ^ selectedFriend.UUID, selectedFriend.Name);
                instance.TabConsole.SelectTab((client.Self.AgentID ^ selectedFriend.UUID).ToString());
            }
            else if (listFriends.SelectedItems.Count > 1)
            {
                List<UUID> participants = new List<UUID>();
                foreach (var item in listFriends.SelectedItems)
                    participants.Add(((FriendInfo)item).UUID);
                UUID tmpID = UUID.Random();
                lblFriendName.Text = "Startings friends conference...";
                instance.TabConsole.DisplayNotificationInChat(lblFriendName.Text, ChatBufferTextStyle.Invisible);
                btnIM.Enabled = false;

                ThreadPool.QueueUserWorkItem(sync =>
                    {
                        using (ManualResetEvent started = new ManualResetEvent(false))
                        {
                            UUID sessionID = UUID.Zero;
                            string sessionName = string.Empty;

                            EventHandler<GroupChatJoinedEventArgs> handler = (object isender, GroupChatJoinedEventArgs ie) =>
                                {
                                    if (ie.TmpSessionID == tmpID)
                                    {
                                        sessionID = ie.SessionID;
                                        sessionName = ie.SessionName;
                                        started.Set();
                                    }
                                };

                            client.Self.GroupChatJoined += handler;
                            client.Self.StartIMConference(participants, tmpID);
                            if (started.WaitOne(30 * 1000, false))
                            {
                                instance.TabConsole.BeginInvoke(new MethodInvoker(() =>
                                    {
                                        instance.TabConsole.AddConferenceIMTab(sessionID, sessionName);
                                        instance.TabConsole.SelectTab(sessionID.ToString());
                                    }
                                ));
                            }
                            client.Self.GroupChatJoined -= handler;
                            BeginInvoke(new MethodInvoker(() => RefreshFriendsList()));
                        }
                    }
                );
            }
        }

        private void btnProfile_Click(object sender, EventArgs e)
        {
            instance.MainForm.ShowAgentProfile(selectedFriend.Name, selectedFriend.UUID);
        }

        private void chkSeeMeOnline_CheckedChanged(object sender, EventArgs e)
        {
            if (settingFriend) return;

            selectedFriend.CanSeeMeOnline = chkSeeMeOnline.Checked;
            client.Friends.GrantRights(selectedFriend.UUID, selectedFriend.TheirFriendRights);
        }

        private void chkSeeMeOnMap_CheckedChanged(object sender, EventArgs e)
        {
            if (settingFriend) return;

            selectedFriend.CanSeeMeOnMap = chkSeeMeOnMap.Checked;
            client.Friends.GrantRights(selectedFriend.UUID, selectedFriend.TheirFriendRights);
        }

        private void chkModifyMyObjects_CheckedChanged(object sender, EventArgs e)
        {
            if (settingFriend) return;

            selectedFriend.CanModifyMyObjects = chkModifyMyObjects.Checked;
            client.Friends.GrantRights(selectedFriend.UUID, selectedFriend.TheirFriendRights);
        }

        private void btnOfferTeleport_Click(object sender, EventArgs e)
        {
            foreach (var item in listFriends.SelectedItems)
            {
                FriendInfo friend = (FriendInfo)item;
                client.Self.SendTeleportLure(friend.UUID, "Join me in " + client.Network.CurrentSim.Name + "!");
            }
        }

        private void btnPay_Click(object sender, EventArgs e)
        {
            (new frmPay(instance, selectedFriend.UUID, selectedFriend.Name, false)).ShowDialog();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            client.Friends.TerminateFriendship(selectedFriend.UUID);
            RefreshFriendsList();
        }

        public void ShowContextMenu()
        {
            RadegastContextMenuStrip menu = GetContextMenu();
            if (menu.HasSelection) menu.Show(listFriends, listFriends.PointToClient(System.Windows.Forms.Control.MousePosition));
        }

        public RadegastContextMenuStrip GetContextMenu()
        {
            RadegastContextMenuStrip friendsContextMenuStrip = new RadegastContextMenuStrip();
            if (listFriends.SelectedItems.Count == 1)
            {
                FriendInfo item = (FriendInfo)listFriends.SelectedItems[0];
                instance.ContextActionManager.AddContributions(friendsContextMenuStrip, typeof(Avatar), item, btnPay.Parent);
                friendsContextMenuStrip.Selection = item.Name;
                friendsContextMenuStrip.HasSelection = true;
            }
            else if (listFriends.SelectedItems.Count > 1)
            {
                instance.ContextActionManager.AddContributions(friendsContextMenuStrip, typeof(ListView), listFriends, btnPay.Parent);
                friendsContextMenuStrip.Selection = "Multiple friends";
                friendsContextMenuStrip.HasSelection = true;
            }
            else
            {
                friendsContextMenuStrip.Selection = null;
                friendsContextMenuStrip.HasSelection = false;
            }
            return friendsContextMenuStrip;
        }

        private void listFriends_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            try
            {
                if (e.Index >= 0)
                {
                    var item = ((ListBox)sender).Items[e.Index];
                    if (item is FriendInfo)
                    {
                        var friend = (FriendInfo)((ListBox)sender).Items[e.Index];
                        string title = instance.Names.Get(friend.UUID);

                        using (var brush = new SolidBrush(e.ForeColor))
                        {
                            e.Graphics.DrawImageUnscaled(imageList1.Images[friend.IsOnline ? 1 : 0], e.Bounds.X, e.Bounds.Y);
                            e.Graphics.DrawString(title, e.Font, brush, e.Bounds.X + 20, e.Bounds.Y + 2);
                        }
                    }
                }
            }
            catch { }

            e.DrawFocusRectangle();
        }

        private void listFriends_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedFriend = (FriendInfo)listFriends.SelectedItem;
            SetControls();
        }

        private void listFriends_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Apps || (e.Control && e.KeyCode == RadegastContextMenuStrip.ContexMenuKeyCode))
            {
                ShowContextMenu();
            }
        }

        private void listFriends_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu();
            }
        }

    }
}
