using System;
namespace NTracktive
{
	public class EditModelTemplate
	{
		public const string GlobalMediaPart = "0/";

		static readonly Random rnd = new Random ();

		static string NewHash ()
		{
			return (rnd.Next () % 0x10000000).ToString ("x07");
		}

		public static EditElement CreateNewEmptyEdit ()
		{
			int newIdFrom = 1001;
			
			string projectIdPart = "" + (rnd.Next () % 1000000).ToString ("D06") + '/';
			string mediaFilePart = "" + (rnd.Next () % 1000000).ToString ("D06") + '/';
			return new EditElement {
				AppVersion = "Waveform 10.0.26",
				ProjectID = projectIdPart + NewHash (),
				CreationTime = (long)(DateTime.Now - new DateTime (1970, 1, 1)).TotalSeconds,
				Transport = new TransportElement (),
				MacroParameters = new MacroParametersElement { Id = newIdFrom++.ToString () },
				TempoSequence = new TempoSequenceElement {
					TimeSignatures = {
						new TimeSigElement { Numerator = 4, Denominator = 4, StartBeat = 0 }
					},
					Tempos = {
						new TempoElement { StartBeat = 0.0, Bpm = 120 },
					}
				},
				PitchSequence = new PitchSequenceElement {
					 Pitches = { new PitchElement { StartBeat = 0.0, Pitch = 60 } }
				},
				Video = new VideoElement (),
				AutoMapXml = new AutoMapXmlElement (),
				ClickTrack = new ClickTrackElement { Level = 0.60 },
				Id3VorbisMetadata = new Id3VorbisMetadataElement { TrackNumber =  1, Date = DateTime.Now.ToString("yyyy") },
				MasterVolume = new MasterVolumeElement {
					 Plugins = {
						new PluginElement () {
							Type = "volume",
							Id = newIdFrom++.ToString (),
							Enabled = true,
							Volume = 0.666,
							MacroParameters = new MacroParametersElement () {
								Id = newIdFrom++.ToString (),
							},
			    				ModifierAssignments = new ModifierAssignmentsElement (),
						},
					 },
				},
				Racks = new RacksElement (),
				MasterPlugins = new MasterPluginsElement (),
				AuxBusNames = new AuxBusNamesElement (),
				InputDevices = new InputDevicesElement (),
				TrackComps = new TrackCompsElement (),
				/*
				ControllerMappings = new ControllerMappingsElement (),
				MidiViewState = new MidiViewStateElement {
					RightTime = 20.0
				},
				ArrangeView = new ArrangeViewElement {
					MixerViewState = new MixerViewStateElement {
						ModifiersVisible = true,
						PluginsVisible = true,
					},
				},
				*/
				TempoTrack = new TempoTrackElement {
					Name = "Global",
					Id = newIdFrom++.ToString (),
		     			MacroParameters = new MacroParametersElement {
		     				Id = newIdFrom++.ToString (),
		     			},
					Modifiers = new ModifiersElement (),
				},
				MarkerTrack = new MarkerTrackElement {
					Id = newIdFrom++.ToString (),
					MacroParameters = new MacroParametersElement {
						Id = newIdFrom++.ToString (),
					},
					Modifiers = new ModifiersElement (),
				},
				ChordTrack = new ChordTrackElement {
					Name = "Chords",
					Id = newIdFrom++.ToString (),
					},
				Tracks = {
					new TrackElement () {
						MidiClips = {
							new MidiClipElement () {
								Length = 8,
								Id = newIdFrom++.ToString (),
								Sequence = new SequenceElement () {
									Events = {
										new NoteElement () {
											P = 60,
											V = 100,
											B = 1.0,
											L = 0.25,
										}
									},
								},
								Name = "MIDI Clip 1",
								Offset = 0,
								Type = "midi",
								Colour = "#00008000",
							},
						},
						Colour = "#00008000",
						Height = 60,
						MacroParameters = new MacroParametersElement (),
						Id = newIdFrom++.ToString (),
						Modifiers = new ModifiersElement (),
						Name = "track 1",
						Plugins = {
							new PluginElement {
								Type = "volume",
								Id = newIdFrom++.ToString (),
								Enabled = true,
								Volume = 0.666,
								MacroParameters = new MacroParametersElement {
									MediaId = mediaFilePart + NewHash (),
								}
							},

							new PluginElement {
								Type = "level",
								Id = newIdFrom++.ToString (),
								Enabled = true,
								Volume = 0.666,
								MacroParameters = new MacroParametersElement {
									MediaId = mediaFilePart + NewHash (),
								}
							},
						},
						OutputDevices = new OutputDevicesElement {
							OutputDevices = {
								new DeviceElement {
									Name = "(default audio output)"
								}
							}
						},
					},
				},
			};
		}
	}
}
