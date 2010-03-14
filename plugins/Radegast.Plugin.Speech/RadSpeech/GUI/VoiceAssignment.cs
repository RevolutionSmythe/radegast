﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using RadegastSpeech.Talk;
using OpenMetaverse;

namespace RadegastSpeech.GUI
{
    public partial class VoiceAssignment : Form
    {
        private PluginControl control;
        private Dictionary<string, AvailableVoice> library;
        private string voiceName;
        private AvailableVoice voice;
        private string avatarName;
        private UUID avatarId;
        private int rateModification;
        private int pitchModification;
        private bool doDemo = false;

        public VoiceAssignment( PluginControl pc, string name, UUID id )
        {
            control = pc;
            avatarName = name;
            avatarId = id;
            InitializeComponent();

            control.talker.SayMore("Assign a voice for " + avatarName, BeepType.Open);

            // Inhibit demonstrations until constructore is done.
            doDemo = false;

            // Populate the fixed fields
            avName.Text = avatarName;
            library = control.talker.voices.voiceLibrary;
            foreach (string vname in library.Keys)
            {
                AvailableVoice v = library[vname];
                voiceList.Items.Add(v.Name);
            }

            // Set the current name, if any.
            voiceList.ClearSelected();
            voiceName = null;

            AssignedVoice av = null;

            // First check the assigned voices.
            av = control.talker.voices.VoiceFor( id, false );

            // A voice has been assigned, so indicate that.
            if (av != null)
            {
                voiceName = av.root.Name;
                voice = library[voiceName];
                rateSelector.SelectedIndex = av.rateModification + 1;
                pitchSelector.SelectedIndex = av.pitchModification + 1;
                SelectByName(voiceName);
            }

            doDemo = true;
        }

        /// <summary>
        /// Mark a voice as selected by its name
        /// </summary>
        /// <param name="name"></param>
        private void SelectByName(string name)
        {
            for (int n = 0; n < voiceList.Items.Count; n++)
            {
                if ((string)voiceList.Items[n] == name)
                {
                    voiceList.SetSelected(n, true);
                    return;
                }
            }
            voiceList.ClearSelected();
        }

        /// <summary>
        /// Demonstrate the selected voice.
        /// </summary>
        private void Demonstrate()
        {
            if (!doDemo) return;

            // Construct a sample statement.
            string sample = "This is " + voiceName.Replace("_", " ");
            switch (rateModification)
            {
                case 00: break;
                case -1: sample += " slow"; break;
                case +1: sample += " fast"; break;
            }
            switch (pitchModification)
            {
                case 00: break;
                case -1: sample += " low"; break;
                case +1: sample += " high"; break;
            }
            sample += ".";

            this.BeginInvoke(new MethodInvoker(delegate()
            {
                control.talker.Say(null,
                    sample,
                    new Vector3(3.0f, 0.0f, 0.0f),
                    MakeAssignedVoice(),
                    false,
                    BeepType.None);
            }
            ));
        }

        /// <summary>
        /// Respond to change in selected voice.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void voiceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (voiceList.SelectedItem == null) return;

            voiceName = (string)voiceList.SelectedItem;
            voice = library[voiceName];
            if (voice.Male)
                gender.Text = "M";
            else
                gender.Text = "F";

            // Reset the modifiers.
            rateModification = 0;
            pitchModification = 0;
            rateSelector.SelectedIndex = rateModification + 1;
            pitchSelector.SelectedIndex = pitchModification + 1;

            Demonstrate();
        }

        private AssignedVoice MakeAssignedVoice()
        {
            return new AssignedVoice( voice, rateModification, pitchModification );
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            if (voiceList.SelectedItem != null)
            {
                control.talker.voices.AssignVoice(MakeAssignedVoice(), avatarName, avatarId );
            }
            control.talker.SayMore("Voice assigned.", BeepType.Good);
            Close();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            control.talker.SayMore("Assignment canceled.", BeepType.Close);
            Close();
        }

        private void pitchSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            pitchModification = pitchSelector.SelectedIndex - 1;
            Demonstrate();
        }

        private void rateSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            rateModification = rateSelector.SelectedIndex - 1;
            Demonstrate();
        }

    }
}
