﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Radegast;
using System.Text.RegularExpressions;
using System.Net;
using System.Windows.Forms;
using System.Threading;

namespace RadegastSpeech.Conversation
{
    /// <summary>
    /// Manages all conversations
    /// </summary>
    internal class Control : AreaControl
    {
        /// <summary>
        /// Conversations correspond to tabbed panels on the main window.
        /// </summary>
        private Dictionary<string, Mode> conversations;
        /// <summary>
        /// Interruptions are short-lived conversations about dialog boxes, etc
        /// </summary>
        private LinkedList<Mode> interruptions;

        // The permanent conversations.
        private Conversation.Chat chat;
        private Conversation.Closet inventory;
        private Conversation.Friends friends;
        private Conversation.Voice voice;
        private Conversation.Surroundings surroundings;
        private Mode currentMode;
        private Mode interrupted;
        internal string LoginName;
        private bool firstTime = true;
        private const string CONVGRAMMAR = "conv";

        internal Control(PluginControl pc)
            : base(pc)
        {
            // Initialize the index to conversations and the list of pending interruptions.
            interruptions = new LinkedList<Mode>();
            conversations = new Dictionary<string,Mode>();
        }

        internal override void Start()
        {
            if (firstTime)
            {
                firstTime = false;

                control.listener.CreateGrammar(
                    CONVGRAMMAR,
                    new string[] { "talk to Max",
                    "skip",
                    "who is online",
                    "open the closet",
                    "friends",
                    "talk" });
            }

            // Automatically handle notifications (blue dialogs)
            Notification.OnNotificationDisplayed +=
                new Notification.NotificationCallback(OnNotificationDisplayed);
//            Notification.OnNotificationClosed +=
//                new Notification.NotificationCallback(OnNotificationClosed);

            // Announce connect and disconnect.
            control.instance.Netcom.ClientConnected +=
                new EventHandler<EventArgs>(Network_ClientConnected);
            control.instance.Netcom.ClientDisconnected +=
                new EventHandler<DisconnectedEventArgs>(Network_Disconnected);

            control.instance.Netcom.ClientLoginStatus +=
                new EventHandler<LoginProgressEventArgs>(netcom_ClientLoginStatus);

            // Notice arrival in a new sim
            control.instance.Client.Network.SimChanged +=
                new EventHandler<SimChangedEventArgs>(Network_SimChanged);

            control.instance.Netcom.ClientLoggingIn +=
                new EventHandler<Radegast.Netcom.OverrideEventArgs>(Netcom_ClientLoggingIn);
            // Watch the coming and going of main window tabs.
            control.instance.TabConsole.OnTabAdded +=
                new TabsConsole.TabCallback(TabConsole_OnTabAdded);
            control.instance.TabConsole.OnTabRemoved +=
                new TabsConsole.TabCallback(TabConsole_OnTabRemoved);

            // Notice when the active tab changes on the graphics user interface.
            control.instance.TabConsole.OnTabSelected +=
                new TabsConsole.TabCallback(OnTabChange);

            // Handle Instant Messages too
            control.instance.Client.Self.IM +=
                new EventHandler<InstantMessageEventArgs>(OnInstantMessage);

            // Outgoing IMs
            control.instance.Netcom.InstantMessageSent += new EventHandler<Radegast.Netcom.InstantMessageSentEventArgs>(Netcom_InstantMessageSent);

            // Watch for global keys
            control.instance.MainForm.KeyUp += new KeyEventHandler(MainForm_KeyUp);

            // System messages in chat window
            control.instance.TabConsole.OnChatNotification += new TabsConsole.ChatNotificationCallback(TabConsole_OnChatNotification);

            control.listener.ActivateGrammar(CONVGRAMMAR);

        }

        /// <summary>
        /// Say various notifications that come in the chat
        /// </summary>
        /// <param name="sender">Message sender</param>
        /// <param name="e">Event args</param>
        void TabConsole_OnChatNotification(object sender, ChatNotificationEventArgs e)
        {
            Talker.Say(e.Message);
        }

        /// <summary>
        /// Watch for global function keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            // Escape clears the speak-ahead queue.
            if (e.KeyCode == Keys.Escape)
            {
                Talker.Flush();
                Talker.SayMore("Flushed.");
                e.Handled = true;
            }
        }


        void Netcom_ClientLoggingIn(object sender, Radegast.Netcom.OverrideEventArgs e)
        {
            Talker.SayMore("Logging in.  Please wait.");
        }

