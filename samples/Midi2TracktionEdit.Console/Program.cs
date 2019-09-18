using System;
using System.IO;
using System.Linq;
using Commons.Music.Midi;
using NTracktive;
	
namespace Midi2TracktionEdit
{
	class MainClass
	{
		public static void Main (string [] args)
		{
			MidiImportContext ctx;
			switch (args.Length) {
			case 0:
				Console.Error.WriteLine (
					$"Usage: Midi2TracktionEdit [.tracktionedit] [.mid]");

				new EditModelWriter ().Write (Console.Out, EditModelTemplate.CreateNewEmptyEdit ());
				return;
			case 1:
				using (var s = File.OpenRead (args.First ())) {
					ctx = new MidiImportContext (
						MidiMusic.Read (s),
						EditModelTemplate.CreateNewEmptyEdit ());
				}

				break;
			default:
				ctx = new CommandArgumentContext
						{MidiFile = args [0], TracktionEditTemplateFile = args [1]}
					.CreateImportContext ();
				break;
			}

			var m2t = new MidiToTracktionEditConverter ();
			m2t.ImportMusic (ctx);
			new EditModelWriter ().Write (Console.Out, ctx.Edit);
		}
	}
}
