﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using OpenMetaverse;
using OpenMetaverse.Voice;
using OpenMetaverse.StructuredData;

namespace Radegast.Plugin.Voice
{
    class VoiceClient : IDisposable
    {
        private VoiceGateway connector;
        private VoiceControl control;
        internal string sipServer = "";
        private string acctServer = "https://www.bhr.vivox.com/api2/";
        private string connectionHandle;
        private string accountHandle;
        private string sessionHandle;
        private string slvoicePath = "";
        private string slvoiceArgs = "-ll -1";
        private string daemonNode = "127.0.0.1";
        private int daemonPort = 44124;
        private string voiceUser;
        private string voicePassword;
        private string spatialUri;
        private string spatialCredentials;
        private Dictionary<string, Session> sessions;
        private Session spatialSession;
        private Uri currentParcelCap;
        private Uri nextParcelCap;
        private string regionName;
        private Thread posThread;
        private ManualResetEvent posRestart;

        private VoiceGateway.VoicePosition position;

        // Last places Self was located
        private Vector3d oldPosition;
        private Vector3d oldAt;

        private SessionForm sessionForm;
        private List<string> inputDevices;
        public List<string> CaptureDevices { get { return inputDevices; } }
        private List<string> outputDevices;
        public List<string> PlaybackDevices { get { return outputDevices; } }

        public event EventHandler OnSessionCreate;
        public event EventHandler OnSessionRemove;

        internal VoiceClient(VoiceControl pc)
        {
            // Low-level functions that move to libomv.
            control = pc;
            sessions = new Dictionary<string, Session>();
            position = new VoiceGateway.VoicePosition();
            position.UpOrientation = new Vector3d(0.0, 1.0, 0.0);
            position.Velocity = new Vector3d(0.0, 0.0, 0.0);
            oldPosition = new Vector3d(0, 0, 0);
            oldAt = new Vector3d(1, 0, 0);

            slvoiceArgs = "-c";         // Cleanup old instances
            slvoiceArgs += " -ll -1";    // Min logging
            slvoiceArgs += " -i 0.0.0.0:" + daemonPort.ToString();
//            slvoiceArgs += " -lf " + control.instance.ClientDir;
            
            // Here is real client-specific code
            OnSessionCreate += new EventHandler(VoiceClient_OnSessionCreate);
            OnSessionRemove += new EventHandler(VoiceClient_OnSessionRemove);
        }

        /// <summary>
        /// Create user interface for a session
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void VoiceClient_OnSessionCreate(object sender, EventArgs e)
        {
            Session s = sender as Session;

            // Create free-standing Form to represent this session.
            control.instance.MainForm.BeginInvoke(
               new MethodInvoker(delegate()
                {
                    sessionForm = new SessionForm(control, s);
                    sessionForm.Show();
                }));

            control.instance.MainForm.TabConsole.DisplayNotificationInChat(
                "Voice started in " + s.RegionName);

            // Default Mic off and Spkr on
            MicMute = true;
            SpkrMute = false;
            SpkrLevel = 64;
            MicLevel = 64;           
        }

        /// <summary>
        /// Remove user interface for a session
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void VoiceClient_OnSessionRemove(object sender, EventArgs e)
        {
            Session s = sender as Session;
            if (sessionForm != null)
                control.instance.MainForm.BeginInvoke(
                   new MethodInvoker(delegate()
                   {
                       sessionForm.Hide();
                       sessionForm = null;
                   }));

            control.instance.MainForm.TabConsole.DisplayNotificationInChat(
                "Voice disconnected in " + s.RegionName);
        }

