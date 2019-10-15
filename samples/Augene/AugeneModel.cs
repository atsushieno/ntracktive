using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Commons.Music.Midi.Mml;
using Midi2TracktionEdit;
using NTracktive;

namespace Augene {

	public abstract class DialogAbstraction
	{
		public class DialogOptions
		{
			public bool MultipleFiles { get; set; }
		}
		
		public abstract void ShowWarning (string message);

		public String [] ShowOpenFileDialog (string dialogTitle) =>
			ShowOpenFileDialog (dialogTitle, new DialogOptions ());
		public abstract String [] ShowOpenFileDialog (string dialogTitle, DialogOptions options);

		public String [] ShowSaveFileDialog (string dialogTitle) =>
			ShowSaveFileDialog (dialogTitle, new DialogOptions ());
		public abstract String [] ShowSaveFileDialog (string dialogTitle, DialogOptions options);
	}
	
	public class AugeneModel
	{
		public static AugeneProject Load (string filename)
		{
			var serializer = new XmlSerializer (typeof (AugeneProject));
			var proj = new AugeneProject ();
			using (var fileStream = File.OpenRead (filename))
				return (AugeneProject) serializer.Deserialize (XmlReader.Create (fileStream));
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

		public static void Compile (AugeneProject project)
		{
			var compiler = new MmlCompiler ();
			var mmls = project.MmlFiles.Select (filename =>
					new MmlInputSource (filename, new StringReader (File.ReadAllText (filename))))
				.Concat (project.MmlStrings.Select (s =>
					new MmlInputSource ("(no file)", new StringReader (s))));
			var music = compiler.Compile (false, mmls.ToArray ());
			var converter = new MidiToTracktionEditConverter ();
			var edit = new EditElement ();
			converter.ImportMusic (new MidiImportContext (music, edit));
			var dstTracks = edit.Tracks.OfType<TrackElement> ().ToArray ();
			for (int n = 0; n < dstTracks.Length; n++)
				if (edit.Tracks [n].Id == null)
					edit.Tracks [n].Id = (n + 1).ToString (CultureInfo.CurrentCulture);
			foreach (var track in project.Tracks) {
				var dstTrack = dstTracks.FirstOrDefault (t =>
					t.Id == track.Id.ToString (CultureInfo.CurrentCulture));
				if (dstTrack == null)
					continue;
				var existingPlugins = dstTrack.Plugins.ToArray ();
				dstTrack.Plugins.Clear ();
				foreach (var p in ToTracktion (AugenePluginSpecifier.FromAudioGraph (
					AudioGraph.Load (XmlReader.Create (track.AudioGraph)))))
					dstTrack.Plugins.Add (p);
				// recover volume and level at the end.
				foreach (var p in existingPlugins)
					dstTrack.Plugins.Add (p);
			}

			new EditModelWriter ().Write (Console.Out, edit);
		}

		static IEnumerable<PluginElement> ToTracktion (IEnumerable<AugenePluginSpecifier> src)
		{
			return src.Select (a => new PluginElement {
				Filename = a.Filename,
				Enabled = true,
				Uid = a.Uid,
				Type = a.Type ?? "vst",
				Name = a.Name,
				Manufacturer = a.Manufacturer,
				State = a.State,
			});
		}

		public AugeneModel (DialogAbstraction dialogs)
		{
			Dialogs = dialogs;
		}
		
		const string ConfigXmlFile = "augene-config.xml";
		
		public AugeneProject Project { get; set; }
		public string ProjectFileName { get; set; }
		
		public string ConfigAudioPluginHostPath { get; set; }
		public string ConfigPlaybackDemoPath { get; set; }

		public DialogAbstraction Dialogs { get; set; }

		public void LoadConfiguration ()
		{
			using (var fs = IsolatedStorageFile.GetUserStoreForAssembly ()) {
				if (!fs.FileExists (ConfigXmlFile))
					return;
				try {
					using (var file = fs.OpenFile (ConfigXmlFile, FileMode.Open)) {
						using (var xr = XmlReader.Create (file)) {
							xr.MoveToContent ();
							xr.ReadStartElement ("config");
							ConfigPlaybackDemoPath =
								xr.ReadElementString ("PlaybackDemo");
							ConfigAudioPluginHostPath =
								xr.ReadElementString ("AudioPluginHost");
						}
					}
				} catch (Exception ex) {
					Console.WriteLine (ex);
					Dialogs.ShowWarning ("Failed to load configuration file. It is ignored.");
				}
			}
		}

		public void SaveConfiguration ()
		{
			using (var fs = IsolatedStorageFile.GetUserStoreForAssembly ()) {
				using (var file = fs.CreateFile (ConfigXmlFile)) {
					using (var xw = XmlWriter.Create (file)) {
						xw.WriteStartElement ("config");
						xw.WriteElementString ("PlaybackDemo",
							ConfigPlaybackDemoPath);
						xw.WriteElementString ("AudioPluginHost",
							ConfigAudioPluginHostPath);
					}
				}
			}
		}

		public event Action RefreshRequested;

		public void ProcessOpenProject ()
		{
			var files = Dialogs.ShowOpenFileDialog ("Open Augene Project");
			if (files.Any ())
			Project = Load (files [0]);
			ProjectFileName = files [0];

			RefreshRequested?.Invoke ();
		}

		public void ProcessSaveProject ()
		{
			if (ProjectFileName == null) {
				var files = Dialogs.ShowSaveFileDialog("Save Augene Project");
				if (files.Any ()) {
					ProjectFileName = files [0];
				}
				else
					return;
			}
			Save (Project, ProjectFileName);
		}

		public void ProcessNewTrack (bool selectFileInsteadOfNewFile)
		{
			if (selectFileInsteadOfNewFile) {
				var files = Dialogs.ShowOpenFileDialog ("Select existing AudioGraph file for a new track");
				if (files.Any ())
					AddNewTrack (files [0]);
			} else {
				var files = Dialogs.ShowSaveFileDialog ("New AudioGraph file for a new track");
				if (files.Any ()) {
					File.WriteAllText (files [0], AudioGraph.EmptyAudioGraph);
					AddNewTrack (files [0]);
				}
			}
		}

		public void AddNewTrack (string filename)
		{
			string filenameRelative = filename;
			if (ProjectFileName != null)
				filenameRelative = new Uri (ProjectFileName).MakeRelative (new Uri (filename)); 
			int newTrackId = 1 + (int) Project.Tracks.Select (t => t.Id).Max ();
			Project.Tracks.Add (new AugeneTrack {Id = newTrackId, AudioGraph = filenameRelative});
			
			RefreshRequested?.Invoke ();
		}

		public void ProcessDeleteTracks (IEnumerable<double> trackIdsToRemove)
		{
			var tracksRemaining = Project.Tracks.Where (t => !trackIdsToRemove.Contains (t.Id));
			Project.Tracks.Clear ();
			Project.Tracks.AddRange (tracksRemaining);

			RefreshRequested?.Invoke ();
		}

		public void ProcessLaunchAudioPluginHost (string audioGraphFile)
		{
			if (ConfigAudioPluginHostPath == null)
				Dialogs.ShowWarning ("AudioPluginHost path is not configured [File > Configure].");
			else {
				Process.Start (ConfigAudioPluginHostPath,
					Path.Combine (ProjectFileName, audioGraphFile));
			}
		}
	}

