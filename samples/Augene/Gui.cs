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
			
			// File
			var file = new MenuItem ("_File");
			file.SubMenu = new Menu ();
			menu.Items.Add (file);
			
			var open = new MenuItem ("_Open");
			file.SubMenu.Items.Add (open);
			open.Clicked += delegate { model.ProcessOpenProject (); };
			
			var save = new MenuItem ("_Save");
			file.SubMenu.Items.Add (save);
			save.Clicked += delegate { model.ProcessSaveProject (); };
			
			var config = new MenuItem("_Configure");
			file.SubMenu.Items.Add (config);
			config.Clicked += delegate { ProcessConfigure (); };
			
			var exit = new MenuItem ("E_xit");
			exit.Clicked += delegate { Application.Exit (); };
			file.SubMenu.Items.Add (exit);

			// Project
			var project = new MenuItem ("_Project");
			project.SubMenu = new Menu ();
			menu.Items.Add (project);

			var compile = new MenuItem ("_Compile");
			project.SubMenu.Items.Add (compile);
			compile.Clicked += delegate { model.ProcessCompile (); };

			var play = new MenuItem ("_Play");
			project.SubMenu.Items.Add (play);
			play.Clicked += delegate { model.ProcessPlay (); };
		}

		public AugeneWindow ()
		{
			Title = "Augene Project Tool";
			Width = 400;
			Height = 400;
			Closed += (o, e) => Application.Exit ();

			model = new AugeneModel (new XwtDialogs ());
			model.RefreshRequested += ResetContent;
			model.LoadConfiguration ();
			SetupMainMenu ();
			InitializeContent ();
		}

		class XwtDialogs : DialogAbstraction
		{
			public override void ShowWarning (string message)
			{
				MessageDialog.ShowWarning (message);
			}

			public override string [] ShowOpenFileDialog (string dialogTitle, DialogOptions options)
			{
				options = options ?? new DialogOptions ();
				var dlg = new OpenFileDialog (dialogTitle);
				dlg.Multiselect = options.MultipleFiles; 
				if (dlg.Run ())
					return dlg.FileNames;
				return new string [0];
			}

			public override string [] ShowSaveFileDialog (string dialogTitle, DialogOptions options)
			{
				options = options ?? new DialogOptions ();
				var dlg = new SaveFileDialog (dialogTitle);
				dlg.Multiselect = options.MultipleFiles; 
				if (dlg.Run ())
					return dlg.FileNames;
				return new string [0];
			}
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
				if (e.MultiplePress > 1 && trackListView.SelectedRows.Length == 1) {
					model.ProcessLaunchAudioPluginHost (listStore.GetValue (trackListView.SelectedRow,
						trackAudioGraphField));
				}
				if (e.Button == PointerButton.Right) {
					var contextMenu = new Menu ();
					
					var newTrackMenuItem = new MenuItem("New track");
					newTrackMenuItem.Clicked += delegate { model.ProcessNewTrack (false); };
					contextMenu.Items.Add (newTrackMenuItem);

					var newTrackWithFileMenuItem = new MenuItem("New track with existing AudioGraph");
					newTrackWithFileMenuItem.Clicked += delegate { model.ProcessNewTrack (true); };
					contextMenu.Items.Add (newTrackWithFileMenuItem);
					
					var deleteTracksMenuItem = new MenuItem("Delete selected track(s)");
					deleteTracksMenuItem.Clicked += delegate { DeleteSelectedTracks (); };
					deleteTracksMenuItem.Sensitive = trackListView.SelectedRows.Any ();

					contextMenu.Items.Add (deleteTracksMenuItem);

					contextMenu.Popup ();
				}
			};
			trackListView.KeyPressed += (o, e) => {
				if (e.Key == Key.Delete || e.Key == Key.BackSpace) {
					DeleteSelectedTracks ();
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

		void DeleteSelectedTracks ()
		{
			var trackListStore = (ListStore) trackListView.DataSource;
			var trackIds = new List<double> ();
			int [] rows = (int []) trackListView.SelectedRows.Clone ();
			foreach (var row in rows.Reverse ())
				trackIds.Add (trackListStore.GetValue (row, trackIdField));

			model.ProcessDeleteTracks (trackIds);
		}

		void ProcessConfigure ()
		{
			var dlg = new Dialog ();
			dlg.Width = 600;
			dlg.Height = 150;
			var vbox = new VBox ();
			dlg.Content = vbox;
			var pentry = new TextEntry { Text = model.ConfigPlaybackDemoPath };
			var aentry = new TextEntry { Text = model.ConfigAudioPluginHostPath };
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
			
			model.SaveConfiguration ();
		}

		readonly AugeneModel model;
		ListView trackListView;
		readonly DataField<double> trackIdField = new DataField<double> ();
		readonly DataField<string> trackAudioGraphField = new DataField<string> ();
		ListView mmlFileListView;
		readonly DataField<string> mmlFileField = new DataField<string> ();
	}
}
