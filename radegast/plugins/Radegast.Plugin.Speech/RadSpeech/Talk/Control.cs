﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using OpenMetaverse;
using Radegast;


namespace RadegastSpeech.Talk
{
    /// <summary>
    /// General control of speech synthesis
    /// </summary>
    internal class Control : AreaControl
    {
		private int seq = 0;
        /// <summary>
        /// Interface to the platform-specific synthesizer
        /// </summary>
        private Synthesizer syn;
        /// <summary>
        /// Queue of utterances waiting to be said
        /// </summary>
        private Queue<QueuedSpeech> queue;
        private bool running = true;
        /// <summary>
        /// Background thread to service the queue
        /// </summary>
        private Thread speakingThread;

        private Substitutions subs;

        /// <summary>
        /// The voice used for all system announcements.
        /// </summary>
        internal AssignedVoice SystemVoice;
        internal AssignedVoice ObjectVoice;
        // System voice comes over the left shoulder.
        private Vector3 SYSTEMPOSITION = new Vector3(-3.0f, +3.0f, 0);

        /// <summary>
        /// The voice library manager
        /// </summary>
        internal Voices voices;
        /// <summary>
        /// Name of the file to be written by the synthesizer.
        /// </summary>
        private readonly string AUDIOFILE;
        private bool firstTime = true;

        internal Control(PluginControl pc)
            : base(pc)
        {
            AUDIOFILE = Path.Combine(control.instance.UserDir, "ChatTemp");
            
            // Substitutions from chat-ese to English.
            subs = new Substitutions( control );
            syn = new Synthesizer(control);
            if (syn == null)
                return;

            voices = new Voices(control);
        }

        /// <summary>
        /// Initialize the synthesis subsystem.
        /// </summary>
        internal override void Start()
        {
            if (syn==null) return;

            // Fire up the synth subsystem.
            syn.Start();

            // Get the list of installed voices.  This is done in a
            // platform-specific way.
            voices.Populate( syn.GetVoices());
            voices.Start();

            // Create the work queue if we can deal with it
            if (control.osLayer != null)
            {
                queue = new Queue<QueuedSpeech>();

                // Start the background thread that does all the text-to-speech.
                speakingThread = new Thread(new ThreadStart(SpeakLoop));
                speakingThread.IsBackground = true;
                speakingThread.Name = "SpeakingThread";
                speakingThread.Start();
            }
        }

        /// <summary>
        /// Shut down the background thread.
        /// </summary>
        internal override void Shutdown()
        {
            // If the background thread is alive, send it the special
            // empty utterance that will make it shut down gracefully.
            if (speakingThread != null && speakingThread.IsAlive)
            {
                Say( null, null, new Vector3(0, 0, 0), null);
                Thread.Sleep(600);
            }
            speakingThread = null;

            // Shut down the synthesizer.
            syn.Shutdown();
        }

        /// <summary>
        /// Assign system voices the first time.
        /// </summary>
        private void FirstCheck()
        {
            if (!firstTime || control.osLayer==null) return;

            firstTime = false;
            // Initialize the library of installed voices.
            SystemVoice = voices.VoiceFor("System");
            ObjectVoice = voices.VoiceFor("Object");
        }

        /// <summary>
        /// Add a rule substituting one word for another
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        internal void AddSubstitution( string a, string b )
        {
            subs.Add(a,b);
        }

        /// <summary>
        /// Add a rule removing a word from the utterance
        /// </summary>
        /// <param name="a"></param>
        internal void AddRemoval( string a)
        {
            subs.Add(a);
        }

        /// <summary>
        /// Empty the queue of things waiting to say.
        /// </summary>
        internal void Flush()
        {
            lock (queue)
            {
                control.sound.Stop();  // Abort any playing speech.
                syn.Stop();
                queue.Clear();  // Remove any pending speech.
            }
        }

