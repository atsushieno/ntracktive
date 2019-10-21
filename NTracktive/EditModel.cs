using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


/*
 * Naming rules:
 * 
 * - Elements end with "Element".
 *   - It is to differentiate type name and member property name to avoid possible conflict e.g. <PITCH pitch="..." />
 * - Simple type properties are serialized as attributes.
 * - All those elements are UPPERCASED, and attributes are camelCased,
 *   with an exception that '_' indicates namespace prefix (... sort of) splitter.
 * 
 */   
namespace NTracktive
{
	public enum DataType
	{
		Unknown,
		String,
		UnixTime,
		Id,
		Length,
		Number,
		Integer,
		BooleanInt,
		Color,
		HexBinary,
		Base64Binary
	}

	public static class ControlType
	{
		public const int ProgramChange = 0x1000 + 1;
		public const int PAf = 0x1000 + 4;
		public const int PitchBend = 0x1000 + 5;
		public const int CAf = 0x1000 + 7;
	}

	public class DataTypeAttribute : Attribute
	{
		public DataTypeAttribute (DataType type)
		{
			DataType = type;
		}

		public DataType DataType { get; set; }
	}


	public class EditElement
	{
		// attributes
		public string ProjectID { get; set; }
		public string AppVersion { get; set; }
		[DataType (DataType.UnixTime)]
		public long CreationTime { get; set; }
		public string ModifiedBy { get; set; }
		[DataType (DataType.Id)]
		public string MediaId { get; set; }
		public string LastSignificantChange { get; set; }

		// elements
		public TransportElement Transport { get; set; }
		public MacroParametersElement MacroParameters { get; set; }
		public TempoSequenceElement TempoSequence { get; set; }
		public PitchSequenceElement PitchSequence { get; set; }
		public VideoElement Video { get; set; }
		public ViewStateElement ViewState { get; set; }
		public AutoMapXmlElement AutoMapXml { get; set; }
		public ClickTrackElement ClickTrack { get; set; }
		public Id3VorbisMetadataElement Id3VorbisMetadata { get; set; }
		public MasterVolumeElement MasterVolume { get; set; }
		// new
		public RacksElement Racks { get; set; }
		// old
		public RackFiltersElement RackFilters { get; set; }
		// new
		public MasterPluginsElement MasterPlugins { get; set; }
		// old
		public MasterFiltersElement MasterFilters { get; set; }
		public AuxBusNamesElement AuxBusNames { get; set; }
		// new
		public InputDevicesElement InputDevices { get; set; }
		// old
		public DevicesExElement DevicesEx { get; set; }
		public TrackCompsElement TrackComps { get; set; }
		public AraDocumentElement AraDocument { get; set; }
		public ControllerMappingsElement ControllerMappings { get; set; }
		public EditMixGroupsElement EditMixGroups { get; set; }
		public AudioEditingElement AudioEditing { get; set; }
		public MidiViewStateElement MidiViewState { get; set; }
		public ArrangeViewElement ArrangeView { get; set; }

		public IList<AbstractTrackElement> Tracks { get; private set; } = new List<AbstractTrackElement> ();
	}


	public class TransportElement
	{
		public double? Position { get; set; }
		public double? ScrubInterval { get; set; }
		public double? LoopPoint1 { get; set; }
		public double? LoopPoint2 { get; set; }
	}


	public class MacroParametersElement
	{
		public string Id { get; set; } // new
		[DataType (DataType.Id)]
		public string MediaId { get; set; } // old
	}


	public class TempoSequenceElement
	{
		public IList<TempoElement> Tempos { get; private set; } = new List<TempoElement> ();
		public IList<TimeSigElement> TimeSignatures { get; private set; } = new List<TimeSigElement> ();
	}


	public class TempoElement
	{
		[DataType (DataType.Length)]
		public double StartBeat { get; set; }
		public double Bpm { get; set; }
		public double Curve { get; set; }
	}