        private void netcom_ClientLoginStatus(
            object sender,
            LoginProgressEventArgs e)
        {
            switch (e.Status)
            {
                case LoginStatus.ConnectingToLogin:
                    // Never seems to happen.  See Netcom_ClientLoggingIn
                    Talker.SayMore("Connecting to login server");
                    return;

                case LoginStatus.ConnectingToSim:
                    Talker.SayMore("Connecting to region");
                    return;

               case LoginStatus.Success:
                    LoginName = control.instance.Netcom.LoginOptions.FullName;
                    //Talker.SayMore("Logged in as " + LoginName);
                    //if (friends != null)
                    //    friends.Announce = true;
                    return;

                case LoginStatus.Failed:
                    Talker.Say(e.Message +
                        ". Press Enter twice to retry", Talk.BeepType.Bad);
                    return;

                default:
                    return;
            }
        }

        /// <summary>
        /// Switch active conversation as tab focus moves.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnTabChange(object sender, TabEventArgs e)
        {
            ActivateConversationFromTab(e.Tab);
        }

        public void ActivateConversationFromTab(RadegastTab Tab)
        {
            System.Windows.Forms.Control sTabControl = Tab.Control;

            if (sTabControl is InventoryConsole)
                SelectConversation(inventory);
            else if (sTabControl is ChatConsole)
            {
                if (chat == null)
                {
                    chat = new Chat(control);
                    chat.Console = sTabControl;
                    AddConversation(chat);
                }
                SelectConversation(chat);
            }
            else if (sTabControl is FriendsConsole)
                SelectConversation(friends);
            else if (sTabControl is VoiceConsole)
                SelectConversation(voice);
            else if (sTabControl is GroupIMTabWindow)
            {
                GroupIMTabWindow tab = (GroupIMTabWindow)sTabControl;
                SelectConversation(
                    control.instance.Groups[tab.SessionId].Name);
            }
            else if (sTabControl is ConferenceIMTabWindow)
            {
                ConferenceIMTabWindow tab = (ConferenceIMTabWindow)sTabControl;
                SelectConversation(tab.SessionName);
            }
            else if (sTabControl is IMTabWindow)
            {
                IMTabWindow tab = (IMTabWindow)sTabControl;
                SelectConversation(tab.TargetName);
            }
            else if (sTabControl is ObjectsConsole)
            {
                SelectConversation(surroundings);
            }

        }

        /// <summary>
        /// Create conversations as tabs are created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TabConsole_OnTabAdded(object sender, TabEventArgs e)
        {
            CreateConversationFromTab(e.Tab, true);
        }

        public void CreateConversationFromTab(RadegastTab Tab, bool selectConversation)
        {
            System.Windows.Forms.Control sTabControl = Tab.Control;

            Mode newConv = null;

            // Create a conversation on first appearance of its tab.
            if (sTabControl is InventoryConsole)
                newConv = inventory = new Closet(control);
            else if (sTabControl is ChatConsole)
            {
                if (chat != null) return;
                newConv = chat = new Chat(control);
            }
            else if (sTabControl is FriendsConsole)
                newConv = friends = new Friends(control);
            else if (sTabControl is VoiceConsole)
                newConv = voice = new Voice(control);
            else if (sTabControl is GroupIMTabWindow)
            {
                GroupIMTabWindow tab = (GroupIMTabWindow)sTabControl;
                AddConversation(new GroupIMSession(control, tab.SessionId));
                return;
            }
            else if (sTabControl is ConferenceIMTabWindow)
            {
                ConferenceIMTabWindow tab = (ConferenceIMTabWindow)sTabControl;
                AddConversation(new ConferenceIMSession(control, tab.SessionId, tab.SessionName));
                return;
            }
            else if (sTabControl is IMTabWindow)
            {
                IMTabWindow tab = (IMTabWindow)sTabControl;
                AddConversation(new SingleIMSession(control, tab.TargetName, tab.TargetId, tab.SessionId));
                return;
            }
            else if (sTabControl is ObjectsConsole)
            {
                surroundings = new Surroundings(control);
                AddConversation(surroundings);
            }

            // If a conversation was created, switch to it.
            if (newConv != null)
            {
                AddConversation(newConv);
                // Select CHAT as soon as it is created.
                if (selectConversation && sTabControl is ChatConsole)
                    SelectConversation(newConv);
            }
        }