        /// <summary>
        /// Start up the Voice service.
        /// </summary>
        internal void Start()
        {
            // Start the background thread
            if (posThread != null && posThread.IsAlive)
                posThread.Abort();
            posThread = new Thread(new ThreadStart(PositionThreadBody));
            posThread.Name = "VoicePositionUpdate";
            posThread.IsBackground = true;
            posRestart = new ManualResetEvent(false);
            posThread.Start();

            connector = new VoiceGateway();

            control.instance.Client.Network.OnEventQueueRunning +=
                new NetworkManager.EventQueueRunningCallback(Network_OnEventQueueRunning);

            // Connection events
            connector.OnDaemonRunning +=
                 new VoiceGateway.DaemonRunningCallback(connector_OnDaemonRunning);
            connector.OnDaemonCouldntRun +=
                new VoiceGateway.DaemonCouldntRunCallback(connector_OnDaemonCouldntRun);
            connector.OnConnectorCreateResponse +=
                new VoiceGateway.ConnectorCreateResponseCallback(connector_OnConnectorCreateResponse);
            connector.OnDaemonConnected +=
                new VoiceGateway.DaemonConnectedCallback(connector_OnDaemonConnected);
            connector.OnDaemonCouldntConnect +=
                new VoiceGateway.DaemonCouldntConnectCallback(connector_OnDaemonCouldntConnect);
            connector.OnAuxAudioPropertiesEvent +=
                new VoiceGateway.AuxAudioPropertiesEventCallback(connector_OnAuxAudioPropertiesEvent);

            // Session events
            connector.OnSessionNewEvent +=
                new VoiceGateway.SessionNewEventCallback(connector_OnSessionNewEvent);
            connector.OnSessionCreateResponse +=
                new VoiceGateway.SessionCreateResponseCallback(connector_OnSessionCreateResponse);
            connector.OnSessionStateChangeEvent +=
                new VoiceGateway.SessionStateChangeEventCallback(connector_OnSessionStateChangeEvent);
            connector.OnSessionAddedEvent +=
                new VoiceGateway.SessionAddedEventCallback(connector_OnSessionAddedEvent);

            // Session Participants events
            connector.OnSessionParticipantPropertiesEvent +=
                new VoiceGateway.SessionParticipantPropertiesEventCallback(connector_OnSessionParticipantPropertiesEvent);
            connector.OnSessionParticipantStateChangeEvent +=
                new VoiceGateway.SessionParticipantStateChangeEventCallback(connector_OnSessionParticipantStateChangeEvent);
            connector.OnSessionParticipantUpdatedEvent +=
                new VoiceGateway.SessionParticipantUpdatedEventCallback(connector_OnSessionParticipantUpdatedEvent);
            connector.OnSessionParticipantAddedEvent +=
                new VoiceGateway.SessionParticipantAddedEventCallback(connector_OnSessionParticipantAddedEvent);

            // Tuning events
            connector.OnAuxCaptureAudioStartResponse +=
                new VoiceGateway.AuxCaptureAudioStartResponseCallback(connector_OnAuxCaptureAudioStartResponse);
            connector.OnAuxGetCaptureDevicesResponse +=
                new VoiceGateway.AuxGetCaptureDevicesResponseCallback(connector_OnAuxGetCaptureDevicesResponse);
            connector.OnAuxGetRenderDevicesResponse +=
                new VoiceGateway.AuxGetRenderDevicesResponseCallback(connector_OnAuxGetRenderDevicesResponse);

            // Account events
            connector.OnAccountLoginResponse +=
                new VoiceGateway.AccountLoginResponseCallback(connector_OnAccountLoginResponse);

            Form main = control.instance.MainForm;
            main.MouseDown += new MouseEventHandler(OnMouseDown);
            main.MouseUp += new MouseEventHandler(OnMouseUp);
            control.voiceConsole.MouseDown +=
                new MouseEventHandler(OnMouseDown);
            control.voiceConsole.MouseUp +=
                new MouseEventHandler(OnMouseUp);

            Logger.Log("Voice initialized", Helpers.LogLevel.Info);
        }

