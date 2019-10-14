using System;
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
		public AugeneWindow ()
		{
			Title = "Augene Project Tool";
			Width = 400;
			Height = 400;
			Closed += (o, e) => Application.Exit ();

			var menu = new Menu ();
			MainMenu = menu;
			var file = new MenuItem ("_File");
			file.SubMenu = new Menu ();
			menu.Items.Add (file);
			
			var fileOpen = new MenuItem ("_Open");
			file.SubMenu.Items.Add (fileOpen);
			fileOpen.Clicked += delegate { ProcessOpenProject (); };
			
			var fileSave = new MenuItem ("_Save");
			file.SubMenu.Items.Add (fileSave);
			fileSave.Clicked += delegate { ProcessSaveProject (); };
			
			var fileExit = new MenuItem ("E_xit");
			fileExit.Clicked += delegate { Application.Exit (); };
			file.SubMenu.Items.Add (fileExit);

			var hbox = new HBox ();
			var trackBox = new VBox ();

			trackListView = new ListView ();
			var listStore = new ListStore (trackIdField, trackAudioGraphField);
			trackListView.Columns.Add ("ID", trackIdField);
			trackListView.Columns.Add ("AudioGraph", trackAudioGraphField);
			trackListView.DataSource = listStore;

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

		void ProcessOpenProject ()
		{
			var dlg = new OpenFileDialog ("Open Augene Project");
			if (dlg.Run ()) {
				model.Project = AugeneModel.Load (dlg.FileName);
				model.ProjectFileName = dlg.FileName;
				var trackListStore = (ListStore) trackListView.DataSource;
				foreach (var track in model.Project.Tracks) {
					int idx = trackListStore.AddRow ();
					trackListStore.SetValues (idx, trackIdField, track.Id, trackAudioGraphField,
						track.AudioGraph);
				}
				var mmlListStore = (ListStore) mmlFileListView.DataSource;
				foreach (var mmlFile in model.Project.MmlFiles) {
					int idx = mmlListStore.AddRow ();
					mmlListStore.SetValue (idx, mmlFileField, mmlFile);
				}
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

		AugeneModel model = new AugeneModel ();
		ListView trackListView;
		DataField<double> trackIdField = new DataField<double> ();
		DataField<string> trackAudioGraphField = new DataField<string> ();
		ListView mmlFileListView;
		DataField<string> mmlFileField = new DataField<string> ();
	}
}
