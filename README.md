# What is this?

NTractive is a .NET library to manipulate Tracktion's edit (`*.tracktionedit`)
XML files. You can load existing files, make changes, and write (back) to
files.

NTractive itself is just a simple strongly-typed XML elements (but we
avoided to depend on any existing serialization layers which won't work
well and possibly bring incompatibility issues between .NET profiles).
Namely, the entire XML processor must handle XML documents which is not
conformant to Namespaces in XML ([JUCE issue](https://github.com/WeAreROLI/JUCE/issues/463)).


# MidiToTracktionEdit

It comes with a sample called MidiToTracktionEdit. It loads a MIDI file,
optionally Tracktion edit file and generates an import of the MIDI file
into the edit file (to console).

Tracktion itself can import SMF too, but you'll find that you can modify
the samples and any arbitrary changes to the importer, to generate
whatever import results you would like to make.


# What it does not support

NTractive is only for XML manipulation. It cannot dynamically generate
`*.tracktion` project files. You can at best create project from Waveform
or any other means e.g. tools buiit with [tracktion_engine](https://github.com/Tracktion/tracktion_engine/)
and manipulate its edit files.

It only targets whatever elements and attributes we are aware of.
There have been various changes between Tracktion/Waveform versions in the
XML element names (possibly with some semantic changes) and we don't cover
all of them. Please file an issue if you found anything missing.