        internal void Stop()
        {
            control.instance.Client.Network.OnEventQueueRunning -=
                new NetworkManager.EventQueueRunningCallback(Network_OnEventQueueRunning);

            if (connector != null)
            {
                // Connection events
                connector.OnDaemonRunning -=
                     new VoiceGateway.DaemonRunningCallback(connector_OnDaemonRunning);
                connector.OnDaemonCouldntRun -=
                    new VoiceGateway.DaemonCouldntRunCallback(connector_OnDaemonCouldntRun);
                connector.OnConnectorCreateResponse -=
                    new VoiceGateway.ConnectorCreateResponseCallback(connector_OnConnectorCreateResponse);
                connector.OnDaemonConnected -=
                    new VoiceGateway.DaemonConnectedCallback(connector_OnDaemonConnected);
                connector.OnDaemonCouldntConnect -=
                    new VoiceGateway.DaemonCouldntConnectCallback(connector_OnDaemonCouldntConnect);
                connector.OnAuxAudioPropertiesEvent -=
                    new VoiceGateway.AuxAudioPropertiesEventCallback(connector_OnAuxAudioPropertiesEvent);

                // Session events
                connector.OnSessionNewEvent -=
                    new VoiceGateway.SessionNewEventCallback(connector_OnSessionNewEvent);
                connector.OnSessionCreateResponse -=
                    new VoiceGateway.SessionCreateResponseCallback(connector_OnSessionCreateResponse);
                connector.OnSessionStateChangeEvent -=
                    new VoiceGateway.SessionStateChangeEventCallback(connector_OnSessionStateChangeEvent);
                connector.OnSessionAddedEvent -=
                    new VoiceGateway.SessionAddedEventCallback(connector_OnSessionAddedEvent);

                // Session Participants events
                connector.OnSessionParticipantPropertiesEvent -=
                    new VoiceGateway.SessionParticipantPropertiesEventCallback(connector_OnSessionParticipantPropertiesEvent);
                connector.OnSessionParticipantStateChangeEvent -=
                    new VoiceGateway.SessionParticipantStateChangeEventCallback(connector_OnSessionParticipantStateChangeEvent);
                connector.OnSessionParticipantUpdatedEvent -=
                    new VoiceGateway.SessionParticipantUpdatedEventCallback(connector_OnSessionParticipantUpdatedEvent);
                connector.OnSessionParticipantAddedEvent -=
                    new VoiceGateway.SessionParticipantAddedEventCallback(connector_OnSessionParticipantAddedEvent);

                // Tuning events
                connector.OnAuxCaptureAudioStartResponse -=
                    new VoiceGateway.AuxCaptureAudioStartResponseCallback(connector_OnAuxCaptureAudioStartResponse);
                connector.OnAuxGetCaptureDevicesResponse -=
                    new VoiceGateway.AuxGetCaptureDevicesResponseCallback(connector_OnAuxGetCaptureDevicesResponse);
                connector.OnAuxGetRenderDevicesResponse -=
                    new VoiceGateway.AuxGetRenderDevicesResponseCallback(connector_OnAuxGetRenderDevicesResponse);

                // Account events
                connector.OnAccountLoginResponse -=
                    new VoiceGateway.AccountLoginResponseCallback(connector_OnAccountLoginResponse);
            }

            // Stop the background thread
            if (posThread != null)
            {
                PosUpdating(false);

                if (posThread.IsAlive)
                    posThread.Abort();
                posThread = null;
            }

            // Close all sessions
            foreach (Session s in sessions.Values)
            {
                s.Close();
            }

            if (connector != null)
            {
                connector.ConnectorInitiateShutdown(connectionHandle);
                connector.StopDaemon();
            }
        }

        /// <summary>
        /// Cleanup oject resources
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        internal string GetVoiceDaemonPath()
        {
            if (Environment.OSVersion.Platform != PlatformID.MacOSX &&
                Environment.OSVersion.Platform != PlatformID.Unix)
            {
                string progFiles;
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ProgramFiles(x86)")))
                {
                    progFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                }
                else
                {
                    progFiles = Environment.GetEnvironmentVariable("ProgramFiles");
                }

                return Path.Combine(progFiles, @"SecondLife\SLVoice.exe");
            }
            else
            {
                //TODO return Path.Combine(progFiles, @"SecondLife\SLVoice");
            }

            return string.Empty;
        }
        /// <summary>
        /// Request voice cap when changeing regions
        /// </summary>
        /// <param name="simulator"></param>
        void Network_OnEventQueueRunning(Simulator simulator)
        {
            // We only care about the sim we are in.
            if (simulator != control.instance.Client.Network.CurrentSim)
                return;

            // Did we provision voice login info?
            if (string.IsNullOrEmpty(voiceUser))
            {
                // The startup steps are
                //  0. Get voice account info
                //  1. Start Daemon
                //  2. Create TCP connection
                //  3. Create Connector
                //  4. Account login
                //  5. Create session

                // Get the voice provisioning data
                System.Uri vCap =
                    control.instance.Client.Network.CurrentSim.Caps.CapabilityURI("ProvisionVoiceAccountRequest");

                // Do we have voice capability?
                if (vCap == null)
                {
                    Logger.Log("Null voice capability", Helpers.LogLevel.Warning);
                }
                else
                {
                    OpenMetaverse.Http.CapsClient capClient =
                        new OpenMetaverse.Http.CapsClient(vCap);
                    capClient.OnComplete +=
                        new OpenMetaverse.Http.CapsClient.CompleteCallback(cClient_OnComplete);
                    OSD postData = new OSD();

                    // STEP 0
                    Logger.Log("Requesting voice capability", Helpers.LogLevel.Info);
                    capClient.BeginGetResponse(postData, OSDFormat.Xml, 10000);
                }

                return;
            }
            else
            {
                // Change voice session for this region.
                ParcelChanged();
            }
        }


