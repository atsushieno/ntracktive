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
			if (context.CleanupExistingTracks)
				context.Edit.Tracks.Clear ();
			
			foreach (var mtrack in context.Midi.Tracks) {
				var ttrack = new TrackElement ();
				context.Edit.Tracks.Add (ttrack);
				ImportTrack (context, mtrack, ttrack);
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
					break;
				}
			}

			clip.Start = 0;
			var e = seq.Events.OfType<AbstractMidiEventElement> ().LastOrDefault ();
			if (e != null)
				clip.Length = e.B + 1.0;
		}
	}
}
