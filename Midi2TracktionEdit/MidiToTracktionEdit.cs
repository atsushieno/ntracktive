using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using NTracktive;
using Commons.Music.Midi;
using CoreMidi;

namespace Midi2TracktionEdit
{
	public class MidiToTracktionEdit
	{
		public void Process (string [] args)
		{
			var argumentContext = GetContextFromCommandArguments (args);
			var ic = argumentContext.CreateImportContext ();
			ImportMusic (ic);
			new EditModelWriter ().Write (Console.Out, ic.Edit);
		}

		CommandArgumentContext GetContextFromCommandArguments (string [] args)
		{
			var context = new CommandArgumentContext ();
			context.MidiFile = args.FirstOrDefault ();
			context.TracktionEditTemplateFile = args.Skip (1).FirstOrDefault ();
			return context;
		}

		public void ImportMusic (MidiImportContext context)
		{
			if (context.CleanupExistingTracks) {
				context.Edit.Tracks.Clear ();
				context.Edit.TempoSequence = null;
			}
			if (context.Edit.TempoSequence == null)
				context.Edit.TempoSequence = new TempoSequenceElement ();

			foreach (var mtrack in context.Midi.Tracks) {
				var ttrack = new TrackElement ();
				context.Edit.Tracks.Add (ttrack);
				ImportTrack (context, mtrack, ttrack);				
				if (!ttrack.MidiClips.Any () && !ttrack.Clips.Any())
					context.Edit.Tracks.Remove (ttrack);
				else {
					ttrack.Plugins.Add (new PluginElement { Type = "volume", Volume = 0.8, Enabled = true });
					ttrack.Plugins.Add (new PluginElement { Type = "level", Enabled = true });
					ttrack.OutputDevices = new OutputDevicesElement ();
					ttrack.OutputDevices.OutputDevices.Add (new DeviceElement { Name = "(default audio output)" });
				}
			}
		}

		double ToTracktionBarSpec (MidiImportContext context, double deltaTime) =>
			deltaTime / (double) context.Midi.DeltaTimeSpec;

		void ImportTrack (MidiImportContext context, MidiTrack mtrack, TrackElement ttrack)
		{
			ttrack.Modifiers = new ModifiersElement ();
			var clip = new MidiClipElement {Type = "midi", Speed = 1.0 };
			ttrack.MidiClips.Add (clip);
			var seq = new SequenceElement ();
			clip.Sequence = seq;
			int currentTotalTime = 0;
			int [,] noteDeltaTimes = new int [16, 128];
			NoteElement [,] notes = new NoteElement [16,128];
			int timeSigNumerator = 4, timeSigDenominator = 4;
			
			foreach (var msg in mtrack.Messages) {
				currentTotalTime += msg.DeltaTime;
				var tTime = ToTracktionBarSpec (context, currentTotalTime);
				switch (msg.Event.EventType) {
				case MidiEvent.NoteOff:
					var noteToOff = notes [msg.Event.Channel, msg.Event.Msb];
					if (noteToOff != null) {
						var l = currentTotalTime - noteDeltaTimes [msg.Event.Channel, msg.Event.Msb];
						if (l == 0)
							Console.Error.WriteLine( ($"!!! Zero-length note: at {ToTracktionBarSpec(context, currentTotalTime)}, value: {msg.Event.Value}"));
						else {
							noteToOff.L = ToTracktionBarSpec (context, l);
							noteToOff.C = msg.Event.Lsb;
						}
					}
					notes [msg.Event.Channel, msg.Event.Msb] = null;
					noteDeltaTimes [msg.Event.Channel, msg.Event.Msb] = 0;
					break;
				case MidiEvent.NoteOn:
					if (msg.Event.Lsb == 0)
						goto case MidiEvent.NoteOff;
					var noteOn = new NoteElement {B = tTime, P = msg.Event.Msb, V = msg.Event.Lsb};
					if (notes [msg.Event.Channel, msg.Event.Msb] != null)
						Console.Error.WriteLine ($"!!! Overlapped note: at {ToTracktionBarSpec(context, currentTotalTime)}, value: {msg.Event.Value.ToString("X08")}");
					notes [msg.Event.Channel, msg.Event.Msb] = noteOn;
					noteDeltaTimes [msg.Event.Channel, msg.Event.Msb] = currentTotalTime;
					seq.Events.Add (noteOn);
					break;
				case MidiEvent.CAf:
					seq.Events.Add (new ControlElement () { B = tTime, Type = ControlType.CAf, Val = msg.Event.Lsb * 128 });
					break;
				case MidiEvent.CC:
					seq.Events.Add (new ControlElement () { B = tTime, Type = msg.Event.Msb, Val = msg.Event.Lsb * 128 });
					break;
				case MidiEvent.Program:
					seq.Events.Add (new ControlElement () { B = tTime, Type = ControlType.ProgramChange, Val = msg.Event.Msb * 128 }); // lol
					break;
				case MidiEvent.PAf:
					seq.Events.Add (new ControlElement () { B = tTime, Type = ControlType.PAf, Val = msg.Event.Lsb * 128, Metadata = msg.Event.Msb });
					break;
				case MidiEvent.Pitch:
					seq.Events.Add (new ControlElement () { B = tTime, Type = ControlType.PitchBend, Val = msg.Event.Msb * 128 + msg.Event.Lsb });
					break;
				default: // sysex or meta
					if (msg.Event.EventType == MidiEvent.Meta) {
						switch (msg.Event.MetaType) {
						//case MidiMetaType.Marker:
						case MidiMetaType.Tempo:
							context.Edit.TempoSequence.Tempos.Add (new TempoElement {
								StartBeat = ToTracktionBarSpec (context,
									currentTotalTime),
								Curve = 1.0, Bpm = ToBpm (msg.Event.Data)
							});
							break;
						case MidiMetaType.TimeSignature:
							var timeSig = msg.Event.Data;
							timeSigNumerator = timeSig [0];
							timeSigDenominator = (int) Math.Pow (2, timeSig [1]);
							context.Edit.TempoSequence.TimeSignatures.Add (
								new TimeSigElement { Numerator= timeSigNumerator, Denominator = timeSigDenominator });
							break;
						}
					}
					break;
				}
			}

			clip.Start = 0;
			var e = seq.Events.OfType<AbstractMidiEventElement> ().LastOrDefault ();
			if (e != null)
				clip.Length = e.B;
			else if (!seq.Events.Any ())
				ttrack.MidiClips.Remove (clip);
		}

		double ToBpm (byte [] data)
		{
			var t = (data [0] << 14) + (data [1] << 7) + data [2];
			return t / 1000.0;
		}
	}
}