       #region Partcipants
        void connector_OnSessionParticipantStateChangeEvent(
            string SessionHandle,
            int StatusCode,
            string StatusString,
            VoiceGateway.ParticipantState State,
            string ParticipantURI,
            string AccountName,
            string DisplayName,
            VoiceGateway.ParticipantType ParticipantType)
        {

        }

        void connector_OnSessionParticipantPropertiesEvent(string SessionHandle,
            string ParticipantURI, bool IsLocallyMuted, bool IsModeratorMuted,
            bool IsSpeaking, int Volume, float Energy)
        {
            control.voiceConsole.SetSStatus(ParticipantURI);
            control.voiceConsole.SetSpkrDial((int)(Energy * 100.0));
        }

        void connector_OnSessionParticipantUpdatedEvent(string sessionHandle,
            string URI,
            bool isMuted,
            bool isSpeaking,
            int volume,
            float energy)
        {
            Session s = FindSession(sessionHandle, false);
            if (s == null) return;
            s.ParticipantUpdate(URI, isMuted, isSpeaking, volume, energy);
        }

        public string SIPFromUUID(UUID id)
        {
            return "sip:" +
                nameFromID(id) +
                "@" +
                sipServer;
        }

        private static string nameFromID(UUID id)
        {
            string result = null;

            if (id==UUID.Zero)
                return result;

            // Prepending this apparently prevents conflicts with reserved names inside the vivox and diamondware code.
	        result = "x";
	
	        // Base64 encode and replace the pieces of base64 that are less compatible 
	        // with e-mail local-parts.
	        // See RFC-4648 "Base 64 Encoding with URL and Filename Safe Alphabet"
            byte[] encbuff = id.GetBytes();
            result += Convert.ToBase64String(encbuff);
            result = result.Replace('+', '-');
            result = result.Replace('/', '_');
		
        	return result;
       }

        void connector_OnSessionParticipantAddedEvent(
                string SessionGroupHandle,
                string SessionHandle,
                string ParticipantUri,
                string AccountName,
                string DisplayName,
                OpenMetaverse.Voice.VoiceGateway.ParticipantType type,
                string Application )
        {
            Session s = FindSession(sessionHandle, false);
            if (s == null) return;
            s.AddParticipant( ParticipantUri );
        }

        void connector_OnSessionParticipantRemovedEvent(
                string SessionGroupHandle,
                string SessionHandle,
                string ParticipantUri,
                string AccountName,
                string Reason )
        {
            Session s = FindSession(sessionHandle, false);
            if (s == null) return;
            s.RemoveParticipant(ParticipantUri);
        }
       #endregion

        #region Sessions
        void connector_OnSessionAddedEvent(string sessionGroupHandle,
            string newSessionHandle,
            string URI,
            bool isChannel,
            bool isIncoming)
        {
           sessionHandle = newSessionHandle;

            // Create our session context.
            Session s = FindSession(sessionHandle, true);
            s.RegionName = regionName;

            Logger.Log("Added voice session in " + regionName, Helpers.LogLevel.Info);
            spatialSession = s;

            // Tell any user-facing code.
            if (OnSessionCreate != null)
                OnSessionCreate(s, null);
        }

        /// <summary>
        /// Handle session creation
        /// </summary>
        void connector_OnSessionCreateResponse(int ReturnCode,
            int StatusCode, string StatusString,
            string SessionHandle,
            VoiceGateway.VoiceRequest Request)
        {
            if (StatusCode == 0) return;
            control.voiceConsole.SetSStatus(StatusString);
        }