        /// <summary>
        /// Quietly close conversations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TabConsole_OnTabRemoved(object sender, TabEventArgs e)
        {
            System.Windows.Forms.Control sTabControl = e.Tab.Control;
            if (sTabControl is InventoryConsole)
                RemoveConversation(inventory.Title);
            else if (sTabControl is ChatConsole)
                RemoveConversation(chat.Title);
            else if (sTabControl is FriendsConsole)
                RemoveConversation(friends.Title);
            else if (sTabControl is VoiceConsole)
                RemoveConversation(voice.Title);
            else if (sTabControl is ConferenceIMTabWindow)
                RemoveConversation(((ConferenceIMTabWindow)e.Tab.Control).SessionName);
            else if (sTabControl is GroupIMTabWindow ||
                     sTabControl is IMTabWindow)
                RemoveConversation(sTabControl.Name);  // TODO wrong name
        }


        internal override void Shutdown()
        {
            if (chat != null)
            {
                chat.Dispose();
                chat = null;
            }

            // Automatically handle notifications (blue dialogs)
            Notification.OnNotificationDisplayed -=
                new Notification.NotificationCallback(OnNotificationDisplayed);

            // Announce connect and disconnect.
            control.instance.Netcom.ClientConnected -=
                new EventHandler<EventArgs>(Network_ClientConnected);
            control.instance.Netcom.ClientDisconnected -=
                new EventHandler<DisconnectedEventArgs>(Network_Disconnected);

            control.instance.Netcom.ClientLoginStatus -=
                new EventHandler<LoginProgressEventArgs>(netcom_ClientLoginStatus);

            // Notice arrival in a new sim
            control.instance.Client.Network.SimChanged -=
                new EventHandler<SimChangedEventArgs>(Network_SimChanged);

            control.instance.Netcom.ClientLoggingIn -=
                new EventHandler<Radegast.Netcom.OverrideEventArgs>(Netcom_ClientLoggingIn);
            // Watch the coming and going of main window tabs.
            control.instance.TabConsole.OnTabAdded -=
                new TabsConsole.TabCallback(TabConsole_OnTabAdded);
            control.instance.TabConsole.OnTabRemoved -=
                new TabsConsole.TabCallback(TabConsole_OnTabRemoved);

            // Notice when the active tab changes on the graphics user interface.
            control.instance.TabConsole.OnTabSelected -=
                new TabsConsole.TabCallback(OnTabChange);

            // Handle Instant Messages too
            control.instance.Client.Self.IM -=
                new EventHandler<InstantMessageEventArgs>(OnInstantMessage);

            // Outgoing IMs
            control.instance.Netcom.InstantMessageSent -= new EventHandler<Radegast.Netcom.InstantMessageSentEventArgs>(Netcom_InstantMessageSent);

            // System notifications in chat
            control.instance.TabConsole.OnChatNotification -= new TabsConsole.ChatNotificationCallback(TabConsole_OnChatNotification);

            control.listener.DeactivateGrammar(CONVGRAMMAR);

            foreach (Mode m in conversations.Values)
            {
                m.Stop();
            }
            foreach (Mode m in interruptions)
            {
                m.Stop();
            }
            conversations.Clear();
            interruptions.Clear();
        }

        void WatchKeys()
        {
        }

        internal bool amCurrent(Mode m)
        {
            return (currentMode == m);
        }

        /// <summary>
        /// Start an interrupting conversation.
        /// </summary>
        void StartInterruption()
        {
            // Remember what we were talking about.
            if (interrupted == null)
                interrupted = currentMode;

            // Visually they stack up, so we take the last one first.
            currentMode = interruptions.Last();
            currentMode.Start();
        }

        /// <summary>
        /// Finish an interruption and resume normal conversation
        /// </summary>
        internal void FinishInterruption( Mode m )
        {
            lock (interruptions)
            {
                // Remove the terminating interruption from the list.
                interruptions.Remove(m);

                // Let it remove any event hooks, etc
                m.Stop();

                // If there are any other interruptions pending, start one.
                // Otherwise resume the interrupted conversation.
                if (interruptions.Count > 0)
                    StartInterruption();
                else
                {
                    currentMode = interrupted;
                    interrupted = null;
                    currentMode.Start();
                }
            }
        }
 
        private void Network_ClientConnected(object sender, EventArgs e)
        {
            Talker.Say("You are connected.", Talk.BeepType.Good);
            if (chat == null)
            {
                chat = new Chat(control);

                AddConversation(chat);
                SelectConversation(chat);
            }
        }

