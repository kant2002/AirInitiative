using AirInitiative.Core;

var reports = await ReportLoader.Load(File.OpenRead(args[0]));