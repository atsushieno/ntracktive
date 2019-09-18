using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Commons.Music.Midi.Mml;
using Midi2TracktionEdit;
using NTracktive;

namespace Mugentrackted
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var serializer = new DataContractJsonSerializer(typeof(AugeneProject));
            var proj = new AugeneProject();
            if (args.Length > 0)
            {
                using (var fileStream = File.OpenRead(args[0]))
                    proj = (AugeneProject) serializer.ReadObject(JsonReaderWriterFactory.CreateJsonReader(
                        fileStream, new XmlDictionaryReaderQuotas()));
            }
            else
            {
                proj.MmlStrings.Add("1 @0 V110 v100 o5 l8 cegcegeg  >c1");
                proj.Tracks.Add(new AugeneTrack {AudioGraph = "Unnamed.filtergraph", Id = 1});
            }

            var memoryStream = new MemoryStream();
            serializer.WriteObject(memoryStream, proj);
            memoryStream.Position = 0;
            Console.WriteLine(new StreamReader(memoryStream).ReadToEnd());

            var compiler = new MmlCompiler();
            var mmls = proj.MmlFiles.Select(filename => 
                    new MmlInputSource(filename, new StringReader(File.ReadAllText(filename))))
                .Concat(proj.MmlStrings.Select (s => new MmlInputSource("(no file)", new StringReader(s))));
            var music = compiler.Compile(false,mmls.ToArray());
            var converter = new MidiToTracktionEditConverter();
            var edit = new EditElement();
            converter.ImportMusic(new MidiImportContext(music, edit));
            new EditModelWriter().Write(Console.Out, edit);
        }
    }

    public class AugeneProject
    {
        public IList<AugeneTrack> Tracks { get; set; } = new List<AugeneTrack>();
        public IList<string> MmlFiles { get; set; } = new List<string>();
        public IList<string> MmlStrings { get; set; } = new List<string>();
    }

    public class AugeneTrack
    {
        public double Id { get; set; }
        public string AudioGraph { get; set; }
    }
}
