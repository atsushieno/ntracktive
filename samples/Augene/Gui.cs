using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xwt;

namespace Augene
{
	public class GuiApplication
	{
		public static void RunGui (string [] args)
		{
			Application.Initialize ();
			new AugeneWindow ().Show ();
			Application.Run ();
		}
	}

	public class AugeneWindow : Window
	{
		void SetupMainMenu ()
		{
			var menu = new Menu ();
			MainMenu = menu;
			var file = new MenuItem ("_File");
			file.SubMenu = new Menu ();
			menu.Items.Add (file);
			
			var open = new MenuItem ("_Open");
			file.SubMenu.Items.Add (open);
			open.Clicked += delegate { ProcessOpenProject (); };
			
			var save = new MenuItem ("_Save");
			file.SubMenu.Items.Add (save);
			save.Clicked += delegate { ProcessSaveProject (); };
			
			var config = new MenuItem("_Configure");
			file.SubMenu.Items.Add (config);
			config.Clicked += delegate { ProcessConfigure (); };
			
			var exit = new MenuItem ("E_xit");
			exit.Clicked += delegate { Application.Exit (); };
			file.SubMenu.Items.Add (exit);
		}

		public AugeneWindow ()
		{
			Title = "Augene Project Tool";
			Width = 400;
			Height = 400;
			Closed += (o, e) => Application.Exit ();

			SetupMainMenu ();
			InitializeContent ();
		}
		
		void InitializeContent ()
		{
			var hbox = new HBox ();
			var trackBox = new VBox ();

			trackListView = new ListView ();
			var listStore = new ListStore (trackIdField, trackAudioGraphField);
			trackListView.Columns.Add ("ID", trackIdField);
			trackListView.Columns.Add ("AudioGraph", trackAudioGraphField);
			trackListView.DataSource = listStore;
			trackListView.ButtonPressed += (o, e) => {
				if (e.MultiplePress > 1) {
					
				}
				if (e.Button == PointerButton.Right) {
					var contextMenu = new Menu ();
					
					var newTrackMenuItem = new MenuItem("New track");
					newTrackMenuItem.Clicked += delegate { ProcessNewTrack (false); };
					contextMenu.Items.Add (newTrackMenuItem);

					var newTrackWithFileMenuItem = new MenuItem("New track with existing AudioGraph");
					newTrackWithFileMenuItem.Clicked += delegate { ProcessNewTrack (true); };
					contextMenu.Items.Add (newTrackWithFileMenuItem);
					
					var deleteTracksMenuItem = new MenuItem("Delete selected track(s)");
					deleteTracksMenuItem.Clicked += delegate { ProcessDeleteTracks (); };
					deleteTracksMenuItem.Sensitive = trackListView.SelectedRows.Any ();

					contextMenu.Items.Add (deleteTracksMenuItem);

					contextMenu.Popup ();
				}
			};
			trackListView.KeyPressed += (o, e) => {
				if (e.Key == Key.Delete || e.Key == Key.BackSpace) {
					ProcessDeleteTracks ();
					e.Handled = true;
				}
			};

			trackBox.PackStart (trackListView, true);

			hbox.PackStart (trackBox, true);

			var mmlFileBox = new VBox ();

			mmlFileListView = new ListView ();
			var mmlListStore = new ListStore (mmlFileField);
			mmlFileListView.Columns.Add ("mugene MML file", mmlFileField);
			mmlFileListView.DataSource = mmlListStore;
			
			mmlFileBox.PackStart (mmlFileListView, true);
			
			hbox.PackStart (mmlFileBox, true);

			Content = hbox;
		}

		void ResetContent ()
		{
			var trackListStore = (ListStore) trackListView.DataSource;
			trackListStore.Clear ();
			var mmlListStore = (ListStore) mmlFileListView.DataSource;
			mmlListStore.Clear ();
			
			foreach (var track in model.Project.Tracks) {
				int idx = trackListStore.AddRow ();
				trackListStore.SetValues (idx, trackIdField, track.Id, trackAudioGraphField,
					track.AudioGraph);
			}
			foreach (var mmlFile in model.Project.MmlFiles) {
				int idx = mmlListStore.AddRow ();
				mmlListStore.SetValue (idx, mmlFileField, mmlFile);
			}
		}

