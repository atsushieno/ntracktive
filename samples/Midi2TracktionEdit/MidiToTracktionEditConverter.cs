using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using NTracktive;
using Commons.Music.Midi;
using CoreMidi;

namespace Midi2TracktionEdit
{
	public class MidiToTracktionEditConverter
	{
		// state
		bool consumed;
		MidiImportContext context;
		MidiMessage [] global_markers = new MidiMessage [] { new MidiMessage (int.MaxValue, default (MidiEvent)), };
		
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
			if (consumed)
				throw new InvalidOperationException ($"This instance is already used. Create another instance of {this.GetType ()} if you want to process more.");
			this.context = context;
			try {
				if (context.CleanupExistingTracks) {
					context.Edit.Tracks.Clear ();
					context.Edit.TempoSequence = null;
				}
				if (context.Edit.TempoSequence == null)
					context.Edit.TempoSequence = new TempoSequenceElement ();

				switch (context.MarkerImportStrategy) {
				case MarkerImportStrategy.Global:
				//case MarkerImportStrategy.Default:
					var markers = new List<MidiMessage> ();
					int t = 0;
					foreach (var m in SmfTrackMerger.Merge (context.Midi).Tracks [0].Messages) {
						t += m.DeltaTime;
						if (m.Event.EventType == MidiEvent.Meta &&
						    m.Event.MetaType == MidiMetaType.Marker)
							markers.Add (new MidiMessage (t,
								new MidiEvent (MidiEvent.Meta, 0, 0, m.Event.Data)));
					}

					global_markers = markers.ToArray ();
				Console.Error.WriteLine("GLOBAL MARKERS:" + global_markers.Length);
					break;
				}

				foreach (var mtrack in context.Midi.Tracks) {
					var trackName = PopulateTrackName (mtrack);
					var ttrack = new TrackElement () {Name = trackName};
					context.Edit.Tracks.Add (ttrack);
					ImportTrack (mtrack, ttrack);
					if (!ttrack.MidiClips.Any () && !ttrack.Clips.Any ())
						context.Edit.Tracks.Remove (ttrack);
					else {
						ttrack.Plugins.Add (new PluginElement
							{Type = "volume", Volume = 0.8, Enabled = true});
						ttrack.Plugins.Add (new PluginElement {Type = "level", Enabled = true});
						ttrack.OutputDevices = new OutputDevicesElement ();
						ttrack.OutputDevices.OutputDevices.Add (new DeviceElement
							{Name = "(default audio output)"});
					}
				}
			} finally {
				consumed = true;
			}
		}

		double ToTracktionBarSpec (double deltaTime) =>
			deltaTime / (double) context.Midi.DeltaTimeSpec;

		void ImportTrack (MidiTrack mtrack, TrackElement ttrack)
		{
			using (var globalMarkersEnumerator =
				((IEnumerable<MidiMessage>) global_markers).GetEnumerator ())
				ImportTrack (mtrack, ttrack, globalMarkersEnumerator);
		}

