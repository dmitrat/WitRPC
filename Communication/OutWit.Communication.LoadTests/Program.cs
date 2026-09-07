using OutWit.Communication.LoadTests;

var options = LoadOptions.Parse(args);
if (options.Help)
{
    Console.WriteLine(LoadOptions.Usage);
    return 0;
}

// One process hosts the server(s), the N clients and the measurement: give the pool the
// threads it will need at once instead of letting it inject them one or two per second.
ThreadPool.SetMinThreads(512, 512);

Console.WriteLine(options);

var report = await new LoadRunner(options).RunAsync(Console.Out);
report.Print(Console.Out);

if (options.JsonPath != null)
{
    report.WriteJson(options.JsonPath);
    Console.WriteLine($"report written to {options.JsonPath}");
}

return 0;
