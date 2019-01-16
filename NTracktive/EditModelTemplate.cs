using System;
namespace NTracktive
{
	public class EditModelTemplate
	{
		static readonly Random rnd = new Random ();

		static string NewHash ()
		{
			return (rnd.Next () % 0x10000000).ToString ("x07");
		}

		public static EditElement CreateNewEmptyEdit ()
		{
			string mediaFilePart = "" + (rnd.Next () % 1000000).ToString ("D06") + '/';
			return new EditElement {
				AppVersion = "Waveform 9.3.2",
				CreationTime = (long)(DateTime.Now - new DateTime (1970, 1, 1)).TotalSeconds,
				LastSignificantChange = "167803aece1", // some random string
				MacroParameters = new MacroParametersElement { MediaId = mediaFilePart + NewHash () },
				TempoSequence = new TempoSequenceElement {
					TimeSignatures = {
						new TimeSigElement { Numerator = 4, Denominator = 4, StartBeat = 0 }
					},
					Tempos = {
						new TempoElement { StartBeat = 0.0, Bpm = 120 },
					}
				},
				PitchSequence = new PitchSequenceElement {},
				ViewState = new ViewStateElement {
					HiddenClips = "",
					LockedClips = "",
					CurrentSidePanel = "Tracks",
					MidiEditorShown = true,
					EndToEnd = true,
					SidePanelsShown = true,
					MixerPanelShown = false,
					FacePlateView = new FacePlateViewElement (),
				},
				AutoMapXml = new AutoMapXmlElement (),
				ClickTrack = new ClickTrackElement (),
				Id3VorbisMetadata = new Id3VorbisMetadataElement (),
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
				MediaId = mediaFilePart + NewHash (),
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
							},
						},
						Colour = "#00008000",
						Filters = {
						},
						Height = 60,
						MacroParameters = new MacroParametersElement (),
						MediaId = mediaFilePart + NewHash (),
						Modifiers = new ModifiersElement (),
						Name = "track 1",
					},
				},
			};
		}
	}
}
