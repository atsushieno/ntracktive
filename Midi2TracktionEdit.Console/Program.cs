using System;
using System.IO;
using NTracktive;
	
namespace Midi2TracktionEdit
{
	class MainClass
	{
		public static void Main (string [] args)
		{
			if (args.Length < 2) {
				Console.Error.WriteLine ($"Usage: {Path.GetFileName (new Uri (typeof (MainClass).Assembly.CodeBase).LocalPath)} [.tracktionedit] [.mid]");

				new EditModelWriter ().Write (Console.Out, EditModelTemplate.CreateNewEmptyEdit ());
				return;
			}
			new MidiToTracktionEdit ().Process (args [0], args [1]);
		}
	}
}