	public class TimeSigElement
	{
		public int Numerator { get; set; }
		public int Denominator { get; set; }
		[DataType (DataType.Length)]
		public double StartBeat { get; set; }
	}


	public class PitchSequenceElement
	{
		public IList<PitchElement> Pitches { get; private set; } = new List<PitchElement> ();
	}


	public class PitchElement
	{
		// new
		[DataType (DataType.Length)]
		public double StartBeat { get; set; }
		// old
		[DataType (DataType.Length)]
		public double Start { get; set; }
		public double Pitch { get; set; }
	}


	public class VideoElement
	{
	}


	public class ViewStateElement
	{
		// attributes
		public bool MinimalTransportBar { get; set; }
		public bool ScrollWhenPlaying { get; set; }
		public string HiddenClips { get; set; }
		public string LockedClips { get; set; }
		public string EnabledTrackTags { get; set; }
		public string DisabledSearchLibraries { get; set; }
		public string CurrentSidePanel { get; set; }
		[DataType (DataType.Length)]
		public double MarkIn { get; set; }
		[DataType (DataType.Length)]
		public double MarkOut { get; set; }
		public double Tracktop { get; set; }
		[DataType (DataType.Length)]
		public double Cursorpos { get; set; }
		[DataType (DataType.Length)]
		public double Viewleft { get; set; }
		[DataType (DataType.Length)]
		public double Viewright { get; set; }
		public bool MidiEditorShown { get; set; }
		public bool EndToEnd { get; set; }
		public bool SidePanelsShown { get; set; }
		public bool MixerPanelShown { get; set; }
		public double MidiEditorHeight { get; set; }
		// elements
		public FacePlateViewElement FacePlateView { get; set; }
		public TrackEditorsElement TrackEditors { get; set; }
	}


	public class TrackEditorsElement
	{
	}


	public class FacePlateViewElement
	{
		public bool? EditModeActive { get; set; }
	}


	public class AutoMapXmlElement
	{
	}


	public class ClickTrackElement
	{
		public double Level { get; set; }
	}


	public class Id3VorbisMetadataElement
	{
		public double TrackNumber { get; set; }
		[DataType (DataType.UnixTime)]
		public string Date { get; set; }
	}


	public class MasterVolumeElement
	{
		// new
		public IList<PluginElement> Plugins { get; private set; } = new List<PluginElement> ();
		// old
		public IList<FilterElement> Filters { get; private set; } = new List<FilterElement> ();
	}


	public class RackElementBase
	{
	}
	

	public class RackFiltersElement : RackElementBase
	{
	}


	public class RacksElement : RackElementBase
	{
	}

	public class MasterPluginsElementBase
	{
	}


	public class MasterFiltersElement : MasterPluginsElementBase
	{
	}


	public class MasterPluginsElement : MasterPluginsElementBase
	{
	}


	public class AuxBusNamesElement
	{
	}


	public class InputDevicesElementBase
	{
		public IList<InputDeviceElement> InputDevices { get; private set; } = new List<InputDeviceElement> ();
	}

	// new

	public class InputDevicesElement : InputDevicesElementBase
	{		
	}

	// old
	// I hope it is not the other casing...

	public class DevicesExElement : InputDevicesElementBase
	{
	}


	public class InputDeviceElement
	{
		public string Name { get; set; }
		[DataType (DataType.Id)]
		public string TargetTrack { get; set; }
		public int TargetIndex { get; set; }
	}


	public class TrackCompsElement
	{
	}


	public class AraDocumentElement
	{
	}


	public class ControllerMappingsElement
	{
	}


	public abstract class AbstractViewElement
	{
		public double? Width { get; set; }
		public double? Height { get; set; }
		public double? VerticalOffset { get; set; }
		public double? VisibleProportion { get; set; }
	}


