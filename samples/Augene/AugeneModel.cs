using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using Commons.Music.Midi;
using Commons.Music.Midi.Mml;
using Midi2TracktionEdit;
using NTracktive;

namespace Augene {

	public abstract class DialogAbstraction
	{
		public class DialogOptions
		{
			public string InitialDirectory { get; set; }
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
		const string ConfigXmlFile = "augene-config.xml";
		
		public bool AutoReloadProject { get; set; }
		
		public bool AutoCompileProject { get; set; }
		
		public AugeneProject Project { get; set; } = new AugeneProject ();
		public string? ProjectFileName { get; set; }
		
		public string? OutputEditFileName { get; set; }

		public string? ConfigAudioPluginHostPath { get; set; }

		public string? ConfigAugenePlayerPath { get; set; }

		public string? LastProjectFile { get; set; }

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
							if (xr.IsEmptyElement)
								return;
							xr.ReadStartElement ("config");
							for (xr.MoveToContent ();
								xr.NodeType == XmlNodeType.Element;
								xr.MoveToContent ()) {
								string name = xr.LocalName;
								string s = xr.ReadElementContentAsString ();
								switch (name) {
								case "AugenePlayer":
									ConfigAugenePlayerPath = s;
									break;
								case "AudioPluginHost":
									ConfigAudioPluginHostPath = s;
									break;
								case "LastProjectFile":
									LastProjectFile = s;
									break;
								}
							}
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
						xw.WriteElementString ("AugenePlayer",
							ConfigAugenePlayerPath);
						xw.WriteElementString ("AudioPluginHost",
							ConfigAudioPluginHostPath);
						xw.WriteElementString ("LastProjectFile",
							LastProjectFile);
					}
				}
			}
		}

		public event Action? RefreshRequested;

		public string GetItemFileRelativePath (string itemFilename)
		{
			string filenameRelative = itemFilename;
			if (ProjectFileName != null)
				filenameRelative = new Uri (ProjectFileName).MakeRelativeUri (new Uri (itemFilename)).ToString ();
			return filenameRelative;
		}

		public string GetItemFileAbsolutePath (string itemFilename)
		{
			return Path.Combine (Path.GetDirectoryName (ProjectFileName), itemFilename);
		}

		public void ProcessOpenProject ()
		{
			var files = Dialogs.ShowOpenFileDialog ("Open Augene Project");
			if (files.Any ()) {
				ProcessLoadProjectFile (files[0]);
			}
		}

		public void ProcessLoadProjectFile (string file)
		{
			var prevFile = ProjectFileName;
			Project = AugeneProject.Load (file);
			ProjectFileName = file;
			LastProjectFile = ProjectFileName;
			if (prevFile != file) {
				// FIXME: it is kind of hack, but so far we unify history with config.
				SaveConfiguration();
				
				project_file_watcher.Path = Path.GetDirectoryName(ProjectFileName);
				if (!project_file_watcher.EnableRaisingEvents)
					project_file_watcher.EnableRaisingEvents = true;

				UpdateAutoReloadSetup ();
			}

			if (RefreshRequested != null)
				Xwt.Application.Invoke (RefreshRequested);
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
			AugeneProject.Save (Project, ProjectFileName);
		}

		public void ProcessNewTrack (bool selectFileInsteadOfNewFile)
		{
			if (selectFileInsteadOfNewFile) {
				var files = Dialogs.ShowOpenFileDialog ("Select existing AudioGraph file for a new track",
					new DialogAbstraction.DialogOptions { InitialDirectory = Path.GetDirectoryName(this.ProjectFileName) });
				if (files.Any ())
					AddNewTrack (files [0]);
			} else {
				var files = Dialogs.ShowSaveFileDialog ("New AudioGraph file for a new track",
					new DialogAbstraction.DialogOptions { InitialDirectory = Path.GetDirectoryName(this.ProjectFileName) });
				if (files.Any ()) {
					File.WriteAllText (files [0], JuceAudioGraph.EmptyAudioGraph);
					AddNewTrack (files [0]);
				}
			}
		}

		public void AddNewTrack (string filename)
		{
			int newTrackId = 1 + (int) Project.Tracks.Count;
			while (Project.Tracks.Any (t => t.Id == newTrackId.ToString ()))
				newTrackId++;
			Project.Tracks.Add (new AugeneTrack {Id = newTrackId.ToString (), AudioGraph = GetItemFileRelativePath (filename)});
			
			RefreshRequested?.Invoke ();
		}

		public void ProcessDeleteTracks (IEnumerable<string> trackIdsToRemove)
		{
			var tracksRemaining = Project.Tracks.Where (t => !trackIdsToRemove.Contains (t.Id)).ToArray ();
			Project.Tracks.Clear ();
			Project.Tracks.AddRange (tracksRemaining);

			RefreshRequested?.Invoke ();
		}

		public void ProcessNewAudioGraph (bool selectFileInsteadOfNewFile)
		{
			if (selectFileInsteadOfNewFile) {
				var files = Dialogs.ShowOpenFileDialog ("Select existing AudioGraph file",
					new DialogAbstraction.DialogOptions { InitialDirectory = Path.GetDirectoryName(this.ProjectFileName) });
				if (files.Any ())
					AddNewAudioGraph (files [0]);
			} else {
				var files = Dialogs.ShowSaveFileDialog ("New AudioGraph file",
					new DialogAbstraction.DialogOptions { InitialDirectory = Path.GetDirectoryName(this.ProjectFileName) });
				if (files.Any ()) {
					File.WriteAllText (files [0], JuceAudioGraph.EmptyAudioGraph);
					AddNewAudioGraph (files [0]);
				}
			}
		}

		public void AddNewAudioGraph (string filename)
		{
			int newGraphId = 1 + (int) Project.AudioGraphs.Count;
			while (Project.Tracks.Any (t => t.Id == newGraphId.ToString ()))
				newGraphId++;
			Project.AudioGraphs.Add (new AugeneAudioGraph {Id = newGraphId.ToString (), Source = GetItemFileRelativePath (filename)});
			
			RefreshRequested?.Invoke ();
		}

		public void ProcessDeleteAudioGraphs (IEnumerable<string> audioGraphIdsToRemove)
		{
			var graphsRemaining = Project.AudioGraphs.Where (t => !audioGraphIdsToRemove.Contains (t.Id)).ToArray ();
			Project.AudioGraphs.Clear ();
			Project.AudioGraphs.AddRange (graphsRemaining);

			RefreshRequested?.Invoke ();
		}

		public void ProcessNewMmlFile (bool selectFileInsteadOfNewFile)
		{
			if (selectFileInsteadOfNewFile) {
				var files = Dialogs.ShowOpenFileDialog ("Select existing MML file",
					new DialogAbstraction.DialogOptions { InitialDirectory = Path.GetDirectoryName(this.ProjectFileName) });
				if (files.Any ())
					AddNewMmlFile (files [0]);
			} else {
				var files = Dialogs.ShowSaveFileDialog ("New MML file",
					new DialogAbstraction.DialogOptions { InitialDirectory = Path.GetDirectoryName(this.ProjectFileName) });
				if (files.Any ()) {
					File.WriteAllText (files [0], "// New MML file");
					AddNewMmlFile (files [0]);
				}
			}
		}
		public void AddNewMmlFile (string filename)
		{
			Project.MmlFiles.Add (GetItemFileRelativePath (filename));
			
			RefreshRequested?.Invoke ();
		}

		public void ProcessUnregisterMmlFiles (IEnumerable<string> filesToUnregister)
		{
			var filesRemaining = Project.MmlFiles.Where (f => !filesToUnregister.Contains (f)).ToArray ();
			Project.MmlFiles.Clear ();
			Project.MmlFiles.AddRange (filesRemaining);

			RefreshRequested?.Invoke ();
		}

		public void ProcessLaunchAudioPluginHost (string audioGraphFile)
		{
			if (ConfigAudioPluginHostPath == null)
				Dialogs.ShowWarning ("AudioPluginHost path is not configured [File > Configure].");
			else {
				Process.Start (ConfigAudioPluginHostPath, GetItemFileAbsolutePath (audioGraphFile));
			}
		}

		public void ProcessNewMasterPluginFile (bool selectFileInsteadOfNewFile)
		{
			if (selectFileInsteadOfNewFile) {
				var files = Dialogs.ShowOpenFileDialog ("Select existing AudioGraph file as a master plugin");
				if (files.Any ())
					AddNewMasterPluginFile (files [0]);
			} else {
				var files = Dialogs.ShowSaveFileDialog ("New AudioGraph file as a master plugin");
				if (files.Any ()) {
					File.WriteAllText (files [0], JuceAudioGraph.EmptyAudioGraph);
					AddNewMasterPluginFile (files [0]);
				}
			}
		}
		public void AddNewMasterPluginFile (string filename)
		{
			Project.MasterPlugins.Add (GetItemFileRelativePath (filename));
			
			RefreshRequested?.Invoke ();
		}

		public void ProcessUnregisterMasterPluginFiles (IEnumerable<string> filesToUnregister)
		{
			var filesRemaining = Project.MasterPlugins.Where (f => !filesToUnregister.Contains (f)).ToArray ();
			Project.MasterPlugins.Clear ();
			Project.MasterPlugins.AddRange (filesRemaining);

			RefreshRequested?.Invoke ();
		}

		public void SetAutoReloadProject(bool value)
		{
			AutoReloadProject = value;

			UpdateAutoReloadSetup ();
		}

		public void SetAutoRecompileProject(bool value)
		{
			AutoCompileProject = value;
		}

		void UpdateAutoReloadSetup ()
		{
			Func<string, string, bool> cmp =
				(s1, s2) => s1 == s2;
			project_file_watcher.Changed += (o, e) => {
				if (!AutoReloadProject && !AutoCompileProject)
					return;
				var proj = ProjectFileName;
				if (proj == null)
					return;
				if (e.FullPath != ProjectFileName && Project.MmlFiles.All (m => !cmp(Path.Combine (Path.GetDirectoryName (proj), m), e.FullPath)))
					return;
				if (AutoReloadProject)
					ProcessLoadProjectFile (proj);
				
				if (AutoCompileProject)
					Compile ();
			};
		}

		private FileSystemWatcher project_file_watcher = new FileSystemWatcher();

		public void ProcessCompile ()
		{
			if (ProjectFileName == null)
				ProcessSaveProject ();
			if (ProjectFileName != null)
				Compile ();
		}

		public void ReportError (string errorId, string msg)
		{
			// FIXME: appropriate error reporting
			Console.Error.WriteLine ($"{errorId}: {msg}");
		}

		string ResolvePathRelativetoProject (string pathSpec)
		{
			return Path.Combine (Path.GetDirectoryName (Path.GetFullPath (ProjectFileName)), pathSpec);	
		}

		private const int TracktionProgramChange = 4097;
		
		public void Compile ()
		{
			if (ProjectFileName == null)
				throw new InvalidOperationException ("To compile the project, ProjectFileName must be specified in prior");

			Func<string, string> abspath = ResolvePathRelativetoProject;
			var compiler = new MmlCompiler ();
			var mmlFilesAbs = Project.MmlFiles.Select (_ => abspath (_)).ToArray ();
			var mmls = mmlFilesAbs.Select (f => new MmlInputSource (f, new StringReader (File.ReadAllText (f))))
				.Concat (Project.MmlStrings.Select (s =>
					new MmlInputSource ("(no file)", new StringReader (s))));
			var music = compiler.Compile (false, mmls.ToArray ());
			var edit = new EditElement ();
			var converter = new MidiToTracktionEditConverter (new MidiImportContext (music, edit));
			converter.ImportMusic ();
			var dstTracks = edit.Tracks.OfType<TrackElement> ().ToArray ();
			
			var audioGraphs = Project.AudioGraphsExpandedFullPath (abspath, null, null).ToArray ();
			
			// Assign numeric IDs to those unnamed tracks.
			for (int n = 0; n < dstTracks.Length; n++)
				if (edit.Tracks [n].Id == null)
					edit.Tracks [n].Id = (n + 1).ToString (CultureInfo.CurrentCulture);
			
			// Step 1: assign audio graphs by Bank Select and Program Change, if any.
			// Such a track must not contain more than one program change, bank select MSB and bank select LSB.
			foreach (var track in edit.Tracks.OfType<TrackElement> ()) {
				var programs = track.Clips.OfType<MidiClipElement> ()
					.Where (c => c.Sequence != null)
					.SelectMany (c => c.Sequence.Events.OfType<ControlElement> ()
						.Where (e => e.Type == TracktionProgramChange)).ToArray ();
				var banks = track.Clips.OfType<MidiClipElement> ()
					.Where (c => c.Sequence != null)
					.SelectMany (c => c.Sequence.Events.OfType<ControlElement> ()
						.Where (e => e.Type == MidiCC.BankSelect)).ToArray ();
				var bankLSBs = track.Clips.OfType<MidiClipElement> ()
					.Where (c => c.Sequence != null)
					.SelectMany (c => c.Sequence.Events.OfType<ControlElement> ()
						.Where (e => e.Type == MidiCC.BankSelectLsb)).ToArray ();
				if (programs.Length != 1 || banks.Length > 1 || bankLSBs.Length > 1)
					continue; // ignore
				var msb = (banks.FirstOrDefault ()?.Val / 128 ?? 0).ToString ();
				var lsb = (bankLSBs.FirstOrDefault ()?.Val / 128 ?? 0).ToString ();
				var program = (programs.First ().Val / 128).ToString ();
				var ag = audioGraphs.FirstOrDefault (a =>
					a.Program == program &&
					(a.BankMsb == msb || a.BankMsb == null && msb == "0") &&
					(a.BankLsb == lsb || a.BankLsb == null && lsb == "0"));
				if (ag != null) {
					var existingPlugins = track.Plugins.ToArray ();
					track.Plugins.Clear ();
					foreach (var p in ToTracktion (AugenePluginSpecifier.FromAudioGraph (
						JuceAudioGraph.Load (XmlReader.Create (abspath (ag.Source))))))
						track.Plugins.Add (p);
					// recover volume and level at the end.
					foreach (var p in existingPlugins)
						track.Plugins.Add (p);
				}
			}

			// Step 2: assign audio graphs by INSTRUMENTNAME (if named). It will overwrite bank mapping.
			foreach (var track in edit.Tracks.OfType<TrackElement> ()) {
				if (track.Extension_InstrumentName == null)
					continue;
				var existingPlugins = track.Plugins.ToArray ();
				track.Plugins.Clear ();
				var ag = audioGraphs.FirstOrDefault (a => a.Id == track.Extension_InstrumentName);
				if (ag != null)
					foreach (var p in ToTracktion (AugenePluginSpecifier.FromAudioGraph (
						JuceAudioGraph.Load (XmlReader.Create (abspath (ag.Source))))))
						track.Plugins.Add (p);
				// recover volume and level at the end.
				foreach (var p in existingPlugins)
					track.Plugins.Add (p);
			}

			// Step 3: assign audio graphs by TRACKNAME (if named). It will overwrite all above.
			foreach (var track in Project.Tracks) {
				var dstTrack = dstTracks.FirstOrDefault (t =>
					t.Id == track.Id);
				if (dstTrack == null)
					continue;
				var existingPlugins = dstTrack.Plugins.ToArray ();
				dstTrack.Plugins.Clear ();
				if (track.AudioGraph != null) {
					// track's AudioGraph may be either a ID reference or a filename.
					var ag = audioGraphs.FirstOrDefault (a => a.Id == track.AudioGraph);
					var agFile = ag?.Source ?? track.AudioGraph;
					if (!File.Exists (abspath (agFile))) {
						ReportError ("AugeneAudioGraphNotFound", "AudioGraph does not exist: " + abspath (agFile));
						continue;
					}
					foreach (var p in ToTracktion (AugenePluginSpecifier.FromAudioGraph (
							JuceAudioGraph.Load (XmlReader.Create (abspath (agFile))))))
						dstTrack.Plugins.Add (p);
				}
				// recover volume and level at the end.
				foreach (var p in existingPlugins)
					dstTrack.Plugins.Add (p);
			}

			foreach (var masterPlugin in Project.MasterPlugins) {
				// AudioGraph may be either a ID reference or a filename.
				var ag = audioGraphs.FirstOrDefault (a => a.Id == masterPlugin);
				var agFile = ag?.Source ?? masterPlugin;
				if (!File.Exists (abspath (agFile))) {
					ReportError ("AugeneAudioGraphNotFound", "AudioGraph does not exist: " + abspath (agFile));
					continue;
				}
				foreach (var p in ToTracktion (AugenePluginSpecifier.FromAudioGraph (
					JuceAudioGraph.Load (XmlReader.Create (abspath (agFile))))))
					edit.MasterPlugins.Add (p);
			}

			string outfile = OutputEditFileName ?? abspath (Path.ChangeExtension (Path.GetFileName (ProjectFileName), ".tracktionedit"));
			using (var sw = File.CreateText (outfile)) {
				new EditModelWriter ().Write (sw, edit);
				OutputEditFileName = outfile;
			}
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
				Volume = 1.0 // maybe? at least we have to avoid default 0.0
			});
		}

		public void ProcessPlay ()
		{
			if (string.IsNullOrWhiteSpace (ConfigAugenePlayerPath))
				Dialogs.ShowWarning ("AugenePlayer path is not configured [File > Configure].");
			else {
				ProcessCompile ();
				if (OutputEditFileName != null)
					Process.Start (ConfigAugenePlayerPath, OutputEditFileName);
			}
		}

		public void OpenFileOrContainingFolder (string fullPath)
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix) {
				if (IsRunningOnMac ())
					Process.Start ("open", fullPath);
				else
					Process.Start ("xdg-open", fullPath);
			}
			else
				Process.Start ("explorer", fullPath);
		}

		[DllImport ("libc")]
		static extern int uname (IntPtr buf);

		static bool IsRunningOnMac ()
		{
			IntPtr buf = IntPtr.Zero;
			try {
				buf = Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = Marshal.PtrToStringAnsi (buf);
					if (os == "Darwin")
						return true;
				}
			} catch {
			} finally {
				if (buf != IntPtr.Zero)
					Marshal.FreeHGlobal (buf);
			}
			return false;
		}
	}
}
