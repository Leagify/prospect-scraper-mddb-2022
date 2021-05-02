using System;
using Spectre.Console;

namespace prospect_scraper_mddb_2022
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            AnsiConsole.Status()
            .Start("Thinking...", ctx => 
            {
                // Simulate some work
                AnsiConsole.MarkupLine("Doing some work...");
                System.Threading.Thread.Sleep(1000);
                
                // Update the status and spinner
                ctx.Status("Thinking some more");
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));

                // Simulate some work
                AnsiConsole.MarkupLine("Doing some more work...");
                System.Threading.Thread.Sleep(2000);
            });
            AnsiConsole.Render(new BarChart()
            .Width(60)
            .Label("[green bold underline]Number of fruits[/]")
            .CenterLabel()
            .AddItem("Sample Team 1", 12, Color.Yellow)
            .AddItem("Sample Team 2", 54, Color.Green)
            .AddItem("Sample Team 3", 33, Color.Red));

        }
    }
}
