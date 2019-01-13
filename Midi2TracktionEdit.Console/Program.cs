using System;
using System.IO;

namespace Midi2TracktionEdit
{
	class MainClass
	{
		public static void Main (string [] args)
		{
			if (args.Length < 2) {
				Console.WriteLine ($"Usage: {Path.GetFileName (new Uri (typeof (MainClass).Assembly.CodeBase).LocalPath)} [.tracktionedit] [.mid]");
				return;
			}
			new MidiToTracktionEdit ().Process (args [0], args [1]);
		}
	}
}
