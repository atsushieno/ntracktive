using System;
using System.Collections.Generic;

	
/*
 * Naming rules:
 * 
 * - Elements end with "Element".
 *   - It is to differentiate type name and member property name to avoid possible conflict e.g. <PITCH pitch="..." />
 * - Simple type properties are serialized as attributes.
 * - All those elements are UPPERCASED, and attributes are camelCased,
 *   with an exception that '_' indicates namespace prefix splitter.
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
		public ViewStateElement ViewState { get; set; }
		public AutoMapXmlElement AutoMapXml { get; set; }
		public ClickTrackElement ClickTrack { get; set; }
		public Id3VorbisMetadataElement Id3VorbisMetadata { get; set; }
		public MasterVolumeElement MasterVolume { get; set; }
		public RackFiltersElement RackFilters { get; set; }
		public MasterFiltersElement MasterFilters { get; set; }
		public AuxBusNamesElement AuxBusNames { get; set; }
		public DevicesExElement DevicesEx { get; set; }
		public TrackCompsElement TrackComps { get; set; }
		public AraDocumentElement AraDocument { get; set; }
		public ControllerMappingsElement ControllerMappings { get; set; }
		public MidiViewStateElement MidiViewState { get; set; }
		public ArrangeViewElement ArrangeView { get; set; }

		public TempoTrackElement TempoTrack { get; set; }
		public IList<MarkerTrackElement> MarkerTracks { get; private set; } = new List<MarkerTrackElement> ();
		public ChordTrackElement ChordTrack { get; set; }
		public IList<TrackElement> Tracks { get; private set; } = new List<TrackElement> ();
	}

	public class TransportElement
	{
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
		[DataType (DataType.Length)]
		public double Start { get; set; }
		public double Pitch { get; set; }
	}

	public class ViewStateElement
	{
		// attributes
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
	}

	public class FacePlateViewElement
	{
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
		public IList<FilterElement> Filter { get; private set; } = new List<FilterElement> ();
	}

	public class RackFiltersElement
	{
	}

	public class MasterFiltersElement
	{
	}

	public class AuxBusNamesElement
	{
	}

	// I hope it is not DeviceSexElement...
	public class DevicesExElement
	{
		public IList<InputDeviceElement> InputDevices { get; private set; } = new List<InputDeviceElement> ();
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
		public double Width { get; set; }
		public double Height { get; set; }
		public double VerticalOffset { get; set; }
		public double VisibleProportion { get; set; }
	}

	public abstract class AbstractTrackElement : AbstractViewElement
	{
		public string Name { get; set; }
		[DataType (DataType.Id)]
		public string MediaId { get; set; }

		public MacroParametersElement MacroParameters { get; set; }
		public ModifiersElement Modifiers { get; set; }
	}

	public class TempoTrackElement : AbstractTrackElement
	{
	}

	public class ModifiersElement
	{
	}

	public class MarkerTrackElement : AbstractTrackElement
	{
		public int TrackType { get; set; }
	}

	public class FilterElement
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
		public double WindowX { get; set; }
		public double WindowY { get; set; }
		public bool WindowLocked { get; set; }

		// elements
		public MacroParametersElement MacroParameters { get; set; }
		public ModifierAssignmentsElement ModifierAssignments { get; set; }
		public FacePlateElement FacePlate { get; set; }
	}

	public class ModifierAssignmentsElement
	{
	}

	public class FacePlateElement : AbstractViewElement
	{
		public bool AutoSize { get; set; }
	}

	public class ChordTrackElement : AbstractTrackElement
	{
	}

	public class TrackElement : AbstractTrackElement
	{
		public double MidiVProp { get; set; }
		public double MidiVOffset { get; set; }
		public string Colour { get; set; }
		public bool Solo { get; set; }
		public bool Mute { get; set; }

		public IList<ClipElement> Clips { get; private set; } = new List<ClipElement> ();
		public IList<FilterElement> Filters { get; private set; } = new List<FilterElement> ();
		public OutputDevicesElement OutputDevices { get; set; }
	}

	public class ClipElement
	{
		// attributes
		public int Channel { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		[DataType (DataType.Length)]
		public double Start { get; set; }
		[DataType (DataType.Length)]
		public double Length { get; set; }
		[DataType (DataType.Length)]
		public double Offset { get; set; }
		[DataType (DataType.Id)]
		public string Source { get; set; }
		public double Sync { get; set; }
		[DataType (DataType.Id)]
		public string MediaId { get; set; }
		public string Colour { get; set; }
		public int CurrentTake { get; set; }
		public double Speed { get; set; }

		// elements
		public MidiSequenceElement MidiSequence { get; set; }
		public QuantisationElement Quantisation { get; set; }
		public GrooveElement Groove { get; set; }
		public PatternGeneratorElement PatternGenerator { get; set; }
	}

	public class MidiSequenceElement
	{
		public int Ver { get; set; }
		public int ChannelNumber { get; set; }
		public IList<AbstractMidiEventElement> Events { get; private set; } = new List<AbstractMidiEventElement> ();
	}

	public abstract class AbstractMidiEventElement
	{
	}

	public class ControlElement : AbstractMidiEventElement
	{
		[DataType (DataType.Length)]
		public double B { get; set; }
		public int Type { get; set; }
		public int Val { get; set; }
	}

	public class NoteElement : AbstractMidiEventElement
	{
		public int P { get; set; }
		[DataType (DataType.Length)]
		public double B { get; set; }
		[DataType (DataType.Length)]
		public double L { get; set; }
		public int V { get; set; }
		public int C { get; set; }
		public double InitialTimbre { get; set; }
		public double InitialPressure { get; set; }
		public double InitialPitchbend { get; set; }
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
	}

	public class GrooveElement
	{
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
}
