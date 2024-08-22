using Content.Scripts;

string program;
if (args.Length == 0)
{
    Console.WriteLine(@"Which program to run?
1: doorsplitter
2: metafixer
3: areaimporter");
    program = Console.ReadLine() ?? string.Empty;
}
else
{
    program = args[1];
}

program = program.ToLower().Trim();

if (program == "1" || "doorsplitter".Contains(program))
{
    DoorSplitter.Run();
}
else if (program == "2" || "metafixer".Contains(program))
{
    MetaFixer.Run();
}
else if (program == "3" || "areaimporter".Contains(program))
{
    new AreaImporter().Run();
}
else
{
    Console.WriteLine("No valid argument given, exiting. Valid arguments: doorsplitter, metafixer");
}