        /// <summary>
        /// Handle a change in session state
        /// </summary>
        /// <param name="SessionHandle"></param>
        /// <param name="StatusCode"></param>
        /// <param name="StatusString"></param>
        /// <param name="State"></param>
        /// <param name="URI"></param>
        /// <param name="IsChannel"></param>
        /// <param name="ChannelName"></param>
        void connector_OnSessionStateChangeEvent(string SessionHandle,
            int StatusCode,
            string StatusString,
            VoiceGateway.SessionState State,
            string URI,
            bool IsChannel,
            string ChannelName)
        {
            Session s;

            switch (State)
            {
                case VoiceGateway.SessionState.Connected:
                    s = FindSession(SessionHandle, true);
                    sessionHandle = SessionHandle;
                    s.RegionName = regionName;
                    spatialSession = s;

                    Logger.Log("Voice connected in " + regionName, Helpers.LogLevel.Info);
                    // Tell any user-facing code.
                    if (OnSessionCreate != null)
                        OnSessionCreate(s, null);
                    break;

                case VoiceGateway.SessionState.Disconnected:
                    s = FindSession(sessionHandle, false);
                    sessions.Remove(sessionHandle);

                    if (s != null)
                    {
                        Logger.Log("Voice disconnected in " + s.RegionName, Helpers.LogLevel.Info);

                        // Inform interested parties
                        if (OnSessionRemove != null)
                            OnSessionRemove(s, null);

                        if (s == spatialSession)
                            spatialSession = null;
                    }

                    // The previous session is now ended.  Check for a new one and
                    // start it going.
                    if (nextParcelCap != null)
                    {
                        currentParcelCap = nextParcelCap;
                        nextParcelCap = null;
                        RequestParcelInfo(currentParcelCap);
                    }
                    break;
            }
            

        }
        
        /// <summary>
        /// Close a voice session
        /// </summary>
        /// <param name="sessionHandle"></param>
        internal void CloseSession(string sessionHandle)
        {
            if (!sessions.ContainsKey(sessionHandle))
                return;

            // Clean up spatial pointers.
            Session s = sessions[sessionHandle];
            if (s.IsSpatial)
            {
                spatialSession = null;
                currentParcelCap = null;
            }

            // Remove this session from the master session list
            sessions.Remove(sessionHandle);

            // Let any user-facing code clean up.
            if (OnSessionRemove != null)
                OnSessionRemove(s, null);

            // Tell SLVoice to clean it up as well.
            connector.SessionTerminate(sessionHandle);
        }

        /// <summary>
        /// Locate a Session context from its handle
        /// </summary>
        /// <param name="sessionHandle"></param>
        /// <returns></returns>
        /// <remarks>Creates the session context if it does not exist.</remarks>
        Session FindSession(string sessionHandle, bool make)
        {
            if (sessions.ContainsKey(sessionHandle))
                return sessions[sessionHandle];

            if (!make) return null;

            // Create a new session and add it to the sessions list.
            Session s = new Session(control, connector, sessionHandle);
            sessions.Add(sessionHandle, s);
            return s;
        }

#endregion

        #region MinorResponses

        void connector_OnSessionNewEvent(string AccountHandle, string SessionHandle,
            string URI, bool IsChannel, string Name, string AudioMedia)
        {
            control.voiceConsole.SetCStatus("New session " + Name);
        }

        void connector_OnAuxCaptureAudioStartResponse(int ReturnCode, int StatusCode,
            string StatusString, VoiceGateway.VoiceRequest Request)
        {
            control.voiceConsole.SetCStatus("Capture " + StatusString);
        }

        void connector_OnAuxAudioPropertiesEvent(bool MicIsActive,
            float MicEnergy, float MicVolume, float SpeakerVolume)
        {
            control.voiceConsole.SetMicDial( (int)(MicEnergy * 100.0));
        }

