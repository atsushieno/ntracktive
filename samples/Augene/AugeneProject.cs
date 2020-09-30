using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Augene {
	public class AugeneProject
	{
		public static AugeneProject Load (string filename)
		{
			var serializer = new XmlSerializer (typeof (AugeneProject));
			using (var fileStream = File.OpenRead(filename)) {
				var project = (AugeneProject) serializer.Deserialize (XmlReader.Create (fileStream));
				return project;
			}
		}

		public static void Save (AugeneProject project, string filename)
		{
			// sanitize absolute paths
			foreach (var track in project.Tracks)
				if (Path.IsPathRooted (track.AudioGraph))
					track.AudioGraph = new Uri (filename).MakeRelativeUri (new Uri (track.AudioGraph)).ToString ();
			
			var serializer = new XmlSerializer (typeof (AugeneProject));
			using (var tw = File.CreateText (filename))
				serializer.Serialize (tw, project);
		}
		
		[XmlArrayItem ("AudioGraph")] public List<AugeneAudioGraph> AudioGraphs { get; set; } = new List<AugeneAudioGraph> ();
		[XmlArrayItem ("AudioGraph")] public List<string> MasterPlugins { get; set; } = new List<string> ();
		public List<AugeneTrack> Tracks { get; set; } = new List<AugeneTrack> ();
		[XmlArrayItem ("MmlFile")] public List<string> MmlFiles { get; set; } = new List<string> ();
		[XmlArrayItem ("MmlString")] public List<string> MmlStrings { get; set; } = new List<string> ();
	}

	public class AugeneAudioGraph
	{
		[XmlAttribute]
		public string? Id { get; set; }
		[XmlAttribute]
		public string? Source { get; set; }
	}

	public class AugeneTrack
	{
		public string? Id { get; set; }
		public string? AudioGraph { get; set; }
	}

	public class JuceAudioGraph
	{
		public const string EmptyAudioGraph = @"<FILTERGRAPH>
  <FILTER uid='5' x='0.5' y='0.1'>
    <PLUGIN name='Audio Input' descriptiveName='' format='Internal' category='I/O devices' manufacturer='JUCE' version='1.0' file='' uid='246006c0' isInstrument='0' fileTime='0' infoUpdateTime='0' numInputs='0' numOutputs='4' isShell='0'/>
    <STATE>0.</STATE>
    <LAYOUT>
      <INPUTS><BUS index='0' layout='disabled'/></INPUTS>
      <OUTPUTS><BUS index='0' layout='disabled'/></OUTPUTS>
    </LAYOUT>
  </FILTER>
  <FILTER uid='6' x='0.25' y='0.1'>
    <PLUGIN name='Midi Input' descriptiveName='' format='Internal' category='I/O devices' manufacturer='JUCE' version='1.0' file='' uid='cb5fde0b' isInstrument='0' fileTime='0' infoUpdateTime='0' numInputs='0' numOutputs='0' isShell='0'/>
    <STATE>0.</STATE>
    <LAYOUT>
      <INPUTS><BUS index='0' layout='disabled'/></INPUTS>
      <OUTPUTS><BUS index='0' layout='disabled'/></OUTPUTS>
    </LAYOUT>
  </FILTER>
  <FILTER uid='7' x='0.5' y='0.9'>
    <PLUGIN name='Audio Output' descriptiveName='' format='Internal' category='I/O devices' manufacturer='JUCE' version='1.0' file='' uid='724248cb' isInstrument='0' fileTime='0' infoUpdateTime='0' numInputs='0' numOutputs='0' isShell='0'/>
    <STATE>0.</STATE>
    <LAYOUT>
      <INPUTS><BUS index='0' layout='L R Ls Rs'/></INPUTS>
      <OUTPUTS><BUS index='0' layout='disabled'/></OUTPUTS>
    </LAYOUT>
  </FILTER>
</FILTERGRAPH>
";
		
		public static IEnumerable<JuceAudioGraph> Load (XmlReader reader)
		{
			var ret = new JuceAudioGraph ();
			var doc = XDocument.Load (reader);
			var input = doc.Root.Elements ("FILTER").FirstOrDefault (e =>
				e.Elements ("PLUGIN").Any (p => string.Equals (p.Attribute ("name")?.Value, "Midi Input", StringComparison.OrdinalIgnoreCase) && // it is MIDI Input since Waveform11 (maybe)
				                                p.Attribute ("format")?.Value == "Internal"));
			var output = doc.Root.Elements ("FILTER").FirstOrDefault (e =>
				e.Elements ("PLUGIN").Any (p => p.Attribute ("name")?.Value == "Audio Output" &&
				                                p.Attribute ("format")?.Value == "Internal"));
			if (input == null || output == null)
				yield break;
			XElement conn;
			for (string? uid = input.Attribute ("uid")?.Value;
				(conn = doc.Root.Elements ("CONNECTION").FirstOrDefault (e =>
					e.Attribute ("srcFilter")?.Value == uid)) != null && conn != output;
				uid = conn.Attribute ("dstFilter")?.Value) {
				if (uid != input.Attribute ("uid")?.Value) {
					var filter = doc.Root.Elements ("FILTER")
						.FirstOrDefault (e => e.Attribute ("uid")?.Value == uid);
					if (filter == null)
						yield break;
					var plugin = filter.Element ("PLUGIN");
					if (plugin == null)
						yield break;
					var state = filter.Element ("STATE");
					var prog = plugin.Attribute ("programNum");
					yield return new JuceAudioGraph {
						File = plugin.Attribute ("file")?.Value,
						Category = plugin.Attribute ("category")?.Value,
						Manufacturer = plugin.Attribute ("manufacturer")?.Value,
						Name = plugin.Attribute ("name")?.Value,
						Uid = plugin.Attribute ("uid")?.Value,
						ProgramNum = prog != null ? int.Parse (prog.Value) : 0,
						State = state != null ? state.Value : null
					};
				}
			}
		}

		//<PLUGIN name="Midi Input" descriptiveName="" format="Internal" category="I/O devices"
		//manufacturer="JUCE" version="1.0" file="" uid="cb5fde0b" isInstrument="0"
		//fileTime="0" infoUpdateTime="0" numInputs="0" numOutputs="0"
		//isShell="0"/>
		//<STATE>0.</STATE>
		//
		// For audio plugins there is something like `type="vst" ` too.
		public string? Name { get; set; }
		public string? DescriptiveName { get; set; }
		public string? Format { get; set; }
		public string? Category { get; set; }
		public string? Manufacturer { get; set; }
		public string? Version { get; set; }
		public string? File { get; set; }
		public string? Uid { get; set; }
		public int IsInstrument { get; set; }
		public long FileTime { get; set; }
		public long InfoUpdateTime { get; set; }
		public int NumInputs { get; set; }
		public int NumOutputs { get; set; }
		public int IsShell { get; set; }
		public string? State { get; set; }
		public int ProgramNum { get; set; } // ?
	}

	public class AugenePluginSpecifier
	{
		public static IEnumerable<AugenePluginSpecifier> FromAudioGraph (IEnumerable<JuceAudioGraph> audioGraph)
		{
			return audioGraph.Select (src => new AugenePluginSpecifier {
				Type = src.Format,
				Uid = src.Uid,
				Filename = src.File,
				Name = src.Name,
				Manufacturer = src.Manufacturer,
				ProgramNum = src.ProgramNum,
				State = src.State
			});
		}

		// They are required by tracktionedit.
		public string? Type { get; set; }
		public string? Uid { get; set; }
		public string? Filename { get; set; }
		public string? Name { get; set; }
		public string? Manufacturer { get; set; }
		public int ProgramNum { get; set; }
		public string? State { get; set; }
	}
}