		void ProcessOpenProject ()
		{
			var dlg = new OpenFileDialog ("Open Augene Project");
			if (dlg.Run ()) {
				model.Project = AugeneModel.Load (dlg.FileName);
				model.ProjectFileName = dlg.FileName;

				ResetContent ();
			}
		}

		void ProcessSaveProject ()
		{
			if (model.ProjectFileName == null) {
				var dlg = new SaveFileDialog("Save Augene Project");
				if (dlg.Run ()) {
					model.ProjectFileName = dlg.FileName;
				}
				else
					return;
			}
			AugeneModel.Save (model.Project, model.ProjectFileName);
		}

		void ProcessNewTrack (bool selectFileInsteadOfNewFile)
		{
			if (selectFileInsteadOfNewFile) {
				var dlg = new OpenFileDialog ("Select existing AudioGraph file for a new track");
				if (dlg.Run ())
					AddNewTrack (dlg.FileName);
			} else {
				var dlg = new SaveFileDialog ("New AudioGraph file for a new track");
				if (dlg.Run ()) {
					File.WriteAllText (dlg.FileName, AudioGraph.EmptyAudioGraph);
					AddNewTrack (dlg.FileName);
				}
			}
		}

		void AddNewTrack (string filename)
		{
			string filenameRelative = filename;
			if (model.ProjectFileName != null)
				filenameRelative = new Uri (model.ProjectFileName).MakeRelative (new Uri (filename)); 
			int newTrackId = 1 + (int) model.Project.Tracks.Select (t => t.Id).Max ();
			model.Project.Tracks.Add (new AugeneTrack
				{Id = newTrackId, AudioGraph = filenameRelative});

			ResetContent ();
		}

		void ProcessDeleteTracks ()
		{
			var trackListStore = (ListStore) trackListView.DataSource;
			var trackIds = new List<double> ();
			int [] rows = (int []) trackListView.SelectedRows.Clone ();
			foreach (var row in rows.Reverse ()) {
				trackIds.Add (trackListStore.GetValue (row, trackIdField));
			}

			var tracksRemaining = model.Project.Tracks.Where (t => !trackIds.Contains (t.Id)).ToArray ();
			model.Project.Tracks.Clear ();
			model.Project.Tracks.AddRange (tracksRemaining);
			
			ResetContent ();
		}

		void ProcessConfigure ()
		{
			var dlg = new Dialog ();
			dlg.Width = 600;
			dlg.Height = 150;
			var vbox = new VBox ();
			dlg.Content = vbox;
			var pentry = new TextEntry ();
			var aentry = new TextEntry ();
			Action<string, TextEntry> f = (label, entry) => {
				var box = new HBox ();
				box.PackStart (new Label(label));
				box.PackStart (entry, true);
				var button = new Button ("Select");
				button.Clicked += delegate {
					var dialog = new OpenFileDialog ();
					if (!dialog.Run ())
						return;
					entry.Text = dialog.FileName;
				};
				box.PackStart (button);
				vbox.PackStart (box);
			};
			f ("Path to PlaybackDemo: ", pentry);
			f ("Path to AudioPluginHost: ", aentry);
			var ok = new Button ("OK");
			ok.Clicked += delegate { dlg.Respond (Command.Ok); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += delegate { dlg.Respond (Command.Cancel); };
			var hcommit = new HBox ();
			vbox.PackStart (hcommit);
			hcommit.PackEnd (cancel);
			hcommit.PackEnd (ok);
			if (dlg.Run () == Command.Cancel)
				return;
			model.ConfigPlaybackDemoPath = pentry.Text;
			model.ConfigAudioPluginHostPath = aentry.Text;
			
			using (var fs = IsolatedStorageFile.GetUserStoreForAssembly ()) {
				using (var file = fs.CreateFile (ConfigXmlFile)) {
					using (var xw = XmlWriter.Create (file)) {
						xw.WriteStartElement ("config");
						xw.WriteElementString ("PlaybackDemo",
							model.ConfigPlaybackDemoPath);
						xw.WriteElementString ("AudioPluginHost",
							model.ConfigAudioPluginHostPath);
					}
				}
			}
		}

		const string ConfigXmlFile = "augene-config.xml";
		AugeneModel model = new AugeneModel ();
		ListView trackListView;
		DataField<double> trackIdField = new DataField<double> ();
		DataField<string> trackAudioGraphField = new DataField<string> ();
		ListView mmlFileListView;
		DataField<string> mmlFileField = new DataField<string> ();
	}
}
