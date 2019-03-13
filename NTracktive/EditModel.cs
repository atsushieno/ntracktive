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

	[StructLayout (LayoutKind.Sequential)]
	public class DataTypeAttribute : Attribute
	{
		public DataTypeAttribute (DataType type)
		{
			DataType = type;
		}

		public DataType DataType { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
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

	[StructLayout (LayoutKind.Sequential)]
	public class TransportElement
	{
		public double? Position { get; set; }
		public double? ScrubInterval { get; set; }
		public double? LoopPoint1 { get; set; }
		public double? LoopPoint2 { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class MacroParametersElement
	{
		public string Id { get; set; } // new
		[DataType (DataType.Id)]
		public string MediaId { get; set; } // old
	}

	[StructLayout (LayoutKind.Sequential)]
	public class TempoSequenceElement
	{
		public IList<TempoElement> Tempos { get; private set; } = new List<TempoElement> ();
		public IList<TimeSigElement> TimeSignatures { get; private set; } = new List<TimeSigElement> ();
	}

	[StructLayout (LayoutKind.Sequential)]
	public class TempoElement
	{
		[DataType (DataType.Length)]
		public double StartBeat { get; set; }
		public double Bpm { get; set; }
		public double Curve { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class TimeSigElement
	{
		public int Numerator { get; set; }
		public int Denominator { get; set; }
		[DataType (DataType.Length)]
		public double StartBeat { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class PitchSequenceElement
	{
		public IList<PitchElement> Pitches { get; private set; } = new List<PitchElement> ();
	}

	[StructLayout (LayoutKind.Sequential)]
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

	[StructLayout (LayoutKind.Sequential)]
	public class VideoElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
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

	[StructLayout (LayoutKind.Sequential)]
	public class TrackEditorsElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class FacePlateViewElement
	{
		public bool? EditModeActive { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class AutoMapXmlElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class ClickTrackElement
	{
		public double Level { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class Id3VorbisMetadataElement
	{
		public double TrackNumber { get; set; }
		[DataType (DataType.UnixTime)]
		public string Date { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class MasterVolumeElement
	{
		// new
		public IList<PluginElement> Plugins { get; private set; } = new List<PluginElement> ();
		// old
		public IList<FilterElement> Filters { get; private set; } = new List<FilterElement> ();
	}

	[StructLayout (LayoutKind.Sequential)]
	public class RackElementBase
	{
	}
	
	[StructLayout (LayoutKind.Sequential)]
	public class RackFiltersElement : RackElementBase
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class RacksElement : RackElementBase
	{
	}

	public class MasterPluginsElementBase
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class MasterFiltersElement : MasterPluginsElementBase
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class MasterPluginsElement : MasterPluginsElementBase
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class AuxBusNamesElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class InputDevicesElementBase
	{
		public IList<InputDeviceElement> InputDevices { get; private set; } = new List<InputDeviceElement> ();
	}

	// new
	[StructLayout (LayoutKind.Sequential)]
	public class InputDevicesElement : InputDevicesElementBase
	{		
	}

	// old
	// I hope it is not the other casing...
	[StructLayout (LayoutKind.Sequential)]
	public class DevicesExElement : InputDevicesElementBase
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class InputDeviceElement
	{
		public string Name { get; set; }
		[DataType (DataType.Id)]
		public string TargetTrack { get; set; }
		public int TargetIndex { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class TrackCompsElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class AraDocumentElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class ControllerMappingsElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public abstract class AbstractViewElement
	{
		public double? Width { get; set; }
		public double? Height { get; set; }
		public double? VerticalOffset { get; set; }
		public double? VisibleProportion { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
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

	[StructLayout (LayoutKind.Sequential)]
	public class TempoTrackElement : AbstractTrackElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class ModifiersElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class MarkerTrackElement : AbstractTrackElement
	{
		public int TrackType { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
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

		// elements
		public MacroParametersElement MacroParameters { get; set; }
		public ModifierAssignmentsElement ModifierAssignments { get; set; }
		public FacePlateElement FacePlate { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class FilterElement : PluginElementBase
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class PluginElement : PluginElementBase
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class ModifierAssignmentsElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
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

	[StructLayout (LayoutKind.Sequential)]
	public class ChordTrackElement : AbstractTrackElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
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
	[StructLayout (LayoutKind.Sequential)]
	public class ClipElement : MidiClipElementBase
	{
		public MidiSequenceElement MidiSequence { get; set; }
	}

	// new
	[StructLayout (LayoutKind.Sequential)]
	public class MidiClipElement : MidiClipElementBase
	{
		public SequenceElement Sequence { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
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

	[StructLayout (LayoutKind.Sequential)]
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
	[StructLayout (LayoutKind.Sequential)]
	public class SequenceElement : SequenceElemntBase
	{
	}

	// old
	[StructLayout (LayoutKind.Sequential)]
	public class MidiSequenceElement : SequenceElemntBase
	{
	}
	
	public class SequenceElemntBase
	{
		public int Ver { get; set; }
		public int ChannelNumber { get; set; }
		public IList<AbstractMidiEventElement> Events { get; private set; } = new List<AbstractMidiEventElement> ();
	}

	[StructLayout (LayoutKind.Sequential)]
	public abstract class AbstractMidiEventElement
	{
		[DataType (DataType.Length)]
		public double B { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class ControlElement : AbstractMidiEventElement
	{
		public int Type { get; set; }
		public int Val { get; set; }
		public int? Metadata { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
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

	[StructLayout (LayoutKind.Sequential)]
	public class SysexElement : AbstractMidiEventElement
	{
		[DataType (DataType.Length)]
		public double Time { get; set; }
		[DataType (DataType.HexBinary)]
		public byte [] Data { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class QuantisationElement
	{
		public string Type { get; set; }
		public double? Amount { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class GrooveElement
	{
		public string Current { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class PatternGeneratorElement
	{
		public ProgressionElement Progression { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class ProgressionElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class OutputDevicesElement
	{
		public IList<DeviceElement> OutputDevices { get; private set; } = new List<DeviceElement> ();
	}

	[StructLayout (LayoutKind.Sequential)]
	public class DeviceElement
	{
		public string Name { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class MidiViewStateElement : AbstractViewElement
	{
		[DataType (DataType.Length)]
		public double LeftTime { get; set; }
		[DataType (DataType.Length)]
		public double RightTime { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
	public class ArrangeViewElement
	{
		public MixerViewStateElement MixerViewState { get; set; }
	}

	[StructLayout (LayoutKind.Sequential)]
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

	[StructLayout (LayoutKind.Sequential)]
	public class EditMixGroupsElement
	{
	}

	[StructLayout (LayoutKind.Sequential)]
	public class AudioEditingElement
	{
	}
}
