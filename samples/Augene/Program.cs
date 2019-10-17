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
		class ConsoleDialogs : DialogAbstraction
		{
			public override void ShowWarning (string message)
			{
				Console.Error.WriteLine (message);
			}

			string [] EnterFileNames (bool acceptMultiple)
			{
				if (!acceptMultiple)
					return new string [] {Console.ReadLine ()};
				Console.Error.WriteLine ("Enter multiple filenames, and blank line to finish.");
				var list = new List<string> ();
				do {
					string s = Console.ReadLine ();
					if (s.Length == 0)
						break;
					list.Add (s);
				} while (true);
				return list.ToArray ();
			}

			public override string [] ShowOpenFileDialog (string dialogTitle, DialogOptions options)
			{
				Console.Error.WriteLine (dialogTitle);
				return EnterFileNames (options.MultipleFiles);
			}

			public override string [] ShowSaveFileDialog (string dialogTitle, DialogOptions options)
			{
				Console.Error.WriteLine (dialogTitle);
				return EnterFileNames (options.MultipleFiles);
			}
		}
		
		public static void Main (string [] args)
		{
			if (args.Contains ("-gui") || args.Contains ("--gui")) {
				GuiApplication.RunGui (args);
				return;
			}
			
			var model = new AugeneModel (new ConsoleDialogs ());

			var serializer = new XmlSerializer (typeof (AugeneProject));
			model.Project = new AugeneProject ();
			if (args.Length > 0) {
				model.Project = AugeneProject.Load (args [0]);
				model.ProjectFileName = args [0];
			}
			else {
				model.Project.MmlFiles.Add ("/sources/commons-music-prog/ntractive/samples/Augene/samples/sample.mugene");
				model.Project.MmlStrings.Add ("1 @0 V110 v100 o5 l8 cegcegeg  >c1");
				model.Project.Tracks.Add (new AugeneTrack
					{AudioGraph = "/home/atsushi/Desktop/Unnamed.filtergraph", Id = 1});
				model.ProjectFileName = Path.Combine (Directory.GetCurrentDirectory (), "dummy.augene");
			}

			// dump project content.
			var memoryStream = new MemoryStream ();
			serializer.Serialize (memoryStream, model.Project);
			memoryStream.Position = 0;
			Console.Error.WriteLine (new StreamReader (memoryStream).ReadToEnd ());

			model.Compile ();
		}
	}
}
