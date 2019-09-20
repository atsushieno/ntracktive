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
        public static void Main(string[] args)
        {
            var serializer = new DataContractSerializer(typeof(AugeneProject));
            var proj = new AugeneProject();
            if (args.Length > 0)
            {
                using (var fileStream = File.OpenRead(args[0]))
                    proj = (AugeneProject) serializer.ReadObject(XmlDictionaryReader.CreateTextReader(
                        fileStream, new XmlDictionaryReaderQuotas()));
            }
            else
            {
                proj.MmlStrings.Add("1 @0 V110 v100 o5 l8 cegcegeg  >c1");
                proj.Tracks.Add(new AugeneTrack {AudioGraph = "/home/atsushi/Desktop/Unnamed.filtergraph", Id = 1});
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
            var dstTracks = edit.Tracks.OfType<TrackElement>().ToArray(); 
            for (int n = 0; n < dstTracks.Length; n++)
                if (edit.Tracks[n].Id == null)
                    edit.Tracks[n].Id = (n + 1).ToString(CultureInfo.CurrentCulture);
            foreach (var track in proj.Tracks)
            {
                var dstTrack = dstTracks.FirstOrDefault(t => t.Id == track.Id.ToString(CultureInfo.CurrentCulture));
                if (dstTrack == null)
                    continue;
                var existingPlugins = dstTrack.Plugins.ToArray();
                dstTrack.Plugins.Clear();
                foreach (var p in ToTracktion(AugenePluginSpecifier.FromAudioGraph(
                    AudioGraph.Load(XmlReader.Create(track.AudioGraph)))))
                    dstTrack.Plugins.Add(p);
                // recover volume and level at the end.
                foreach (var p in existingPlugins)
                    dstTrack.Plugins.Add(p);
            }

            new EditModelWriter().Write(Console.Out, edit);
        }

        static IEnumerable<PluginElement> ToTracktion(IEnumerable<AugenePluginSpecifier> src)
        {
            return src.Select(a => new PluginElement
            {
                Filename = a.Filename,
                Enabled = true,
                Uid = a.Uid,
                Type = a.Type,
                Name = a.Name,
                Manufacturer = a.Manufacturer,
                State = a.State,
            });
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

    public class AudioGraph
    {
        public static IEnumerable<AudioGraph> Load(XmlReader reader)
        {
            var ret = new AudioGraph();
            var doc = XDocument.Load(reader);
            var input = doc.Root.Elements("FILTER").FirstOrDefault(e =>
                e.Elements("PLUGIN").Any(p => p.Attribute("name")?.Value == "Midi Input" &&
                                                p.Attribute("format")?.Value == "Internal"));
            var output = doc.Root.Elements("FILTER").FirstOrDefault(e =>
                e.Elements("PLUGIN").Any(p => p.Attribute("name")?.Value == "Audio Output" &&
                                                p.Attribute("format")?.Value == "Internal"));
            if (input == null || output == null)
                yield break;
            XElement conn;
            for (string uid = input.Attribute("uid")?.Value;
                (conn = doc.Root.Elements("CONNECTION").FirstOrDefault(e =>
                    e.Attribute("srcFilter")?.Value == uid)) != null && conn != output;
                uid = conn.Attribute("dstFilter")?.Value)
            {
                if (uid != input.Attribute("uid")?.Value)
                {
                    var filter = doc.Root.Elements("FILTER").FirstOrDefault(e => e.Attribute("uid")?.Value == uid);
                    if (filter == null)
                        yield break;
                    var plugin = filter.Element("PLUGIN"); 
                    if (plugin == null)
                        yield break;
                    var state = filter.Element("STATE");
                    var prog = plugin.Attribute("programNum");
                    yield return new AudioGraph
                    {
                        File = plugin.Attribute("file")?.Value,
                        Category = plugin.Attribute("category")?.Value,
                        Manufacturer = plugin.Attribute("manufacturer")?.Value,
                        Name = plugin.Attribute("name")?.Value,
                        Uid = plugin.Attribute("uid")?.Value,
                        ProgramNum = prog != null ? int.Parse(prog.Value) : 0,
                        State = state != null ? state.Value : null
                    };
                }
            }
        }

        //<PLUGIN name="Midi Input" descriptiveName="" format="Internal" category="I/O devices"
        //manufacturer="JUCE" version="1.0" file="" uid="cb5fde0b" isInstrument="0"
        //fileTime="0" infoUpdateTime="0" numInputs="0" numOutputs="0"
        //isShell="0"/>
        //<STATE>0.</STATE>
        public string Name { get; set; }
        public string DescriptiveName { get; set; }
        public string Format { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public string Version { get; set; }
        public string File { get; set; }
        public string Uid { get; set; }
        public int IsInstrument { get; set; }
        public long FileTime { get; set; }
        public long InfoUpdateTime { get; set; }
        public int NumInputs { get; set; }
        public int NumOutputs { get; set; }
        public int IsShell { get; set; }
        public string State { get; set; }
        public int ProgramNum { get; set; } // ?
    }

    public class AugenePluginSpecifier
    {
        public static IEnumerable<AugenePluginSpecifier> FromAudioGraph(IEnumerable<AudioGraph> audioGraph)
        {
            return audioGraph.Select(src => new AugenePluginSpecifier
            {
                Type = src.Format,
                Uid = src.Uid,
                Filename = src.File,
                Name = src.Name,
                Manufacturer = src.Manufacturer,
                ProgramNum = src.ProgramNum,
                State = src.State
            });
        }
        
        // They are required by tracktionedit.
        public string Type { get; set; }
        public string Uid { get; set; }
        public string Filename { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public int ProgramNum { get; set; }
        public string State { get; set; }
    }
}