	public abstract class AbstractTrackElement : AbstractViewElement
	{
		public string Name { get; set; }
		// new
		public string Id { get; set; }
		// old
		[DataType (DataType.Id)]
		public string MediaId { get; set; }

		public MacroParametersElement MacroParameters { get; set; }
		public ModifiersElement Modifiers { get; set; }
	}

	public abstract class AbstractContentTrackElement : AbstractTrackElement
	{
		public IList<AutomationTrackElement> AutomationTracks { get; private set; } = new List<AutomationTrackElement> ();
		// new
		public IList<PluginElement> Plugins { get; private set; } = new List<PluginElement> ();
		// old
		public IList<FilterElement> Filters { get; private set; } = new List<FilterElement> ();		
	}

	public class AutomationTrackElement : AbstractTrackElement
	{
		public string Colour { get; set; }
		public int? CurrentAutoParamPluginID { get; set; }
		public int? CurrentAutoParamTag { get; set; }
	}

	// used by both folder track and submix track.
	public class FolderTrackElement : AbstractContentTrackElement
	{
		public bool? Expanded { get; set; }
		
		public IList<AbstractTrackElement> Tracks { get; set; } = new List<AbstractTrackElement> ();
	}


	public class TempoTrackElement : AbstractTrackElement
	{
	}


	public class ModifiersElement
	{
		public IList<AbstractModifierElement> Modifiers { get; set; } = new List<AbstractModifierElement> ();
	}
	
	// These elements are used both as definitions and as uses...
	public abstract class AbstractModifierElement
	{
		// definitions
		public string Id { get; set; }
		public bool RemapOnTempoChange { get; set; }
		public string Colour { get; set; }
		public string Base64_Parameters { get; set; }

		// uses
		public int? Source { get; set; }
		public string ParamID { get; set; }
		public double? Value { get; set; }
		
		// definitions
		public ModifierAssignmentsElement ModifierAssignments { get; set; }
	}

	public class LFOElement : AbstractModifierElement
	{
		public double? Rate { get; set; }
		public double? RateType { get; set; }
		public double? SyncType { get; set; }
		public double? Wave { get; set; }
	}

	public class StepElement : AbstractModifierElement
	{
		// definitions
		public double? SyncType { get; set; }
		public double? NumSteps { get; set; }
	}

	public class EnvelopeFollowerElement : AbstractModifierElement
	{
		// definitions
		public double? Enabled { get; set; } // looks like every parameter is number-based
		public double? GainDb { get; set; }
		public double? Attack { get; set; }
		public double? Hold { get; set; }
		public double? Release { get; set; }
	}

	public class RandomElement : AbstractModifierElement
	{
		public double? Type { get; set; }
		public double? Shape { get; set; }
		public double? SyncType { get; set; }
		public double? Rate { get; set; }
	}

	public class MidiTrackerElement : AbstractModifierElement
	{
		public NodesElement Nodes { get; set; }
	}
	
	public class NodesElement
	{
		public IList<NodeElement> Nodes { get; set; } = new List<NodeElement> ();
	}

	public class NodeElement
	{
		public int Midi { get; set; }
		public double Value { get; set; }
	}


	public class MarkerTrackElement : AbstractTrackElement
	{
		public int TrackType { get; set; }
	}


	public class PluginElementBase
	{
		// attributes
		public string Type { get; set; }
		[DataType (DataType.Id)]
		public string Uid { get; set; }
		public string Filename { get; set; }
		public string Name { get; set; }
		public string Manufacturer { get; set; }
		[DataType (DataType.Id)]
		public string Id { get; set; }
		public bool Enabled { get; set; }
		public int ProgramNum { get; set; }
		[DataType (DataType.Base64Binary)]
		public string State { get; set; }
		[DataType (DataType.Base64Binary)]
		public string Base64_Layout { get; set; }
		public double Volume { get; set; }
		public double? WindowX { get; set; }
		public double? WindowY { get; set; }
		public bool? WindowLocked { get; set; }
		public bool? RemapOnTempoChange { get; set; }
		public double? Dry { get; set; }
		[DataType (DataType.Base64Binary)]
		public string Base64_Parameters { get; set; }