        void OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                control.voiceConsole.micMute_Set(true);
                MicMute =true;
            }
        }

        void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                control.voiceConsole.micMute_Set(false);
                MicMute = false;
            }
        }
        #endregion

        /// <summary>
        /// Handle completion of main voice cap request.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="result"></param>
        /// <param name="error"></param>
        void cClient_OnComplete(OpenMetaverse.Http.CapsClient client,
            OpenMetaverse.StructuredData.OSD result,
            Exception error)
        {
            if (error != null)
            {
                control.voiceConsole.SetCStatus(error.Message);
                return;
            }

            Logger.Log("Voice provisioned", Helpers.LogLevel.Info);

            OpenMetaverse.StructuredData.OSDMap pMap = result as OpenMetaverse.StructuredData.OSDMap;

            // We can get back 4 interesting values:
            //      voice_sip_uri_hostname
            //      voice_account_server_name   (actually a full URI)
            //      username
            //      password
            if (pMap.ContainsKey("voice_sip_uri_hostname"))
                sipServer = pMap["voice_sip_uri_hostname"].AsString();
            if (pMap.ContainsKey("voice_account_server_name"))
                acctServer = pMap["voice_account_server_name"].AsString();
            voiceUser = pMap["username"].AsString();
            voicePassword = pMap["password"].AsString();

            // Start the SLVoice daemon
            slvoicePath = GetVoiceDaemonPath();

            // Test if the executable exists
            if (!System.IO.File.Exists(slvoicePath))
            {
                control.voiceConsole.SetCStatus("SLVoice.exe is missing");
                Logger.Log("SLVoice is missing", Helpers.LogLevel.Error);
                return;
            }

            // STEP 1
            connector.StartDaemon(slvoicePath, slvoiceArgs);
        }

        #region Daemon
        void connector_OnDaemonCouldntConnect()
        {
            Logger.Log("No voice daemon connect", Helpers.LogLevel.Error);
        }

        void connector_OnDaemonCouldntRun()
        {
            Logger.Log("Daemon not started", Helpers.LogLevel.Error);
        }

        /// <summary>
        /// Daemon has started so connect to it.
        /// </summary>
        void connector_OnDaemonRunning()
        {
            connector.OnDaemonRunning -=
                new VoiceGateway.DaemonRunningCallback(connector_OnDaemonRunning);
            Logger.Log("Daemon started", Helpers.LogLevel.Info);

            // STEP 2
            connector.ConnectToDaemon(daemonNode, daemonPort);
        }
 
        /// <summary>
        /// The daemon TCP connection is open.
        /// </summary>
        /// <param name="ReturnCode"></param>
        /// <param name="VersionID"></param>
        /// <param name="StatusCode"></param>
        /// <param name="StatusString"></param>
        /// <param name="ConnectorHandle"></param>
        /// <param name="Request"></param>
        void connector_OnDaemonConnected()
        {
            Logger.Log("Daemon connected", Helpers.LogLevel.Info);

            // The connector is what does the logging.
            VoiceGateway.VoiceLoggingSettings vLog = new VoiceGateway.VoiceLoggingSettings();
            vLog.Folder = control.instance.ClientDir;
            vLog.Enabled = true;
            vLog.FileNamePrefix = "Voice";
            vLog.LogLevel = 5;


            // STEP 3
            int reqId = connector.ConnectorCreate(
                "V2 SDK",       // Magic value keeps SLVoice happy
                acctServer,     // Account manager server
                30000, 30099,   // port range
                vLog);
            if (reqId < 0)
            {
                control.voiceConsole.SetCStatus("No connector");
            }
        }

        /// <summary>
        /// Handle creation of the Connector.
        /// </summary>
        /// <param name="ReturnCode"></param>
        /// <param name="VersionID"></param>
        /// <param name="StatusCode"></param>
        /// <param name="StatusString"></param>
        /// <param name="ConnectorHandle"></param>
        /// <param name="Request"></param>
        void connector_OnConnectorCreateResponse(int ReturnCode,
            string VersionID, int StatusCode,
            string StatusString, string ConnectorHandle, VoiceGateway.VoiceRequest Request)
        {
            Logger.Log("Voice daemon protocol started "+StatusString, Helpers.LogLevel.Info);

            connectionHandle = ConnectorHandle;

            if (StatusCode != 0)
                return;

            // Request the available devices, to populate the GUI for choosing.
            connector.AuxGetCaptureDevices();
            connector.AuxGetRenderDevices();

            // STEP 4
            connector.AccountLogin(
                connectionHandle,
                voiceUser,
                voicePassword,
                "VerifyAnswer",   // This can also be "AutoAnswer"
                "",             // Default account management server URI
                10,            // Throttle state changes
                true);          // Enable buddies and presence
        }
        #endregion

        void connector_OnAccountLoginResponse(int ReturnCode, int StatusCode, string StatusString,
            string AccountHandle,
            VoiceGateway.VoiceRequest Request)
        {
            Logger.Log("Account Login "+StatusString, Helpers.LogLevel.Info );
            accountHandle = AccountHandle;

            ParcelChanged();
        }

        #region Tuning
        /// <summary>
        /// Handle response to audio output device query
        /// </summary>
        /// <param name="ReturnCode"></param>
        /// <param name="StatusCode"></param>
        /// <param name="StatusString"></param>
        /// <param name="RenderDevices"></param>
        /// <param name="CurrentRenderDevice"></param>
        /// <param name="Request"></param>
        void connector_OnAuxGetRenderDevicesResponse(int ReturnCode,
           int StatusCode,
           string StatusString,
           List<string> RenderDevices,
           string CurrentRenderDevice,
           VoiceGateway.VoiceRequest Request)
        {
            // Put the list of devices on the Setup form
            // Default comes form the settings file
            string selectedOutput =
                control.instance.GlobalSettings["plugin.voice.render"].AsString();
            outputDevices = RenderDevices;
            control.voiceConsole.SetRenderDevices(RenderDevices, selectedOutput);
        }

        /// <summary>
        /// Handle response to audio input device query
        /// </summary>
        /// <param name="ReturnCode"></param>
        /// <param name="StatusCode"></param>
        /// <param name="StatusString"></param>
        /// <param name="CaptureDevices"></param>
        /// <param name="CurrentCaptureDevice"></param>
        /// <param name="Request"></param>
        void connector_OnAuxGetCaptureDevicesResponse(int ReturnCode,
            int StatusCode, string StatusString,
            List<string> CaptureDevices,
            string CurrentCaptureDevice,
            VoiceGateway.VoiceRequest Request)
        {
            // Put the list of devices on the Setup form.
            // Default comes from the settings file.
            string selectedInput =
                control.instance.GlobalSettings["plugin.voice.capture"].AsString();
            inputDevices = CaptureDevices;
            control.voiceConsole.SetCaptureDevices( CaptureDevices, selectedInput);
        }

        #endregion



        /// <summary>
        /// Set voice channel for new parcel
        /// </summary>
        ///
        internal void ParcelChanged()
        {
            // Get the capability for this parcel.
            Caps c = control.instance.Client.Network.CurrentSim.Caps;
            System.Uri pCap = c.CapabilityURI("ParcelVoiceInfoRequest");

            if (pCap == null)
            {
                Logger.Log("Null voice capability", Helpers.LogLevel.Error);
                return;
            }
            
            if (pCap == currentParcelCap)
            {
                // Parcel has not changed, so nothing to do.
                return;
            }

            // Parcel has changed.  If we were already in a spatial session, we have to close it first.
            if (spatialSession != null)
            {
                nextParcelCap = pCap;
                CloseSession( spatialSession.SessionHandle );
            }

            // Not already in a session, so can start the new one.
            RequestParcelInfo(pCap);
        }

        private OpenMetaverse.Http.CapsClient parcelCap;

        /// <summary>
        /// Request info from a parcel capability Uri.
        /// </summary>
        /// <param name="cap"></param>
        
        void RequestParcelInfo(Uri cap)
        {
            Logger.Log("Requesting region voice info", Helpers.LogLevel.Info);

            parcelCap = new OpenMetaverse.Http.CapsClient(cap);
            parcelCap.OnComplete +=
                new OpenMetaverse.Http.CapsClient.CompleteCallback(pCap_OnComplete);
            OSD postData = new OSD();

            currentParcelCap = cap;
            parcelCap.BeginGetResponse(postData, OSDFormat.Xml, 10000);
        }

        /// <summary>
        /// Receive parcel voice cap
        /// </summary>
        /// <param name="client"></param>
        /// <param name="result"></param>
        /// <param name="error"></param>
        void pCap_OnComplete(OpenMetaverse.Http.CapsClient client,
            OpenMetaverse.StructuredData.OSD result,
            Exception error)
        {
            parcelCap.OnComplete -=
                new OpenMetaverse.Http.CapsClient.CompleteCallback(pCap_OnComplete);
            parcelCap = null;

            if (error != null)
            {
                Logger.Log("Region voice cap "+error.Message, Helpers.LogLevel.Error);
                return;
            }

            OpenMetaverse.StructuredData.OSDMap pMap = result as OpenMetaverse.StructuredData.OSDMap;

            regionName = pMap["region_name"].AsString();

            if (pMap.ContainsKey("voice_credentials"))
            {
                OpenMetaverse.StructuredData.OSDMap cred =
                    pMap["voice_credentials"] as OpenMetaverse.StructuredData.OSDMap;

                if (cred.ContainsKey("channel_uri"))
                    spatialUri = cred["channel_uri"].AsString();
                if (cred.ContainsKey("channel_credentials"))
                    spatialCredentials = cred["channel_credentials"].AsString();
            }

            if (spatialUri == null || spatialUri == "")
            {
                control.voiceConsole.SetSStatus("No voice chat allowed here");
                return;
            }

            Logger.Log("Voice connecting for region " + regionName, Helpers.LogLevel.Info);

            // STEP 5
            int reqId = connector.SessionCreate(
                accountHandle,
                spatialUri, // uri
                "", // Channel name seems to be always null
                spatialCredentials, // spatialCredentials, // session password
                true,   // Join Audio
                false,   // Join Text
                "");
            if (reqId < 0)
            {
                control.voiceConsole.SetCStatus("Session ReqID " + reqId.ToString());
            }
        }

        