	public class AugeneProject
	{
		public List<AugeneTrack> Tracks { get; set; } = new List<AugeneTrack> ();
		[XmlArrayItem ("MmlFile")] public List<string> MmlFiles { get; set; } = new List<string> ();
		[XmlArrayItem ("MmlString")] public List<string> MmlStrings { get; set; } = new List<string> ();
	}

	public class AugeneTrack
	{
		public double Id { get; set; }
		public string AudioGraph { get; set; }
	}

	public class AudioGraph
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
		
		public static IEnumerable<AudioGraph> Load (XmlReader reader)
		{
			var ret = new AudioGraph ();
			var doc = XDocument.Load (reader);
			var input = doc.Root.Elements ("FILTER").FirstOrDefault (e =>
				e.Elements ("PLUGIN").Any (p => p.Attribute ("name")?.Value == "Midi Input" &&
				                                p.Attribute ("format")?.Value == "Internal"));
			var output = doc.Root.Elements ("FILTER").FirstOrDefault (e =>
				e.Elements ("PLUGIN").Any (p => p.Attribute ("name")?.Value == "Audio Output" &&
				                                p.Attribute ("format")?.Value == "Internal"));
			if (input == null || output == null)
				yield break;
			XElement conn;
			for (string uid = input.Attribute ("uid")?.Value;
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
					yield return new AudioGraph {
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
		public string Name { get; set; }
		public string DescriptiveName { get; set; }
		public string Format { get; set; }
		public string Category { get; set; }
		public string Manufacturer { get; set; }
		public string Version { get; set; }
		public string File { get; set; }
		public string Uid { get; set; }
		public int IsInstrument { get; set; }
		public long FileTime { get; set; }
		public long InfoUpdateTime { get; set; }
		public int NumInputs { get; set; }
		public int NumOutputs { get; set; }
		public int IsShell { get; set; }
		public string State { get; set; }
		public int ProgramNum { get; set; } // ?
	}

	public class AugenePluginSpecifier
	{
		public static IEnumerable<AugenePluginSpecifier> FromAudioGraph (IEnumerable<AudioGraph> audioGraph)
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
		public string Type { get; set; }
		public string Uid { get; set; }
		public string Filename { get; set; }
		public string Name { get; set; }
		public string Manufacturer { get; set; }
		public int ProgramNum { get; set; }
		public string State { get; set; }
	}
}