        #region WaysOfSpeaking
        /// <summary>
        /// Say something in the System voice.
        /// </summary>
        /// <param name="message"></param>
        internal void Say( string message )
        {
            FirstCheck();
            Say("Notice", message, SYSTEMPOSITION, SystemVoice, false, BeepType.None);
        }

        /// <summary>
        /// Say something that requires action or announces a new session
        /// </summary>
        /// <param name="message"></param>
        /// <param name="beep"></param>
        internal void Say(string message, BeepType beep)
        {
            FirstCheck();
            Say("Notice", message, SYSTEMPOSITION, SystemVoice, false, beep );
        }

        /// <summary>
        /// Say something in a named, but disembodied, voice.
        /// </summary>
        /// <param name="who"></param>
        /// <param name="message"></param>
        internal void Say( string who, string message )
        {
            Say( who, message, SYSTEMPOSITION, voices.VoiceFor(who), false, BeepType.None );
        }


        /// <summary>
        /// Continue a conversation without any introduction.
        /// </summary>
        /// <param name="message"></param>
        /// <remarks>This is used for quick exchanges while navigating something.</remarks>
        internal void SayMore(string message)
        {
            FirstCheck();
            Say(null, message, SYSTEMPOSITION, SystemVoice, false, BeepType.None);
        }

        internal void SayMore(string message, Vector3 position)
        {
            FirstCheck();
            Say(null, message, position, SystemVoice, false, BeepType.None);
        }

        internal void SayMore(string message, BeepType beep)
        {
            FirstCheck();
            Say(null, message, SYSTEMPOSITION, SystemVoice, false, beep);
        }

        internal void Say(string who, string message, Vector3 position)
        {
            Say(who, message, position, null);
        }

        internal void SayIMPerson(string who, string message, Vector3 position, AssignedVoice av)
        {
            SayPersonSpatial(who, message, position, av, false);
        }

        /// <summary>
        /// Speak for another avatar.
        /// </summary>
        /// <param name="who"></param>
        /// <param name="message"></param>
        /// <param name="position"></param>
        /// <param name="av"></param>
        /// <remarks>This includes special processing for /me and other chat
        /// conventions.</remarks>
        internal void SayPerson(string who, string message, Vector3 position, AssignedVoice av)
        {
            SayPersonSpatial(who, message, position, av, true);
        }

        private void SayPersonSpatial( string who, string message, Vector3 position, AssignedVoice av, bool spatial )
        {
            // Fix up any chat-ese symbols and pronounciations.
            message = subs.FixExpressions(message);
            if (message == "") return;

            // Distinguish speech from self-narration.
            if (message.StartsWith("/me"))
            {
                SayAction(
                    who,
                    message.Substring(3),
                    position,
                    av,
                    spatial);
            }
            else
            {
                Say(
                    who,
                    message,
                    position,
                    av,
                    spatial,
                    BeepType.None);
            }
        }

        /// <summary>
        /// Speak for an object, with position and the system voice.
        /// </summary>
        /// <param name="who"></param>
        /// <param name="message"></param>
        /// <param name="position"></param>
        internal void SayObject(string who, string message, Vector3 position)
        {
            Say(who, message, position, ObjectVoice);
        }

        /// <summary>
        /// Say something as a named person or object at a location.
        /// </summary>
        /// <param name="who">Name of person speaking</param>
        /// <param name="what">What they said</param>
        /// <param name="where">Where they were standing when they said it</param>
        /// <param name="v">The voice they use</param>
        internal void Say(
            string who,
            string what,
            Vector3 where,
            AssignedVoice v)
        {
            if (queue == null) return;

            // Create queue entry from the information.
            QueuedSpeech e = new QueuedSpeech(
                subs.FixExpressions(who),
                subs.FixExpressions(what),
                where,
                v,
                false);

            // Put that on the queue and wake up the background thread.
            lock (queue)
            {
                queue.Enqueue(e);
                Monitor.Pulse(queue);
            }
        }