#region GUI
        internal int MicLevel
        {
            set
            {
                connector.ConnectorSetLocalMicVolume(connectionHandle, value);
            }
        }
        internal int SpkrLevel
        {
            set
            {
                connector.ConnectorSetLocalSpeakerVolume(connectionHandle, value);
            }
        }

        /// <summary>
        /// Change the audio capture device by user input
        /// </summary>
        /// <param name="name"></param>
        internal void SetMicDevice(string name)
         {
             connector.AuxSetCaptureDevice(name);
             control.instance.GlobalSettings["plugin.voice.capture"] =
                OSD.FromString(name);
         }

        /// <summary>
        /// Change the audio render device by user input
        /// </summary>
        /// <param name="name"></param>
        internal void SetSpkrDevice(string name)
        {
             connector.AuxSetRenderDevice(name);
             control.instance.GlobalSettings["plugin.voice.render"] =
                OSD.FromString(name);
        }

        internal bool MicMute
        {
            set
            {
                connector.ConnectorMuteLocalMic(connectionHandle, value);
            }
        }

        internal bool SpkrMute
        {
            set
            {
                connector.ConnectorMuteLocalSpeaker(connectionHandle, value);
            }
        }

        /// <summary>
        /// Toggle test mode
        /// </summary>
        /// <param name="on"></param>
        internal void TestMode(bool on)
        {
            if (on)
            {
                if (spatialSession != null)
                {
                    spatialSession.Close();
                    spatialSession = null;
                }
                connector.AuxCaptureAudioStart(0);
            }
            else
            {
                connector.AuxCaptureAudioStop();
                ParcelChanged();
            }
        }
