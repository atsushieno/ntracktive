using System;
using System.IO;
using System.Xml;
using Commons.Music.Midi;
using NTracktive;

namespace Midi2TracktionEdit
{
	public enum MarkerImportStrategy
	{
		Default,
		None,
		Global,
		PerTrack,
	}
		
	public class MidiImportContext
	{
		public MidiImportContext (CommandArgumentContext commandArgumentContext)
		{
			Midi = LoadSmf (commandArgumentContext.MidiFile);
			Edit = LoadEdit (commandArgumentContext.TracktionEditTemplateFile);
		}

		public MidiImportContext (MidiMusic midi, EditElement edit)
		{
			Midi = midi;
			Edit = edit;
		}

		public bool CleanupExistingTracks { get; set; } = true;
		
		public MarkerImportStrategy MarkerImportStrategy { get; set; }

		MidiMusic midi;
		EditElement edit;

		public MidiMusic Midi {
			get => midi;
			set {
				if (midi != null)
					throw new InvalidOperationException ("MIDI song is already set to the read-only context");
				midi = value;
			}
		}
		
		public EditElement Edit {
			get => edit;
			set {
				if (edit != null)
					throw new InvalidOperationException ("Edit is already set to the read-only context");
				edit = value;
			}
		}

		EditElement LoadEdit (string editFile)
		{
			using (var reader = new XmlTextReader (editFile) { Namespaces = false })
				return new EditModelReader ().Read (reader);
		}

		MidiMusic LoadSmf (string midiFile)
		{
			using (var stream = File.OpenRead (midiFile))
				return MidiMusic.Read (stream);
		}
	}
}
