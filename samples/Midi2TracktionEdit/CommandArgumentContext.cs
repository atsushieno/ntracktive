namespace Midi2TracktionEdit
{
	public class CommandArgumentContext
	{
		public string MidiFile { get; set; }
		public string TracktionEditTemplateFile { get; set; }

		public MidiImportContext CreateImportContext () => new MidiImportContext (this);
	}
}