		// elements
		public MacroParametersElement MacroParameters { get; set; }
		public ModifierAssignmentsElement ModifierAssignments { get; set; }
		public FacePlateElement FacePlate { get; set; }
	}


	public class FilterElement : PluginElementBase
	{
	}


	public class PluginElement : PluginElementBase
	{
	}


	public class ModifierAssignmentsElement
	{
		public IList<AbstractModifierElement> Modifiers { get; set; } = new List<AbstractModifierElement> ();
	}


	public class FacePlateElement : AbstractViewElement
	{
		public bool AutoSize { get; set; }
		public bool? AssignEnabled { get; set; }

		public BackgroundElement Background { get; set; }
		public IList<FacePlateContentBase> Contents { get; set; } = new List<FacePlateContentBase> ();
	}

	public abstract class FacePlateContentBase
	{
		// HACK: it should be IList<int>
		public string Bounds { get; set; }
		// HACK: it should be IList<int>
		public string ParameterIDs { get; set; }
	}

	public class BackgroundElement
	{
		public double ImageAlpha { get; set; }
	}

	public class ParameterElement : FacePlateContentBase
	{
	}

	public class ButtonElement : FacePlateContentBase
	{
	}

	public class XYElement : FacePlateContentBase
	{
	}


	public class ChordTrackElement : AbstractTrackElement
	{
	}


	public class TrackElement : AbstractContentTrackElement
	{
		public double? MidiVProp { get; set; }
		public double? MidiVOffset { get; set; }
		public string Colour { get; set; }
		public bool? Solo { get; set; }
		public bool? Mute { get; set; }

		public IList<ClipElementBase> Clips { get; private set; } = new List<ClipElementBase> ();
		public OutputDevicesElement OutputDevices { get; set; }
		public TrackSnapshotsElement TrackSnapshots { get; set; }
	}

	public class TrackSnapshotsElement
	{
	}

	// old

	public class ClipElement : MidiClipElementBase
	{
		public MidiSequenceElement MidiSequence { get; set; }
	}

	// new

	public class MidiClipElement : MidiClipElementBase
	{
		public SequenceElement Sequence { get; set; }
	}


	public abstract class MidiClipElementBase : ClipElementBase
	{
		public string Type { get; set; }
		public double? Sync { get; set; }
		public bool ShowingTakes { get; set; }
		public bool MpeMode { get; set; }
		public double VolDb { get; set; }
		public double OriginalLength { get; set; }
		public bool SendProgramChange { get; set; }
		public bool SendBankChange { get; set; }
	}


	public abstract class ClipElementBase
	{
		// attributes
		public int Channel { get; set; }
		public string Name { get; set; }
		[DataType (DataType.Length)]
		public double Start { get; set; }
		[DataType (DataType.Length)]
		public double Length { get; set; }
		[DataType (DataType.Length)]
		public double Offset { get; set; }
		[DataType (DataType.Id)]
		public string Source { get; set; }
		// new
		public string Id { get; set; }
		// old
		[DataType (DataType.Id)]
		public string MediaId { get; set; }
		public string Colour { get; set; }
		public int CurrentTake { get; set; }
		public double Speed { get; set; }
		public bool Mute { get; set; }
		public double? LinkID { get; set; }
		public double? LoopStartBeats { get; set; }
		public double? LoopLengthBeats { get; set; }

		// elements
		public QuantisationElement Quantisation { get; set; }
		public GrooveElement Groove { get; set; }
		public PatternGeneratorElement PatternGenerator { get; set; }
	}

	public class StepClipElement : ClipElementBase
	{
		public double Sequence { get; set; }
		
		public ChannelsElement Channels { get; set; }
		public PatternsElement Patterns { get; set; }
	}

