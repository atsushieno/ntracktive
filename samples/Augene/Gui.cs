using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xwt;

namespace Augene
{
	public class GuiApplication
	{
		public static void RunGui (AugeneModel model)
		{
			Application.Initialize ();
			new AugeneWindow (model).Show ();
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

			var autoReload = new CheckBoxMenuItem ("_Reload Project automatically");
			project.SubMenu.Items.Add (autoReload);
			autoReload.Clicked += delegate {
				model.SetAutoReloadProject (autoReload.Checked);
			};

			var autoRecompile = new CheckBoxMenuItem ("_Recompile Project automatically");
			project.SubMenu.Items.Add (autoRecompile);
			autoReload.Clicked += delegate {
				model.SetAutoRecompileProject (autoReload.Checked);
			};

			var compile = new MenuItem ("_Compile");
			project.SubMenu.Items.Add (compile);
			compile.Clicked += delegate { model.ProcessCompile (); };

			var play = new MenuItem ("_Play");
			project.SubMenu.Items.Add (play);
			play.Clicked += delegate { model.ProcessPlay (); };
		}

		public AugeneWindow (AugeneModel model)
		{
			Title = "Augene Project Tool";
			Width = 600;
			Height = 400;
			Closed += (o, e) => Application.Exit ();
			
			SetupMainMenu ();
			InitializeContent ();

			this.model = model;
			model.Dialogs = new XwtDialogs ();
			model.RefreshRequested += ResetContent;
			model.LoadConfiguration ();
			if (!string.IsNullOrWhiteSpace (model.LastProjectFile))
				model.ProcessLoadProjectFile (model.LastProjectFile!);
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
				dlg.CurrentFolder = options.InitialDirectory;
				if (dlg.Run ())
					return dlg.FileNames;
				return new string [0];
			}

