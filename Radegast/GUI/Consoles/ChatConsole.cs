// 
// Radegast Metaverse Client
// Copyright (c) 2009, Radegast Development Team
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
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Radegast.Netcom;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Radegast
{
    public partial class ChatConsole : UserControl
    {
        private RadegastInstance instance;
        private RadegastNetcom netcom { get { return instance.Netcom; } }
        private GridClient client { get { return instance.Client; } }
        private ChatTextManager chatManager;
        private TabsConsole tabConsole;
        private Avatar currentAvatar;
        private RadegastMovement movement { get { return instance.Movement; } }
        private Regex chatRegex = new Regex(@"^/(\d+)\s*(.*)", RegexOptions.Compiled);
        private List<string> chatHistory = new List<string>();
        private int chatPointer;

        public readonly Dictionary<UUID, ulong> agentSimHandle = new Dictionary<UUID, ulong>();
        public ChatInputBox ChatInputText { get { return cbxInput; } }

        public ChatConsole(RadegastInstance instance)
        {
            InitializeComponent();
            Disposed += new EventHandler(ChatConsole_Disposed);

            if (!instance.advancedDebugging)
            {
                tbtnAnim.Visible = false;
                tbtnTextures.Visible = false;

                ctxAnim.Visible = false;
                ctxTextures.Visible = false;
            }

            this.instance = instance;
            this.instance.ClientChanged += new EventHandler<ClientChangedEventArgs>(instance_ClientChanged);

            if (instance.GlobalSettings["chat_font_size"].Type != OSDType.Real)
            {
                instance.GlobalSettings["chat_font_size"] = OSD.FromReal(cbxInput.Font.Size);
            }

            instance.GlobalSettings.OnSettingChanged += new Settings.SettingChangedCallback(GlobalSettings_OnSettingChanged);

            // Callbacks
            netcom.ClientLoginStatus += new EventHandler<LoginProgressEventArgs>(netcom_ClientLoginStatus);
            netcom.ClientLoggedOut += new EventHandler(netcom_ClientLoggedOut);
            RegisterClientEvents(client);

            chatManager = new ChatTextManager(instance, new RichTextBoxPrinter(rtbChat));
            chatManager.PrintStartupMessage();

            this.instance.MainForm.Load += new EventHandler(MainForm_Load);

            lvwObjects.ListViewItemSorter = new SorterClass(instance);
            cbChatType.SelectedIndex = 1;

            UpdateFontSize();
        }

        private void RegisterClientEvents(GridClient client)
        {
            client.Grid.CoarseLocationUpdate += new EventHandler<CoarseLocationUpdateEventArgs>(Grid_CoarseLocationUpdate);
        }

        private void UnregisterClientEvents(GridClient client)
        {
            client.Grid.CoarseLocationUpdate -= new EventHandler<CoarseLocationUpdateEventArgs>(Grid_CoarseLocationUpdate);
        }

        void instance_ClientChanged(object sender, ClientChangedEventArgs e)
        {
            UnregisterClientEvents(e.OldClient);
            RegisterClientEvents(client);
        }

        void ChatConsole_Disposed(object sender, EventArgs e)
        {
            instance.ClientChanged -= new EventHandler<ClientChangedEventArgs>(instance_ClientChanged);
            netcom.ClientLoginStatus -= new EventHandler<LoginProgressEventArgs>(netcom_ClientLoginStatus);
            netcom.ClientLoggedOut -= new EventHandler(netcom_ClientLoggedOut);
            UnregisterClientEvents(client);
            chatManager.Dispose();
            chatManager = null;
        }

        static public Font ChangeFontSize(Font font, float fontSize)
        {
            if (font != null)
            {
                float currentSize = font.Size;
                if (currentSize != fontSize)
                {
                    font = new Font(font.Name, fontSize,
                        font.Style, font.Unit,
                        font.GdiCharSet, font.GdiVerticalFont);
                }
            }
            return font;
        }

        void UpdateFontSize()
        {
            float size = (float)instance.GlobalSettings["chat_font_size"].AsReal();
            cbxInput.Font = ChatConsole.ChangeFontSize(cbxInput.Font, size);
            rtbChat.Font = ChatConsole.ChangeFontSize(rtbChat.Font, size);
            chatManager.ReprintAllText();
        }

        void GlobalSettings_OnSettingChanged(object sender, SettingsEventArgs e)
        {
            if (e.Key == "chat_font_size")
                UpdateFontSize();
        }

        public List<UUID> GetAvatarList()
        {
            lock (agentSimHandle)
            {
                List<UUID> ret = new List<UUID>();
                foreach (ListViewItem item in lvwObjects.Items)
                {
                    if (item.Tag is UUID)
                        ret.Add((UUID)item.Tag);
                }
                return ret;
            }
        }

        void Grid_CoarseLocationUpdate(object sender, CoarseLocationUpdateEventArgs e)
        {
            if (client.Network.CurrentSim == null /*|| client.Network.CurrentSim.Handle != sim.Handle*/)
            {
                return;
            }

            if (InvokeRequired)
            {
                if (IsHandleCreated)
                    BeginInvoke(new MethodInvoker(() => Grid_CoarseLocationUpdate(sender, e)));
                return;
            }

            // later on we can set this with something from the GUI
            const double MAX_DISTANCE = 362.0; // one sim a corner to corner distance
            lock (agentSimHandle)
                try
                {
                    lvwObjects.BeginUpdate();
                    Vector3d mypos = e.Simulator.AvatarPositions.ContainsKey(client.Self.AgentID)
                                        ? ToVector3D(e.Simulator, e.Simulator.AvatarPositions[client.Self.AgentID])
                                        : client.Self.GlobalPosition;

                    // CoarseLocationUpdate gives us hight of 0 when actual height is
                    // between 1024-4096m.
                    if (mypos.Z < 0.1)
                    {
                        mypos.Z = client.Self.GlobalPosition.Z;
                    }

                    List<UUID> existing = new List<UUID>();
                    List<UUID> removed = new List<UUID>(e.RemovedEntries);

                    e.Simulator.AvatarPositions.ForEach(delegate(KeyValuePair<UUID, Vector3> avi)
                    {
                        existing.Add(avi.Key);
                        if (!lvwObjects.Items.ContainsKey(avi.Key.ToString()))
                        {
                            string name = instance.getAvatarName(avi.Key);
                            ListViewItem item = lvwObjects.Items.Add(avi.Key.ToString(), name, string.Empty);
                            if (avi.Key == client.Self.AgentID)
                            {
                                // Stops our name saying "Loading..."
                                item.Text = client.Self.Name;
                                item.Font = new Font(item.Font, FontStyle.Bold);
                            }
                            item.Tag = avi.Key;
                            agentSimHandle[avi.Key] = e.Simulator.Handle;
                        }
                    });

                    foreach (ListViewItem item in lvwObjects.Items)
                    {
                        if (item == null) continue;
                        UUID key = (UUID)item.Tag;

                        if (agentSimHandle[key] != e.Simulator.Handle)
                        {
                            // not for this sim
                            continue;
                        }

                        if (key == client.Self.AgentID)
                        {
                            continue;
                        }

                        //the AvatarPostions is checked once more because it changes wildly on its own
                        //even though the !existing should have been adequate
                        Vector3 pos;
                        if (!existing.Contains(key) || !e.Simulator.AvatarPositions.TryGetValue(key, out pos))
                        {
                            // not here anymore
                            removed.Add(key);
                            continue;
                        }

                        Avatar foundAvi = e.Simulator.ObjectsAvatars.Find((Avatar av) => { return av.ID == key; });

                        // CoarseLocationUpdate gives us hight of 0 when actual height is
                        // between 1024-4096m.
                        if (pos.Z < 0.1)
                        {
                            if (foundAvi != null)
                            {
                                if (foundAvi.ParentID == 0)
                                {
                                    pos.Z = foundAvi.Position.Z;
                                }
                                else
                                {
                                    if (e.Simulator.ObjectsPrimitives.ContainsKey(foundAvi.ParentID))
                                    {
                                        pos.Z = e.Simulator.ObjectsPrimitives[foundAvi.ParentID].Position.Z;
                                    }
                                }
                            }
                        }

                        int d = (int)Vector3d.Distance(ToVector3D(e.Simulator, pos), mypos);

                        if (e.Simulator != client.Network.CurrentSim && d > MAX_DISTANCE)
                        {
                            removed.Add(key);
                            continue;
                        }

                        if (pos.Z < 0.1)
                        {
                            item.Text = instance.getAvatarName(key) + " (?m)";
                        }
                        else
                        {
                            item.Text = instance.getAvatarName(key) + " (" + d + "m)";
                        }

                        if (foundAvi != null)
                        {
                            item.Text += "*";
                        }
                    }

                    foreach (UUID key in removed)
                    {
                        lvwObjects.Items.RemoveByKey(key.ToString());
                    }

                    lvwObjects.Sort();
                }
                catch (Exception ex)
                {
                    Logger.Log("Grid_OnCoarseLocationUpdate: " + ex, OpenMetaverse.Helpers.LogLevel.Error, client);
                }
                finally
                {
                    lvwObjects.EndUpdate();
                }
        }

        static Vector3d ToVector3D(Simulator simulator, Vector3 pos)
        {
            if (simulator != null)
            {
                uint globalX, globalY;
                Utils.LongToUInts(simulator.Handle, out globalX, out globalY);

                return new Vector3d(
                    (double)globalX + (double)pos.X,
                    (double)globalY + (double)pos.Y,
                    (double)pos.Z);
            }
            else
                return Vector3d.Zero;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            tabConsole = instance.TabConsole;
        }

        private void netcom_ClientLoginStatus(object sender, LoginProgressEventArgs e)
        {
            if (e.Status != LoginStatus.Success) return;

            cbxInput.Enabled = true;
            client.Avatars.RequestAvatarProperties(client.Self.AgentID);
            cbxInput.Focus();
        }

        private void netcom_ClientLoggedOut(object sender, EventArgs e)
        {
            cbxInput.Enabled = false;
            btnSay.Enabled = false;
            cbChatType.Enabled = false;

            lvwObjects.Items.Clear();
        }

        void ChatHistoryPrev()
        {
            if (chatPointer == 0) return;
            chatPointer--;
            if (chatHistory.Count > chatPointer)
            {
                cbxInput.Text = chatHistory[chatPointer];
                cbxInput.SelectionStart = cbxInput.Text.Length;
                cbxInput.SelectionLength = 0;
            }
        }

        void ChatHistoryNext()
        {
            if (chatPointer == chatHistory.Count) return;
            chatPointer++;
            if (chatPointer == chatHistory.Count)
            {
                cbxInput.Text = string.Empty;
                return;
            }
            cbxInput.Text = chatHistory[chatPointer];
            cbxInput.SelectionStart = cbxInput.Text.Length;
            cbxInput.SelectionLength = 0;
        }

        private void cbxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up && e.Modifiers == Keys.Control)
            {
                e.Handled = e.SuppressKeyPress = true;
                ChatHistoryPrev();
                return;
            }

            if (e.KeyCode == Keys.Down && e.Modifiers == Keys.Control)
            {
                e.Handled = e.SuppressKeyPress = true;
                ChatHistoryNext();
                return;
            }

            if (e.KeyCode != Keys.Enter) return;
            e.Handled = e.SuppressKeyPress = true;

            if (e.Shift)
                ProcessChatInput(cbxInput.Text, ChatType.Whisper);
            else if (e.Control)
                ProcessChatInput(cbxInput.Text, ChatType.Shout);
            else
                ProcessChatInput(cbxInput.Text, ChatType.Normal);
        }

        private void ProcessChatInput(string input, ChatType type)
        {
            if (string.IsNullOrEmpty(input)) return;
            chatHistory.Add(input);
            chatPointer = chatHistory.Count;
            ClearChatInput();

            string msg;

            if (input.Length >= 1000)
            {
                msg = input.Substring(0, 1000);
            }
            else
            {
                msg = input;
            }

            msg = msg.Replace(ChatInputBox.NewlineMarker, Environment.NewLine);

            if (instance.GlobalSettings["mu_emotes"].AsBoolean() && msg.StartsWith(":"))
            {
                msg = "/me " + msg.Substring(1);
            }

            int ch = 0;
            Match m = chatRegex.Match(msg);

            if (m.Groups.Count > 2)
            {
                ch = int.Parse(m.Groups[1].Value);
                msg = m.Groups[2].Value;
            }

            if (instance.CommandsManager.IsValidCommand(msg))
            {
                instance.CommandsManager.ExecuteCommand(msg);
            }
            else
            {
                #region RLV
                if (instance.RLV.Enabled && ch != 0)
                {
                    if (instance.RLV.RestictionActive("sendchannel", ch.ToString()))
                        return;
                }

                if (instance.RLV.Enabled && ch == 0)
                {
                    // emote
                    if (msg.StartsWith("/me"))
                    {
                        var opt = instance.RLV.GetOptions("rediremote");
                        if (opt.Count > 0)
                        {
                            foreach (var rchanstr in opt)
                            {
                                int rchat = 0;
                                if (int.TryParse(rchanstr, out rchat) && rchat > 0)
                                {
                                    client.Self.Chat(msg, rchat, type);
                                }
                            }
                            return;
                        }
                    }
                    else if (!msg.StartsWith("/"))
                    {
                        var opt = instance.RLV.GetOptions("redirchat");

                        if (opt.Count > 0)
                        {
                            foreach (var rchanstr in opt)
                            {
                                int rchat = 0;
                                if (int.TryParse(rchanstr, out rchat) && rchat > 0)
                                {
                                    client.Self.Chat(msg, rchat, type);
                                }
                            }
                            return;
                        }

                        if (instance.RLV.RestictionActive("sendchat"))
                        {
                            msg = "...";
                        }

                        if (type == ChatType.Whisper && instance.RLV.RestictionActive("chatwhisper"))
                            type = ChatType.Normal;

                        if (type == ChatType.Shout && instance.RLV.RestictionActive("chatshout"))
                            type = ChatType.Normal;

                        if (instance.RLV.RestictionActive("chatnormal"))
                            type = ChatType.Whisper;

                    }
                }
                #endregion

                netcom.ChatOut(msg, type, ch);
            }

        }

        private void ClearChatInput()
        {
            cbxInput.Text = string.Empty;
        }

        private void btnSay_Click(object sender, EventArgs e)
        {
            ProcessChatInput(cbxInput.Text, (ChatType)cbChatType.SelectedIndex);
            cbxInput.Focus();
        }

        private void cbxInput_TextChanged(object sender, EventArgs e)
        {
            if (cbxInput.Text.Length > 0)
            {
                btnSay.Enabled = cbChatType.Enabled = true;

                if (!cbxInput.Text.StartsWith("/"))
                {
                    if (!instance.State.IsTyping)
                        instance.State.SetTyping(true);
                }
            }
            else
            {
                btnSay.Enabled = cbChatType.Enabled = false;
                instance.State.SetTyping(false);
            }
        }

        public ChatTextManager ChatManager
        {
            get { return chatManager; }
        }

        private void tbtnStartIM_Click(object sender, EventArgs e)
        {
            if (lvwObjects.SelectedItems.Count == 0) return;
            UUID av = (UUID)lvwObjects.SelectedItems[0].Tag;
            string name = instance.getAvatarName(av);

            if (tabConsole.TabExists((client.Self.AgentID ^ av).ToString()))
            {
                tabConsole.SelectTab((client.Self.AgentID ^ av).ToString());
                return;
            }

            tabConsole.AddIMTab(av, client.Self.AgentID ^ av, name);
            tabConsole.SelectTab((client.Self.AgentID ^ av).ToString());
        }

        private void tbtnFollow_Click(object sender, EventArgs e)
        {
            Avatar av = currentAvatar;
            if (av == null) return;

            if (instance.State.FollowName == string.Empty)
            {
                instance.State.Follow(av.Name, av.ID);
                ctxFollow.Text = "Unfollow " + av.Name;
            }
            else
            {
                instance.State.Follow(string.Empty, UUID.Zero);
                ctxFollow.Text = "Follow";
            }
        }

        private void ctxPoint_Click(object sender, EventArgs e)
        {
            if (!instance.State.IsPointing)
            {
                Avatar av = currentAvatar;
                if (av == null) return;
                instance.State.SetPointing(av, 5);
                ctxPoint.Text = "Unpoint";
            }
            else
            {
                ctxPoint.Text = "Point at";
                instance.State.UnSetPointing();
            }
        }


        private void lvwObjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvwObjects.SelectedItems.Count == 0)
            {
                currentAvatar = null;
                tbtnStartIM.Enabled = tbtnFollow.Enabled = tbtnProfile.Enabled = tbtnTextures.Enabled = tbtnMaster.Enabled = tbtnAttach.Enabled = tbtnAnim.Enabled = false;
                ctxPay.Enabled = ctxSource.Enabled = ctxPoint.Enabled = ctxStartIM.Enabled = ctxFollow.Enabled = ctxProfile.Enabled = ctxTextures.Enabled = ctxMaster.Enabled = ctxAttach.Enabled = ctxAnim.Enabled = false;
            }
            else
            {
                currentAvatar = client.Network.CurrentSim.ObjectsAvatars.Find(delegate(Avatar a)
                {
                    return a.ID == (UUID)lvwObjects.SelectedItems[0].Tag;
                });

                tbtnStartIM.Enabled = tbtnProfile.Enabled = true;
                tbtnFollow.Enabled = tbtnTextures.Enabled = tbtnMaster.Enabled = tbtnAttach.Enabled = tbtnAnim.Enabled = currentAvatar != null;

                ctxPay.Enabled = ctxSource.Enabled = ctxStartIM.Enabled = ctxProfile.Enabled = true;
                ctxPoint.Enabled = ctxFollow.Enabled = ctxTextures.Enabled = ctxMaster.Enabled = ctxAttach.Enabled = ctxAnim.Enabled = currentAvatar != null;

                if ((UUID)lvwObjects.SelectedItems[0].Tag == client.Self.AgentID)
                {
                    tbtnFollow.Enabled = tbtnStartIM.Enabled = false;
                    ctxPay.Enabled = ctxFollow.Enabled = ctxStartIM.Enabled = false;
                }
            }
            if (instance.State.IsPointing)
            {
                ctxPoint.Enabled = true;
            }
        }

        private void rtbChat_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            instance.MainForm.ProcessLink(e.LinkText);
        }

        private void tbtnProfile_Click(object sender, EventArgs e)
        {
            if (lvwObjects.SelectedItems.Count == 0) return;
            UUID av = (UUID)lvwObjects.SelectedItems[0].Tag;
            string name = instance.getAvatarName(av);

            instance.MainForm.ShowAgentProfile(name, av);
        }

        private void dumpOufitBtn_Click(object sender, EventArgs e)
        {
            Avatar av = currentAvatar;
            if (av == null) return;

            if (!instance.TabConsole.TabExists("OT: " + av.Name))
            {
                instance.TabConsole.AddOTTab(av);
            }
            instance.TabConsole.SelectTab("OT: " + av.Name);
        }

        private void tbtnMaster_Click(object sender, EventArgs e)
        {
            Avatar av = currentAvatar;
            if (av == null) return;

            if (!instance.TabConsole.TabExists("MS: " + av.Name))
            {
                instance.TabConsole.AddMSTab(av);
            }
            instance.TabConsole.SelectTab("MS: " + av.Name);
        }

        private void tbtnAttach_Click(object sender, EventArgs e)
        {
            Avatar av = currentAvatar;
            if (av == null) return;

            if (!instance.TabConsole.TabExists("AT: " + av.Name))
            {
                instance.TabConsole.AddATTab(av);
            }
            instance.TabConsole.SelectTab("AT: " + av.Name);
        }

        private void tbtnAnim_Click(object sender, EventArgs e)
        {
            Avatar av = currentAvatar;
            if (av == null) return;

            if (!instance.TabConsole.TabExists("Anim: " + av.Name))
            {
                instance.TabConsole.AddAnimTab(av);
            }
            instance.TabConsole.SelectTab("Anim: " + av.Name);


        }

        private void btnTurnLeft_MouseDown(object sender, MouseEventArgs e)
        {
            movement.TurningLeft = true;
        }

        private void btnTurnLeft_MouseUp(object sender, MouseEventArgs e)
        {
            movement.TurningLeft = false;
        }

        private void btnTurnRight_MouseDown(object sender, MouseEventArgs e)
        {
            movement.TurningRight = true;
        }

        private void btnTurnRight_MouseUp(object sender, MouseEventArgs e)
        {
            movement.TurningRight = false;
        }

        private void btnFwd_MouseDown(object sender, MouseEventArgs e)
        {
            movement.MovingForward = true;
        }

        private void btnFwd_MouseUp(object sender, MouseEventArgs e)
        {
            movement.MovingForward = false;
        }

        private void btnMoveBack_MouseDown(object sender, MouseEventArgs e)
        {
            movement.MovingBackward = true;
        }

        private void btnMoveBack_MouseUp(object sender, MouseEventArgs e)
        {
            movement.MovingBackward = false;
        }

        private void pnlMovement_Click(object sender, EventArgs e)
        {
            client.Self.Jump(true);
            System.Threading.Thread.Sleep(500);
            client.Self.Jump(false);
        }

        private void lvwObjects_DragDrop(object sender, DragEventArgs e)
        {
            Point local = lvwObjects.PointToClient(new Point(e.X, e.Y));
            ListViewItem litem = lvwObjects.GetItemAt(local.X, local.Y);
            if (litem == null) return;
            TreeNode node = e.Data.GetData(typeof(TreeNode)) as TreeNode;
            if (node == null) return;

            if (node.Tag is InventoryItem)
            {
                InventoryItem item = node.Tag as InventoryItem;
                client.Inventory.GiveItem(item.UUID, item.Name, item.AssetType, (UUID)litem.Tag, true);
                instance.TabConsole.DisplayNotificationInChat("Offered item " + item.Name + " to " + instance.getAvatarName((UUID)litem.Tag) + ".");
            }
            else if (node.Tag is InventoryFolder)
            {
                InventoryFolder folder = node.Tag as InventoryFolder;
                client.Inventory.GiveFolder(folder.UUID, folder.Name, AssetType.Folder, (UUID)litem.Tag, true);
                instance.TabConsole.DisplayNotificationInChat("Offered folder " + folder.Name + " to " + instance.getAvatarName((UUID)litem.Tag) + ".");
            }
        }

        private void lvwObjects_DragOver(object sender, DragEventArgs e)
        {
            Point local = lvwObjects.PointToClient(new Point(e.X, e.Y));
            ListViewItem litem = lvwObjects.GetItemAt(local.X, local.Y);
            if (litem == null) return;

            if (!e.Data.GetDataPresent(typeof(TreeNode))) return;

            e.Effect = DragDropEffects.Copy;
        }

        private void avatarContext_Opening(object sender, CancelEventArgs e)
        {
            if (lvwObjects.SelectedItems.Count == 0 && !instance.State.IsPointing)
            {
                e.Cancel = true;
                return;
            }
            else if (instance.State.IsPointing)
            {
                ctxPoint.Enabled = true;
                ctxPoint.Text = "Unpoint";
            }
            instance.ContextActionManager.AddContributions(
                avatarContext, typeof(Avatar), lvwObjects.SelectedItems[0]);
        }

        private void ctxSource_Click(object sender, EventArgs e)
        {
            if (lvwObjects.SelectedItems.Count != 1) return;

            instance.State.EffectSource = (UUID)lvwObjects.SelectedItems[0].Tag;
        }

        private void ctxPay_Click(object sender, EventArgs e)
        {
            if (lvwObjects.SelectedItems.Count != 1) return;
            (new frmPay(instance, (UUID)lvwObjects.SelectedItems[0].Tag, instance.getAvatarName((UUID)lvwObjects.SelectedItems[0].Tag), false)).ShowDialog();
        }

        private void ChatConsole_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
                cbxInput.Focus();
        }

        private void rtbChat_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            RadegastContextMenuStrip cms = new RadegastContextMenuStrip();
            instance.ContextActionManager.AddContributions(cms, instance.Client);
            cms.Show((Control)sender, new Point(e.X, e.Y));
        }

        private void lvwObjects_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem item = lvwObjects.GetItemAt(e.X, e.Y);
            if (item != null)
            {
                try
                {
                    UUID agentID = new UUID(item.Tag.ToString());
                    instance.MainForm.ShowAgentProfile(instance.getAvatarName(agentID), agentID);
                }
                catch (Exception) { }
            }
        }

        private void cbxInput_SizeChanged(object sender, EventArgs e)
        {
            pnlChatInput.Height = cbxInput.Height + 3;
        }

        private void splitContainer1_Panel1_SizeChanged(object sender, EventArgs e)
        {
            rtbChat.Size = splitContainer1.Panel1.ClientSize;
        }

        private void ctxOfferTP_Click(object sender, EventArgs e)
        {
            if (lvwObjects.SelectedItems.Count != 1) return;
            UUID av = (UUID)lvwObjects.SelectedItems[0].Tag;
            client.Self.SendTeleportLure(av, "Join me in " + client.Network.CurrentSim.Name + "!");
        }

        private void ctxTeleportTo_Click(object sender, EventArgs e)
        {
            if (lvwObjects.SelectedItems.Count != 1) return;
            UUID person = (UUID)lvwObjects.SelectedItems[0].Tag;
            string pname = instance.getAvatarName(person);
            Simulator sim = null;
            Vector3 pos;

            if (instance.State.TryFindAvatar(person, out sim, out pos))
            {
                tabConsole.DisplayNotificationInChat(string.Format("Teleporting to {0}", pname));
                client.Self.RequestTeleport(sim.Handle, pos);
            }
            else
            {
                tabConsole.DisplayNotificationInChat(string.Format("Could not locate {0}", pname));
            }
        }
    }

    public class SorterClass : System.Collections.IComparer
    {
        private static Regex distanceRegex = new Regex(@"\((?<dist>\d+)\s*m\)", RegexOptions.Compiled);
        private Match match;
        RadegastInstance instance;

        public SorterClass(RadegastInstance instance)
        {
            this.instance = instance;
        }

        //this routine should return -1 if xy and 0 if x==y.
        // for our sample we'll just use string comparison
        public int Compare(object x, object y)
        {

            System.Windows.Forms.ListViewItem item1 = (System.Windows.Forms.ListViewItem)x;
            System.Windows.Forms.ListViewItem item2 = (System.Windows.Forms.ListViewItem)y;

            if (item1.Text == instance.Client.Self.Name)
                return -1;

            if (item2.Text == instance.Client.Self.Name)
                return 1;

            int distance1 = int.MaxValue, distance2 = int.MaxValue;

            if ((match = distanceRegex.Match(item1.Text)).Success)
                distance1 = int.Parse(match.Groups["dist"].Value);

            if ((match = distanceRegex.Match(item2.Text)).Success)
                distance2 = int.Parse(match.Groups["dist"].Value);

            if (distance1 < distance2)
                return -1;
            else if (distance1 > distance2)
                return 1;
            else
                return string.Compare(item1.Text, item2.Text);

        }
    }

}
