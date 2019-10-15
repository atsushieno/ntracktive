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

			// dump project content.
			var memoryStream = new MemoryStream ();
			serializer.Serialize (memoryStream, proj);
			memoryStream.Position = 0;
			Console.Error.WriteLine (new StreamReader (memoryStream).ReadToEnd ());

			AugeneModel.Compile (proj);
		}
	}
}
