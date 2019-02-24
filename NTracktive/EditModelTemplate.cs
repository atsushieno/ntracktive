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
			string projectIdPart = "" + (rnd.Next () % 1000000).ToString ("D06") + '/';
			string mediaFilePart = "" + (rnd.Next () % 1000000).ToString ("D06") + '/';
			return new EditElement {
				AppVersion = "Waveform 10.0.26",
				ProjectID = projectIdPart + NewHash (),
				CreationTime = (long)(DateTime.Now - new DateTime (1970, 1, 1)).TotalSeconds,
				Transport = new TransportElement (),
				MacroParameters = new MacroParametersElement { Id = "1001" },
				TempoSequence = new TempoSequenceElement {
					TimeSignatures = {
						new TimeSigElement { Numerator = 4, Denominator = 4, StartBeat = 0 }
					},
					Tempos = {
						new TempoElement { StartBeat = 0.0, Bpm = 120 },
					}
				},
				PitchSequence = new PitchSequenceElement {},
				AutoMapXml = new AutoMapXmlElement (),
				ClickTrack = new ClickTrackElement { Level = 0.60 },
				Id3VorbisMetadata = new Id3VorbisMetadataElement { TrackNumber =  1, Date = DateTime.Now.ToString("yyyy") },
				MasterVolume = new MasterVolumeElement {
					 Filter = {
						new FilterElement () {
							Type = "volume",
							Id = "0/" + NewHash (),
							Enabled = true,
							Volume = 0.666,
							MacroParameters = new MacroParametersElement () {
								MediaId = mediaFilePart + NewHash ()
							},
			    				ModifierAssignments = new ModifierAssignmentsElement (),
						},
					 },
				},
				RackFilters = new RackFiltersElement (),
				MasterFilters = new MasterFiltersElement (),
				AuxBusNames = new AuxBusNamesElement (),
				DevicesEx = new DevicesExElement (),
				TrackComps = new TrackCompsElement (),
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
				TempoTrack = new TempoTrackElement {
					Name = "Global",
					MediaId = mediaFilePart + NewHash (),
		     			MacroParameters = new MacroParametersElement {
		     				MediaId = mediaFilePart + NewHash ()
		     			},
					Modifiers = new ModifiersElement (),
				},
				ModifiedBy = Environment.UserName,
				ChordTrack = new ChordTrackElement { 
					 MediaId = mediaFilePart + NewHash (),
					},
				MarkerTracks = { },
				Tracks = {
					new TrackElement () {
						Clips = {
							new ClipElement () {
								Length = 8,
								MediaId = mediaFilePart + NewHash (),
								MidiSequence = new MidiSequenceElement () {
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
						MediaId = mediaFilePart + NewHash (),
						Modifiers = new ModifiersElement (),
						Name = "track 1",
						Filters = {
							new FilterElement {
								Type = "volume",
								Id = GlobalMediaPart + NewHash (),
								Enabled = true,
								Volume = 0.666,
								MacroParameters = new MacroParametersElement {
									MediaId = mediaFilePart + NewHash (),
								}
							},

							new FilterElement {
								Type = "level",
								Id = GlobalMediaPart + NewHash (),
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
