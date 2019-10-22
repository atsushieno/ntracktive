using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
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
		const string ConfigXmlFile = "augene-config.xml";
		
		public AugeneProject Project { get; set; }
		public string ProjectFileName { get; set; }
		
		public string OutputEditFileName { get; set; }

		public string ConfigAudioPluginHostPath { get; set; }

		public string ConfigAugenePlayerPath { get; set; }

		public string LastProjectFile { get; set; }

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

		public event Action RefreshRequested;

		public string GetItemFileRelativePath (string itemFilename)
		{
			string filenameRelative = itemFilename;
			if (ProjectFileName != null)
				filenameRelative = new Uri (ProjectFileName).MakeRelative (new Uri (itemFilename));
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
			Project = AugeneProject.Load (file);
			ProjectFileName = file;
			LastProjectFile = ProjectFileName;
			// FIXME: it is kind of hack, but so far we unify history with config.
			SaveConfiguration ();

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
			AugeneProject.Save (Project, ProjectFileName);
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
			int newTrackId = 1 + (int) Project.Tracks.Select (t => t.Id).Max ();
			Project.Tracks.Add (new AugeneTrack {Id = newTrackId, AudioGraph = GetItemFileRelativePath (filename)});
			
			RefreshRequested?.Invoke ();
		}

		public void ProcessDeleteTracks (IEnumerable<double> trackIdsToRemove)
		{
			var tracksRemaining = Project.Tracks.Where (t => !trackIdsToRemove.Contains (t.Id)).ToArray ();
			Project.Tracks.Clear ();
			Project.Tracks.AddRange (tracksRemaining);

			RefreshRequested?.Invoke ();
		}

		public void ProcessNewMmlFile (bool selectFileInsteadOfNewFile)
		{
			if (selectFileInsteadOfNewFile) {
				var files = Dialogs.ShowOpenFileDialog ("Select existing MML file");
				if (files.Any ())
					AddNewMmlFile (files [0]);
			} else {
				var files = Dialogs.ShowSaveFileDialog ("New MML file");
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
					File.WriteAllText (files [0], AudioGraph.EmptyAudioGraph);
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

		public void ProcessCompile ()
		{
			if (ProjectFileName == null)
				ProcessSaveProject ();
			if (ProjectFileName != null)
				Compile ();
		}
		
		public void Compile ()
		{
			if (ProjectFileName == null)
				throw new InvalidOperationException ("To compile the project, ProjectFileName must be specified in prior");
			Func<string, string> abspath = src => Path.Combine (Path.GetDirectoryName (Path.GetFullPath (ProjectFileName)), src);
			var compiler = new MmlCompiler ();
			var mmlFilesAbs = Project.MmlFiles.Select (_ => abspath (_)).ToArray ();
			var mmls = mmlFilesAbs.Select (f => new MmlInputSource (f, new StringReader (File.ReadAllText (f))))
				.Concat (Project.MmlStrings.Select (s =>
					new MmlInputSource ("(no file)", new StringReader (s))));
			var music = compiler.Compile (false, mmls.ToArray ());
			var converter = new MidiToTracktionEditConverter ();
			var edit = new EditElement ();
			converter.ImportMusic (new MidiImportContext (music, edit));
			var dstTracks = edit.Tracks.OfType<TrackElement> ().ToArray ();
			for (int n = 0; n < dstTracks.Length; n++)
				if (edit.Tracks [n].Id == null)
					edit.Tracks [n].Id = (n + 1).ToString (CultureInfo.CurrentCulture);
			foreach (var track in Project.Tracks) {
				var dstTrack = dstTracks.FirstOrDefault (t =>
					t.Id == track.Id.ToString (CultureInfo.CurrentCulture));
				if (dstTrack == null)
					continue;
				var existingPlugins = dstTrack.Plugins.ToArray ();
				dstTrack.Plugins.Clear ();
				foreach (var p in ToTracktion (AugenePluginSpecifier.FromAudioGraph (
					AudioGraph.Load (XmlReader.Create (abspath (track.AudioGraph))))))
					dstTrack.Plugins.Add (p);
				// recover volume and level at the end.
				foreach (var p in existingPlugins)
					dstTrack.Plugins.Add (p);
			}
			foreach (var masterPlugin in Project.MasterPlugins)
				foreach (var p in ToTracktion (AugenePluginSpecifier.FromAudioGraph (
					AudioGraph.Load (XmlReader.Create (abspath (masterPlugin))))))
					edit.MasterPlugins.Add (p);

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
			});
		}

		public void ProcessPlay ()
		{
			if (string.IsNullOrWhiteSpace (ConfigAugenePlayerPath))
				Dialogs.ShowWarning ("augene-player path is not configured [File > Configure].");
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
