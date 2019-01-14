using System;
using System.Xml;
using NTracktive;
	
namespace Midi2TracktionEdit
{
	public class MidiToTracktionEdit
	{
		public void Process (string editTemplateFile, string midiFile)
		{
			using (var reader = new XmlTextReader (editTemplateFile) { Namespaces = false }) {
				var edit = new EditModelReader ().Read (reader);
				new EditModelWriter ().Write (Console.Out, edit);
			}
		}
	}
}
