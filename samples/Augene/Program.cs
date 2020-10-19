using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
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
		
		[STAThread]
		public static void Main (string [] args)
		{
			bool gui = false;
			string? audiopluginhost = null, augenePlayer = null;
			var rest = new Mono.Options.OptionSet ()
				.Add ("gui", s => gui = true) // backward compatibility (no need anymore)
				.Add ("audiopluginhost=", s => audiopluginhost = s)
				.Add ("augene-player=", (s, v) => augenePlayer = s)
				.Parse (args);
			var model = new AugeneModel {
				ConfigAudioPluginHostPath = audiopluginhost,
				ConfigAugenePlayerPath = augenePlayer
			};

			model.Dialogs = new ConsoleDialogs ();
			
			var serializer = new XmlSerializer (typeof (AugeneProject));
			model.Project = new AugeneProject ();
			if (rest.Any ()) {
				model.Project = AugeneProject.Load (rest.First ());
				model.ProjectFileName = rest.First ();
			}

			if (gui || !args.Any ()) {
				GuiApplication.RunGui (model);
				return;
			}
			model.Compile ();
		}
	}
}
