using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
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
		public void Compile ()
		{
			Func<string, string> abspath = (src) => Path.Combine (Path.GetDirectoryName (Path.GetFullPath (ProjectFileName)), src);
			var compiler = new MmlCompiler ();
			var mmlFilesAbs = Project.MmlFiles.Select (filename => abspath (filename)).ToArray ();
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

			string outfile = OutputEditFileName ?? Path.ChangeExtension (abspath (ProjectFileName), ".tracktionedit");
			using (var sw = File.CreateText (outfile))
				new EditModelWriter ().Write (sw, edit);
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
		
		public string OutputEditFileName { get; set; }
		
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
			Project = AugeneProject.Load (files [0]);
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
					Path.Combine (Path.GetDirectoryName (ProjectFileName), audioGraphFile));
			}
		}

		public void ProcessCompile ()
		{
			if (ProjectFileName == null)
				ProcessSaveProject ();
			if (ProjectFileName != null)
				Compile ();
		}

		public void ProcessPlay ()
		{
			if (string.IsNullOrWhiteSpace (ConfigPlaybackDemoPath))
				Dialogs.ShowWarning ("PlaybackDemo path is not configured [File > Configure].");
			else {
				ProcessCompile ();
				if (OutputEditFileName != null)
					Process.Start (ConfigPlaybackDemoPath, ProjectFileName);
			}
		}
	}
}
