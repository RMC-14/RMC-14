using Content.Scripts;

string program;
if (args.Length == 0)
{
    Console.WriteLine("Which program to run? [doorsplitter, metafixer]");
    program = Console.ReadLine() ?? string.Empty;
}
else
{
    program = args[1];
}

program = program.ToLower().Trim();

if ("doorsplitter".Contains(program))
{
    DoorSplitter.Run();
}
else if ("metafixer".Contains(program))
{
    MetaFixer.Run();
}
else
{
    Console.WriteLine("No valid argument given, exiting. Valid arguments: doorsplitter, metafixer");
}