        void Network_SimChanged(object sender, SimChangedEventArgs e)
        {
            Talker.Say("You are now in " +
                control.instance.Client.Network.CurrentSim.Name,
                Talk.BeepType.Good);
        }


        /// <summary>
        /// Announce reason for disconnect.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="message"></param>
        private void Network_Disconnected(object sender, DisconnectedEventArgs e)
        {
            switch (e.Reason)
            {
                case NetworkManager.DisconnectType.ClientInitiated:
                    Talker.Say("You are disconnected.");
                    break;
                case NetworkManager.DisconnectType.SimShutdown:
                    Talker.Say("The region you were in has been shut down.  You are disconnected.",
                        Talk.BeepType.Bad);
                    break;
                case NetworkManager.DisconnectType.NetworkTimeout:
                    Talker.Say("You have been disconnected by a network timeout.",
                        Talk.BeepType.Bad);
                    break;
                case NetworkManager.DisconnectType.ServerInitiated:
                    Talker.Say("The server has disconnected you.  " + e.Message,
                        Talk.BeepType.Bad);
                    break;
            }
        }
        private void ListFriends()
        {
            List<FriendInfo> onlineFriends =
                control.instance.Client.Friends.FriendList.FindAll(delegate(FriendInfo f) { return f.IsOnline; });
            string list = "";
            foreach (FriendInfo f in onlineFriends)
            {
                list += f.Name + ", ";
            }
            list += "are online.";
            Talker.Say(list);
        }