#endregion

        internal class SessionListItem : System.Windows.Forms.ListViewItem
        {
//            OpenMetaverse.Voice.VoiceParticipant participant;
        }

#region Location Update
        /// <summary>
        /// Tell Vivox where we are standing
        /// </summary>
        /// <remarks>This has to be called when we move or turn.</remarks>
        internal void UpdatePosition(AgentManager self)
        {
            // Get position in Global coordinates
            Vector3d OMVpos = new Vector3d(self.GlobalPosition);

            // Do not send trivial updates.
            if (OMVpos.ApproxEquals(oldPosition, 1.0))
                return;

            oldPosition = OMVpos;

            // Convert to the coordinate space that Vivox uses
            // OMV X is East, Y is North, Z is up
            // VVX X is East, Y is up, Z is South
            position.Position = new Vector3d(OMVpos.X, OMVpos.Z, -OMVpos.Y);

            // TODO Rotate these two vectors

            // Get azimuth from the facing Quaternion.
            // By definition, facing.W = Cos( angle/2 )
            double angle = 2.0 * Math.Acos(self.Movement.BodyRotation.W);

            position.LeftOrientation = new Vector3d(-1.0, 0.0, 0.0);
            position.AtOrientation = new Vector3d((float)Math.Acos(angle), 0.0, -(float)Math.Asin(angle));

            connector.SessionSet3DPosition(
                sessionHandle,
                position,
                position);
        }

        /// <summary>
        /// Start and stop updating out position.
        /// </summary>
        /// <param name="go"></param>
        internal void PosUpdating( bool go )
        {
            if (go)
                posRestart.Set();
            else
                posRestart.Reset();
        }

        private void PositionThreadBody()
        {
            while (true)
            {
                posRestart.WaitOne();
                Thread.Sleep(1500);
                UpdatePosition(control.instance.Client.Self);
            }
        }
#endregion

    }
}