	public class ChannelsElement
	{
		public IList<ChannelElement> Channels { get; set; } = new List<ChannelElement> ();
	}

	// HACK: <CHANNEL> can appear in both <CHANNELS> and <PATTERN>.
	// This library does not provide "decent" way to distinguish serialization names, so define both members in this type.
	public class ChannelElement
	{
		// for ChannelsElement
		public int Channel { get; set; }
		public int Note { get; set; }
		public int Velocity { get; set; }
		public string Name { get; set; }
		// for PatternElement
		// HACK: they should all be arrays.
		public string Pattern { get; set; } // "1000101010001000"...
		public string Velocities { get; set; }
		public string Gates { get; set; }
	}

	public class PatternsElement
	{
		public IList<PatternElement> Patterns { get; set; } = new List<PatternElement> ();
	}

	public class PatternElement
	{
		public int NumNotes { get; set; }
		public double NoteLength { get; set; }
		public IList<ChannelElement> Channels { get; set; } = new List<ChannelElement> ();
	}

	// new

	public class SequenceElement : SequenceElemntBase
	{
	}

	// old

	public class MidiSequenceElement : SequenceElemntBase
	{
	}
	
	public class SequenceElemntBase
	{
		public int Ver { get; set; }
		public int ChannelNumber { get; set; }
		public IList<AbstractMidiEventElement> Events { get; private set; } = new List<AbstractMidiEventElement> ();
	}


	public abstract class AbstractMidiEventElement
	{
		[DataType (DataType.Length)]
		public double B { get; set; }
	}


	public class ControlElement : AbstractMidiEventElement
	{
		public int Type { get; set; }
		public int Val { get; set; }
		public int? Metadata { get; set; }
	}


	public class NoteElement : AbstractMidiEventElement
	{
		public int P { get; set; }
		[DataType (DataType.Length)]
		public double L { get; set; }
		public int V { get; set; }
		public int C { get; set; }
		public double? InitialTimbre { get; set; }
		public double? InitialPressure { get; set; }
		public double? InitialPitchbend { get; set; }
	}


	public class SysexElement : AbstractMidiEventElement
	{
		[DataType (DataType.Length)]
		public double Time { get; set; }
		[DataType (DataType.HexBinary)]
		public byte [] Data { get; set; }
	}


	public class QuantisationElement
	{
		public string Type { get; set; }
		public double? Amount { get; set; }
	}


	public class GrooveElement
	{
		public string Current { get; set; }
	}


	public class PatternGeneratorElement
	{
		public ProgressionElement Progression { get; set; }
	}


	public class ProgressionElement
	{
	}


	public class OutputDevicesElement
	{
		public IList<DeviceElement> OutputDevices { get; private set; } = new List<DeviceElement> ();
	}


	public class DeviceElement
	{
		public string Name { get; set; }
	}


	public class MidiViewStateElement : AbstractViewElement
	{
		[DataType (DataType.Length)]
		public double LeftTime { get; set; }
		[DataType (DataType.Length)]
		public double RightTime { get; set; }
	}


	public class ArrangeViewElement
	{
		public MixerViewStateElement MixerViewState { get; set; }
	}


	public class MixerViewStateElement
	{
		[DataType (DataType.BooleanInt)]
		public bool OverviewVisible { get; set; }
		[DataType (DataType.BooleanInt)]
		public bool SmallMetersVisible { get; set; }
		[DataType (DataType.BooleanInt)]
		public bool BigMetersVisible { get; set; }
		[DataType (DataType.BooleanInt)]
		public bool OutputsVisible { get; set; }
		[DataType (DataType.BooleanInt)]
		public bool ModifiersVisible { get; set; }
		[DataType (DataType.BooleanInt)]
		public bool PluginsVisible { get; set; }
	}


	public class EditMixGroupsElement
	{
	}


	public class AudioEditingElement
	{
	}
}