        /// <summary>
        /// Check for general commands
        /// </summary>
        /// <param name="message"></param>
        /// <returns>true if command recognized</returns>
        private bool Command(string message)
        {
            switch (message.ToLower())
            {
                case "talk to max":
                    AddInterruption(new Max(control));
                    break;
                case "who is online":
                    ListFriends();
                    break;
                case "open the closet":
                    control.instance.TabConsole.SelectTab("inventory");
                    SelectConversation(inventory);
                    break;
                case "friends":
                    control.instance.TabConsole.SelectTab("friends");
                    SelectConversation(friends);
                    break;
                case "skip":
                    Talker.Flush();
                    Talker.SayMore("Flushed.");
                    break;
                case "talk":
                    control.instance.TabConsole.SelectTab("chat");
                    SelectConversation(chat);
                    break;
                case "voice":
                    control.instance.TabConsole.SelectTab("voice");
                    SelectConversation(voice);
                    break;
                default:
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Dispatch recognized text to appropriate conversation.
        /// </summary>
        /// <param name="message"></param>
        internal void Hear(string message)
        {
            // General commands.
            if (Command(message)) return;

            // Let the current conversation handle it.
            if (currentMode != null)
                currentMode.Hear(message);
        }

        internal void SelectConversation(Mode c)
        {
            if (c == null)
            {
                Talker.Say("Trying to start non-existant conversation", Talk.BeepType.Bad );
                return;
            }
            // Avoid multiple starts.
            if (currentMode == c) return;

            // Let the old conversation deactivate any event hooks, grammars, etc.
            if (currentMode != null)
                currentMode.Stop();

            currentMode = c;
            currentMode.Start();
        }

        internal void SelectConversation(string name)
        {
            if (conversations.ContainsKey(name))
            {
                SelectConversation(conversations[name]);
            }
            else
            {
                Talker.Say("Can not find conversation " + name, Talk.BeepType.Bad);
            }
        }

        /// <summary>
        /// Find an existing conversation by name.
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        /// <remarks>Used for IM sessions.</remarks>
        internal Mode GetConversation(string title)
        {
            if (conversations.ContainsKey(title))
                return conversations[title];

            return null;
        }

        /// <summary>
        /// Add a conversation context to those we are tracking.
        /// </summary>
        /// <param name="m"></param>
        internal void AddConversation(Mode m)
        {
            if (!conversations.ContainsKey(m.Title))
                conversations[m.Title] = m;
        }

        /// <summary>
        /// Remove the context for a conversation that is no longer visible.
        /// </summary>
        /// <param name="name"></param>
        internal void RemoveConversation(string name)
        {
            bool change = false;

            lock (conversations)
            {
                if (conversations.ContainsKey(name))
                {
                    Mode doomed = conversations[name];
                    if (currentMode == doomed)
                    {
                        change = true;
                        currentMode = chat;
                    }
                    if (interrupted == doomed)
                        interrupted = chat;

                    conversations.Remove(name);
                    if (change)
                        SelectConversation(currentMode);
                }
            }
        }

        /// <summary>
        /// Take note of a new interruption.
        /// </summary>
        /// <param name="m"></param>
        internal void AddInterruption(Mode m)
        {
            lock (interruptions)
            {
                // Add to the end of the list.
                interruptions.AddLast(m);

                // If the list WAS empty, start this.
                if (interruptions.Count == 1)
                    StartInterruption();
            }
        }

        internal void ChangeFocus(Mode toThis)
        {
            currentMode = toThis;
             if (currentMode != null)
                currentMode.Start();
        }

 
        /// <summary>
        /// Event handler for new blue dialog boxes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnNotificationDisplayed(object sender, NotificationEventArgs e)
        {
            AddInterruption(new Conversation.BlueMenu(control,e));
        }

        /// <summary>
        /// Event handler for outgoing IMs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Netcom_InstantMessageSent(object sender, Radegast.Netcom.InstantMessageSentEventArgs e)
        {
            // Message to an individual
            Conversation.IMSession sess = (IMSession)control.converse.GetConversation(control.instance.Names.Get(e.TargetID, true));
            if (sess != null)
                sess.OnMessage(Client.Self.AgentID, Client.Self.Name, e.Message);
        }


        /// <summary>
        /// Handle Instant Messages
        /// </summary>
        /// <param name="im"></param>
        /// <param name="simulator"></param>
        void OnInstantMessage(object sender, InstantMessageEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(sync =>
                {
                    Thread.Sleep(100); // Give tab a chance to show up
                    Conversation.IMSession sess = null;
                    string groupName;

                    // All sorts of things come in as a instant messages. For actual messages
                    // we need to match them up with an existing Conversation.  IM Conversations
                    // are keyed by the name of the group or individual involved.
                    switch (e.IM.Dialog)
                    {
                        case InstantMessageDialog.MessageFromAgent:
                            if (control.instance.Groups.ContainsKey(e.IM.IMSessionID))
                            {
                                // Message from a group member
                                groupName = control.instance.Groups[e.IM.IMSessionID].Name;
                                sess = (IMSession)control.converse.GetConversation(groupName);
                                if (sess != null)
                                    sess.OnMessage(e.IM.FromAgentID, e.IM.FromAgentName, e.IM.Message);
                                else
                                    Talker.Say(e.IM.FromAgentName + ", " + e.IM.Message);
                            }
                            else if (e.IM.BinaryBucket.Length >= 2)
                            {
                                // Ad-hoc friend conference
                                sess = (IMSession)control.converse.GetConversation(Utils.BytesToString(e.IM.BinaryBucket));
                                if (sess != null)
                                    sess.OnMessage(e.IM.FromAgentID, e.IM.FromAgentName, e.IM.Message);
                                else
                                    Talker.Say(e.IM.FromAgentName + ", " + e.IM.Message);
                            }
                            else if (e.IM.FromAgentName == "Second Life")
                            {
                                Talker.Say("Second Life says " + e.IM.Message);
                            }
                            else
                            {
                                // Message from an individual
                                sess = (IMSession)control.converse.GetConversation(e.IM.FromAgentName);
                                if (sess != null)
                                    sess.OnMessage(e.IM.FromAgentID, e.IM.FromAgentName, e.IM.Message);
                                else
                                    Talker.Say(e.IM.FromAgentName + ", " + e.IM.Message);
                            }
                            break;

                        case InstantMessageDialog.SessionSend:
                            if (control.instance.Groups.ContainsKey(e.IM.IMSessionID))
                            {
                                // Message from a group member
                                groupName = control.instance.Groups[e.IM.IMSessionID].Name;
                                sess = (IMSession)control.converse.GetConversation(groupName);
                            }
                            else if (e.IM.BinaryBucket.Length >= 2) // ad hoc friends conference
                            {
                                sess = (IMSession)control.converse.GetConversation(Utils.BytesToString(e.IM.BinaryBucket));
                            }

                            if (sess != null)
                                sess.OnMessage(e.IM.FromAgentID, e.IM.FromAgentName, e.IM.Message);
                            break;

                        case InstantMessageDialog.FriendshipOffered:
                            Talker.Say(e.IM.FromAgentName + " is offering friendship.");
                            break;

                        default:
                            break;
                    }
                }
            );

        }
    }
}