			public override string [] ShowSaveFileDialog (string dialogTitle, DialogOptions options)
			{
				options = options ?? new DialogOptions ();
				var dlg = new SaveFileDialog (dialogTitle);
				dlg.Multiselect = options.MultipleFiles; 
				dlg.CurrentFolder = options.InitialDirectory;
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
			var trackListStore = new ListStore (trackIdField, trackAudioGraphField);
			trackListView.Columns.Add ("ID", trackIdField);
			trackListView.Columns.Add ("AudioGraph", trackAudioGraphField);
			trackListView.DataSource = trackListStore;
			trackListView.ButtonPressed += (o, e) => {
				if (e.MultiplePress > 1 && trackListView.SelectedRows.Length == 1) {
					model.ProcessLaunchAudioPluginHost (GetSelectedAudioGraphFilePathFromTracks ());
				}
				if (e.Button == PointerButton.Right) {
					var contextMenu = new Menu ();
					
					var newTrackMenuItem = new MenuItem("New track");
					newTrackMenuItem.Clicked += delegate { model.ProcessNewTrack (false); };
					contextMenu.Items.Add (newTrackMenuItem);

					var newTrackWithFileMenuItem = new MenuItem("New track with existing AudioGraph");
					newTrackWithFileMenuItem.Clicked += delegate { model.ProcessNewTrack (true); };
					contextMenu.Items.Add (newTrackWithFileMenuItem);

					var openFile = new MenuItem("Open with AudioPluginHost"); // same as double click
					openFile.Clicked += delegate { model.ProcessLaunchAudioPluginHost (GetSelectedAudioGraphFilePathFromTracks ()); };
					openFile.Sensitive = trackListView.SelectedRows.Length == 1;
					contextMenu.Items.Add (openFile);

					var openContaininfFolder = new MenuItem("Open containing folder");
					openContaininfFolder.Clicked += delegate { model.OpenFileOrContainingFolder (Path.GetDirectoryName (GetSelectedAudioGraphFilePathFromTracks ())); };
					openContaininfFolder.Sensitive = trackListView.SelectedRows.Length == 1;
					contextMenu.Items.Add (openContaininfFolder);
					
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
			
			// audioGraphListView
			var audioGraphBox = new VBox ();

			audioGraphListView = new ListView ();
			var audioGraphListStore = new ListStore (audioGraphIdField, audioGraphSourceField);
			audioGraphListView.Columns.Add ("ID", audioGraphIdField);
			audioGraphListView.Columns.Add ("Source", audioGraphSourceField);
			audioGraphListView.DataSource = audioGraphListStore;
			audioGraphListView.ButtonPressed += (o, e) => {
				if (e.MultiplePress > 1 && audioGraphListView.SelectedRows.Length == 1) {
					model.ProcessLaunchAudioPluginHost (GetSelectedAudioGraphFilePathFromAudioGraphs ());
				}
				if (e.Button == PointerButton.Right) {
					var contextMenu = new Menu ();
					
					var newAudioGraphMenuItem = new MenuItem("New audio graph");
					newAudioGraphMenuItem.Clicked += delegate { model.ProcessNewAudioGraph (false); };
					contextMenu.Items.Add (newAudioGraphMenuItem);

					var newAudioGraphWithFileMenuItem = new MenuItem("New audio graph with existing AudioGraph");
					newAudioGraphWithFileMenuItem.Clicked += delegate { model.ProcessNewAudioGraph (true); };
					contextMenu.Items.Add (newAudioGraphWithFileMenuItem);

					var openFile = new MenuItem("Open with AudioPluginHost"); // same as double click
					openFile.Clicked += delegate { model.ProcessLaunchAudioPluginHost (GetSelectedAudioGraphFilePathFromAudioGraphs ()); };
					openFile.Sensitive = audioGraphListView.SelectedRows.Length == 1;
					contextMenu.Items.Add (openFile);

					var openContaininfFolder = new MenuItem("Open containing folder");
					openContaininfFolder.Clicked += delegate { model.OpenFileOrContainingFolder (Path.GetDirectoryName (GetSelectedAudioGraphFilePathFromAudioGraphs ())); };
					openContaininfFolder.Sensitive = audioGraphListView.SelectedRows.Length == 1;
					contextMenu.Items.Add (openContaininfFolder);
					
					var deleteAudioGraphsMenuItem = new MenuItem("Delete selected audio graph(s)");
					deleteAudioGraphsMenuItem.Clicked += delegate { DeleteSelectedTracks (); };
					deleteAudioGraphsMenuItem.Sensitive = audioGraphListView.SelectedRows.Any ();
					contextMenu.Items.Add (deleteAudioGraphsMenuItem);

					contextMenu.Popup ();
				}
			};
			audioGraphListView.KeyPressed += (o, e) => {
				if (e.Key == Key.Delete || e.Key == Key.BackSpace) {
					DeleteSelectedAudioGraphs ();
					e.Handled = true;
				}
			};

			audioGraphBox.PackStart (audioGraphListView, true);

			hbox.PackStart (audioGraphBox, true);

			// mmlFileListView
			var mmlFileBox = new VBox ();

			mmlFileListView = new ListView ();
			var mmlListStore = new ListStore (mmlFileField);
			mmlFileListView.Columns.Add ("mugene MML file", mmlFileField);
			mmlFileListView.DataSource = mmlListStore;
			mmlFileListView.ButtonPressed += (o, e) => {
				if (e.MultiplePress > 1 && mmlFileListView.SelectedRows.Length == 1) {
					model.OpenFileOrContainingFolder (GetSelectedMmlFilePath ());
				}
				if (e.Button == PointerButton.Right) {
					var contextMenu = new Menu ();
					
					var newMmlMenuItem = new MenuItem("New MML");
					newMmlMenuItem.Clicked += delegate { model.ProcessNewMmlFile (false); };
					contextMenu.Items.Add (newMmlMenuItem);

					var addExistingMml = new MenuItem("Add existing MML");
					addExistingMml.Clicked += delegate { model.ProcessNewMmlFile (true); };
					contextMenu.Items.Add (addExistingMml);

					var openFile = new MenuItem("Open MML"); // same as double click
					openFile.Clicked += delegate { model.OpenFileOrContainingFolder (GetSelectedMmlFilePath ()); };
					openFile.Sensitive = mmlFileListView.SelectedRows.Length == 1;
					contextMenu.Items.Add (openFile);

					var openContaininfFolder = new MenuItem("Open containing folder");
					openContaininfFolder.Clicked += delegate { model.OpenFileOrContainingFolder (Path.GetDirectoryName (GetSelectedMmlFilePath ())); };
					openContaininfFolder.Sensitive = mmlFileListView.SelectedRows.Length == 1;
					contextMenu.Items.Add (openContaininfFolder);
					
					var unregisterMmlMenuItem = new MenuItem("Remove selected MML(s) from project");
					unregisterMmlMenuItem.Clicked += delegate { UnregisterSelectedMmlFiles (); };
					unregisterMmlMenuItem.Sensitive = mmlFileListView.SelectedRows.Any ();
					contextMenu.Items.Add (unregisterMmlMenuItem);

					contextMenu.Popup ();
				}
			};
			mmlFileListView.KeyPressed += (o, e) => {
				if (e.Key == Key.Delete || e.Key == Key.BackSpace) {
					UnregisterSelectedMmlFiles ();
					e.Handled = true;
				}
			};
			
			mmlFileBox.PackStart (mmlFileListView, true);
			
			hbox.PackStart (mmlFileBox, true);
			
			var masterPluginListBox = new VBox ();

			masterPluginListView = new ListView ();
			var masterPluginListStore = new ListStore (masterPluginFileField);
			masterPluginListView.Columns.Add ("Master plugins", masterPluginFileField);
			masterPluginListView.DataSource = masterPluginListStore;
			masterPluginListView.ButtonPressed += (o, e) => {
				if (e.MultiplePress > 1 && masterPluginListView.SelectedRows.Length == 1) {
					model.ProcessLaunchAudioPluginHost (GetSelectedMasterPluginFilePath ());
				}
				if (e.Button == PointerButton.Right) {
					var contextMenu = new Menu ();
					
					var newItem = new MenuItem("New master plugin");
					newItem.Clicked += delegate { model.ProcessNewMasterPluginFile (false); };
					contextMenu.Items.Add (newItem);

					var addExisting = new MenuItem("Add existing AudioGraph as master plugin");
					addExisting.Clicked += delegate { model.ProcessNewMasterPluginFile (true); };
					contextMenu.Items.Add (addExisting);

					var openFile = new MenuItem("Open master plugin"); // same as double click
					openFile.Clicked += delegate { model.ProcessLaunchAudioPluginHost (GetSelectedMasterPluginFilePath ()); };
					openFile.Sensitive = masterPluginListView.SelectedRows.Length == 1;
					contextMenu.Items.Add (openFile);

					var openContaininfFolder = new MenuItem("Open containing folder");
					openContaininfFolder.Clicked += delegate { model.OpenFileOrContainingFolder (Path.GetDirectoryName (GetSelectedMasterPluginFilePath ())); };
					openContaininfFolder.Sensitive = masterPluginListView.SelectedRows.Length == 1;
					contextMenu.Items.Add (openContaininfFolder);
					
					var unregister = new MenuItem("Remove selected master plugin(s) from project");
					unregister.Clicked += delegate { UnregisterSelectedMasterPluginFiles (); };
					unregister.Sensitive = masterPluginListView.SelectedRows.Any ();
					contextMenu.Items.Add (unregister);

					contextMenu.Popup ();
				}
			};
			masterPluginListView.KeyPressed += (o, e) => {
				if (e.Key == Key.Delete || e.Key == Key.BackSpace) {
					UnregisterSelectedMasterPluginFiles ();
					e.Handled = true;
				}
			};
			
			masterPluginListBox.PackStart (masterPluginListView, true);
			
			hbox.PackStart (masterPluginListBox, true);
			
			Content = hbox;
		}

		void ResetContent ()
		{
			var trackListStore = (ListStore) trackListView.DataSource;
			trackListStore.Clear ();
			var audioGraphListStore = (ListStore) audioGraphListView.DataSource;
			audioGraphListStore.Clear ();
			var mmlListStore = (ListStore) mmlFileListView.DataSource;
			mmlListStore.Clear ();
			var masterPluginListStore = (ListStore) masterPluginListView.DataSource;
			masterPluginListStore.Clear ();
			
			foreach (var track in model.Project.Tracks) {
				int idx = trackListStore.AddRow ();
				trackListStore.SetValues (idx, trackIdField, track.Id, trackAudioGraphField,
					track.AudioGraph);
			}
			foreach (var graph in model.Project.AudioGraphs) {
				int idx = audioGraphListStore.AddRow ();
				audioGraphListStore.SetValues (idx, audioGraphIdField, graph.Id, audioGraphSourceField,
					graph.Source);
			}
			foreach (var mmlFile in model.Project.MmlFiles) {
				int idx = mmlListStore.AddRow ();
				mmlListStore.SetValue (idx, mmlFileField, mmlFile);
			}
			foreach (var masterPluginFile in model.Project.MasterPlugins) {
				int idx = masterPluginListStore.AddRow ();
				masterPluginListStore.SetValue (idx, masterPluginFileField, masterPluginFile);
			}
		}

		void DeleteSelectedTracks ()
		{
			var trackListStore = (ListStore) trackListView.DataSource;
			var trackIds = new List<string> ();
			int [] rows = (int []) trackListView.SelectedRows.Clone ();
			foreach (var row in rows.Reverse ())
				trackIds.Add (trackListStore.GetValue (row, trackIdField));

			model.ProcessDeleteTracks (trackIds);
		}

		void DeleteSelectedAudioGraphs ()
		{
			var audioGraphListStore = (ListStore) audioGraphListView.DataSource;
			var graphIds = new List<string> ();
			int [] rows = (int []) audioGraphListView.SelectedRows.Clone ();
			foreach (var row in rows.Reverse ())
				graphIds.Add (audioGraphListStore.GetValue (row, trackIdField));

			model.ProcessDeleteAudioGraphs (graphIds);
		}

		void UnregisterSelectedMmlFiles ()
		{
			var mmlListStore = (ListStore) mmlFileListView.DataSource;
			var mmlFiles = new List<string> ();
			int [] rows = (int []) mmlFileListView.SelectedRows.Clone ();
			foreach (var row in rows.Reverse ())
				mmlFiles.Add (mmlListStore.GetValue (row, mmlFileField));

			model.ProcessUnregisterMmlFiles (mmlFiles);
		}

		void UnregisterSelectedMasterPluginFiles ()
		{
			var masterPluginListStore = (ListStore) masterPluginListView.DataSource;
			var masterPluginFiles = new List<string> ();
			int [] rows = (int []) masterPluginListView.SelectedRows.Clone ();
			foreach (var row in rows.Reverse ())
				masterPluginFiles.Add (masterPluginListStore.GetValue (row, masterPluginFileField));

			model.ProcessUnregisterMasterPluginFiles (masterPluginFiles);
		}

		void ProcessConfigure ()
		{
			var dlg = new Dialog ();
			dlg.Width = 600;
			dlg.Height = 150;
			var vbox = new VBox ();
			dlg.Content = vbox;
			var pentry = new TextEntry { Text = model.ConfigAugenePlayerPath };
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
			f ("Path to AugenePlayer: ", pentry);
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
			model.ConfigAugenePlayerPath = pentry.Text;
			model.ConfigAudioPluginHostPath = aentry.Text;
			
			model.SaveConfiguration ();
		}

		string GetSelectedMmlFilePath ()
		{
			var lv = mmlFileListView;
			return model.GetItemFileAbsolutePath (
				(string) lv.DataSource.GetValue (lv.SelectedRow, mmlFileField.Index));
		}

		string GetSelectedAudioGraphFilePathFromTracks ()
		{
			var lv = trackListView;
			return model.GetItemFileAbsolutePath (
				(string) lv.DataSource.GetValue (lv.SelectedRow, trackAudioGraphField.Index));
		}

		string GetSelectedAudioGraphFilePathFromAudioGraphs ()
		{
			var lv = audioGraphListView;
			return model.GetItemFileAbsolutePath (
				(string) lv.DataSource.GetValue (lv.SelectedRow, audioGraphSourceField.Index));
		}

		string GetSelectedMasterPluginFilePath ()
		{
			var lv = masterPluginListView;
			return model.GetItemFileAbsolutePath (
				(string) lv.DataSource.GetValue (lv.SelectedRow, masterPluginFileField.Index));
		}
		
		readonly AugeneModel model;
		ListView trackListView;
		readonly DataField<string> trackIdField = new DataField<string> ();
		readonly DataField<string> trackAudioGraphField = new DataField<string> ();
		ListView audioGraphListView;
		readonly DataField<string> audioGraphIdField = new DataField<string> ();
		readonly DataField<string> audioGraphSourceField = new DataField<string> ();
		ListView mmlFileListView;
		readonly DataField<string> mmlFileField = new DataField<string> ();
		private ListView masterPluginListView;
		readonly DataField<string> masterPluginFileField = new DataField<string> ();
	}
}