		void ImportTrack (MidiTrack mtrack, TrackElement ttrack, IEnumerator<MidiMessage> globalMarkersEnumerator)
		{
			double currentClipStart = 0;
			// they are explicitly assigned due to C# limitation of initialization check...
			MidiMessage nextGlobalMarker = default (MidiMessage);
			MidiClipElement clip = null; 
			SequenceElement seq = null;
			int currentTotalTime = 0;

			Action terminateClip = () => {
				if (clip == null)
					return;
				clip.PatternGenerator = new PatternGeneratorElement ();
				clip.PatternGenerator.Progression = new ProgressionElement ();
				var e = seq.Events.OfType<AbstractMidiEventElement> ().LastOrDefault ();
				if (e != null) {
					var note = e as NoteElement;
					var extend = note != null ? note.L : 0;
					clip.Length = e.B + extend;
				}
				else if (!seq.Events.Any ())
					ttrack.MidiClips.Remove (clip);				
			};
			Action proceedToNextGlobalMarker = () => {
				if (globalMarkersEnumerator.MoveNext ())
					nextGlobalMarker = globalMarkersEnumerator.Current;
				else
					nextGlobalMarker = new MidiMessage (int.MaxValue, default (MidiEvent));
			};
			
			Action nextClip = () => {
				terminateClip ();
				currentClipStart = ToTracktionBarSpec (nextGlobalMarker.DeltaTime);
				string name = nextGlobalMarker.Event.Data == null
					? null
					: Encoding.UTF8.GetString (nextGlobalMarker.Event.Data);
				clip = new MidiClipElement {Type = "midi", Speed = 1.0, Start = currentClipStart, Name = name};
				ttrack.MidiClips.Add (clip);
				seq = new SequenceElement ();
				clip.Sequence = seq;
				
				proceedToNextGlobalMarker ();
			};
			nextClip ();

			ttrack.Modifiers = new ModifiersElement ();
			int [,] noteDeltaTimes = new int [16, 128];
			NoteElement [,] notes = new NoteElement [16,128];
			int timeSigNumerator = 4, timeSigDenominator = 4;
			double currentBpm = 120.0;

			foreach (var msg in mtrack.Messages) {
				currentTotalTime += msg.DeltaTime;
				while (true) {
					if (nextGlobalMarker.DeltaTime <= currentTotalTime)
						nextClip ();
					else
						break;
				}

				var tTime = ToTracktionBarSpec (currentTotalTime) - currentClipStart;
				switch (msg.Event.EventType) {
				case MidiEvent.NoteOff:
					var noteToOff = notes [msg.Event.Channel, msg.Event.Msb];
					if (noteToOff != null) {
						var l = currentTotalTime - noteDeltaTimes [msg.Event.Channel, msg.Event.Msb];
						if (l == 0)
							Console.Error.WriteLine( ($"!!! Zero-length note: at {ToTracktionBarSpec(currentTotalTime)}, value: {msg.Event.Value}"));
						else {
							noteToOff.L = ToTracktionBarSpec (l);
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
						Console.Error.WriteLine ($"!!! Overlapped note: at {ToTracktionBarSpec(currentTotalTime)}, value: {msg.Event.Value.ToString("X08")}");
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
						case MidiMetaType.Marker:
							switch (context.MarkerImportStrategy) {
							case MarkerImportStrategy.PerTrack:
								// TODO: implement
								break;
							}
							break;
						case MidiMetaType.Tempo:
							currentBpm = ToBpm (msg.Event.Data);
							context.Edit.TempoSequence.Tempos.Add (new TempoElement {
								StartBeat = ToTracktionBarSpec (currentTotalTime),
								Curve = 1.0, Bpm = currentBpm
							});
							break;
						case MidiMetaType.TimeSignature:
							var timeSig = msg.Event.Data;
							timeSigNumerator = timeSig [0];
							timeSigDenominator = (int) Math.Pow (2, timeSig [1]);
							context.Edit.TempoSequence.TimeSignatures.Add (
								new TimeSigElement { StartBeat = ToTracktionBarSpec (currentTotalTime), Numerator= timeSigNumerator, Denominator = timeSigDenominator });
							// Tracktion engine has a problem that its tempo calculation goes fubar when timesig denomitator becomes non-4 value.
							context.Edit.TempoSequence.Tempos.Add (new TempoElement {
								StartBeat = ToTracktionBarSpec (currentTotalTime),
								Curve = 1.0, Bpm = currentBpm / (timeSigDenominator / 4)
							});
							break;
						}
					}
					break;
				}
			}

			terminateClip ();

		}

		double ToBpm (byte [] data)
		{
			var t = (data [0] << 16) + (data [1] << 8) + data [2];
			return 60000000.0 / t;
		}

		string PopulateTrackName (MidiTrack track)
		{
			var trackNameData = track.Messages.Select (m => m.Event).FirstOrDefault (e =>
				e.EventType == MidiEvent.Meta && e.MetaType == MidiMetaType.TrackName).Data;
			var trackName = trackNameData != null ? Encoding.UTF8.GetString (trackNameData) : null;
			var progChgs = track.Messages.Select (m => m.Event)
				.Where (e => e.EventType == MidiEvent.Program).ToArray ();
			int firstProgramChangeValue = progChgs.Length > 0 ? progChgs [0].Msb : -1;
			trackName = (0 <= firstProgramChangeValue && firstProgramChangeValue < GeneralMidi.InstrumentNames.Length) ? GeneralMidi.InstrumentNames [firstProgramChangeValue] : null;
			return trackName;
		}
	}
}