        internal void Say(
            string who,
            string what,
            Vector3 where,
            AssignedVoice v,
            bool doRotate,
            BeepType beep)
        {
            if (queue == null) return;

            QueuedSpeech e = new QueuedSpeech(
                subs.FixExpressions(who),
                subs.FixExpressions(what),
                where, v, false, doRotate, beep);

            // Put that on the queue and wake up the background thread.
            lock (queue)
            {
                queue.Enqueue(e);
                Monitor.Pulse(queue);
            }
        }
        /// <summary>
        /// Queue up an action to say.
        /// </summary>
        /// <param name="who"></param>
        /// <param name="what"></param>
        /// <param name="where"></param>
        /// <param name="v"></param>
        internal void SayAction(
            string who,
            string what,
            Vector3 where,
            AssignedVoice v,
            bool spatial)
        {
            if (queue == null) return;

            QueuedSpeech e = new QueuedSpeech(
                subs.FixExpressions(who),
                subs.FixExpressions(what),
                where, v, true, spatial, BeepType.None);
            lock (queue)
            {
                queue.Enqueue(e);
                Monitor.Pulse(queue);
            }
        }
        #endregion

        #region BackgroundThread
        /// <summary>
        /// The speaking thread body.
        /// </summary>
        /// <remarks>This loops on the queue of things to say, and speaks them
        /// one at a time.</remarks>
        private void SpeakLoop()
        {
            QueuedSpeech utterance;

            try
            {
                while (running)
                {
                    // Wait for something to show up in the queue.
                    lock (queue)
                    {
                        while (queue.Count == 0)
                        {
                            Monitor.Wait(queue);
                        }

                        utterance = queue.Dequeue();
                    }

                    // Watch for the special "shutdown" message.
                    if (utterance.message == null &&
                        utterance.speaker == null)
                    {
                        running = false;
                        continue;
                    }

                    string thisfile = AUDIOFILE + string.Format("{0}", seq++) + ".wav";
                    if (seq > 4) seq = 0;

                    // Synthesize it into a file.
                    syn.Speak(utterance, thisfile);

                    // TODO Get FMOD working on Mac
                    if (System.Environment.OSVersion.Platform != PlatformID.MacOSX)
                    {
                        // Play back the file
                        control.sound.Play(
                            thisfile,              // Name of the file
                            16000,                  // Same rate
                            utterance.position,     // Location
                            true,                   // Delete the file when done
                            utterance.isSpatial);   // Whether it is inworld or moves with listener
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Synth shutdown " + e.Message);
            }
        }
        #endregion

    }

    /// <summary>
    /// Represents something waiting to be said
    /// </summary>
    public class QueuedSpeech
    {
        public string speaker = "";
        public string message;
        public AssignedVoice voice;
        internal Vector3 position;
        public bool isAction = false;
        internal bool isSpatial = false;
        public BeepType beep = BeepType.None;

        internal QueuedSpeech(string who, string what,
            Vector3 where, AssignedVoice v, bool action,
            bool doRotate, BeepType b)
        {
            speaker = who;
            message = what;
            position = where;
            voice = v;
            isAction = action;
            isSpatial = doRotate;
            beep = b;
        }

        /// <summary>
        /// Constructor for common beepless and spatialized utterances.
        /// </summary>
        /// <param name="who"></param>
        /// <param name="what"></param>
        /// <param name="where"></param>
        /// <param name="v"></param>
        /// <param name="action"></param>
        internal QueuedSpeech(string who, string what,
           Vector3 where, AssignedVoice v, bool action)
        {
            speaker = who;
            message = what;
            position = where;
            voice = v;
            isAction = action;
            isSpatial = true;
        }

    }

    public enum BeepType
    {
        None = -1,
        Confirm = 0,// A simple OK is required
        Comm,       // New IM session
        Dialog,     // Dialog box with multiple choices
        Bad,        // Something failed, an error, etc
        Good,       // Something succeeded.
        Money,      // Something about money
        Open,
        Close
    }

}
