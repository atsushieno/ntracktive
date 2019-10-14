using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Commons.Music.Midi.Mml;
using Midi2TracktionEdit;
using NTracktive;

namespace Augene
{

	internal class Program
	{
		public static void Main (string [] args)
		{
			if (args.Contains ("-gui") || args.Contains ("--gui")) {
				GuiApplication.RunGui (args);
				return;
			}

			var serializer = new XmlSerializer (typeof (AugeneProject));
			var proj = new AugeneProject ();
			if (args.Length > 0) {
				proj = AugeneModel.Load (args [0]);
			}
			else {
				proj.MmlStrings.Add ("1 @0 V110 v100 o5 l8 cegcegeg  >c1");
				proj.Tracks.Add (new AugeneTrack
					{AudioGraph = "/home/atsushi/Desktop/Unnamed.filtergraph", Id = 1});
			}

			var memoryStream = new MemoryStream ();
			serializer.Serialize (memoryStream, proj);
			memoryStream.Position = 0;
			Console.Error.WriteLine (new StreamReader (memoryStream).ReadToEnd ());

			var compiler = new MmlCompiler ();
			var mmls = proj.MmlFiles.Select (filename =>
					new MmlInputSource (filename, new StringReader (File.ReadAllText (filename))))
				.Concat (proj.MmlStrings.Select (s =>
					new MmlInputSource ("(no file)", new StringReader (s))));
			var music = compiler.Compile (false, mmls.ToArray ());
			var converter = new MidiToTracktionEditConverter ();
			var edit = new EditElement ();
			converter.ImportMusic (new MidiImportContext (music, edit));
			var dstTracks = edit.Tracks.OfType<TrackElement> ().ToArray ();
			for (int n = 0; n < dstTracks.Length; n++)
				if (edit.Tracks [n].Id == null)
					edit.Tracks [n].Id = (n + 1).ToString (CultureInfo.CurrentCulture);
			foreach (var track in proj.Tracks) {
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
	}
}
