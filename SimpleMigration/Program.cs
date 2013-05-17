namespace SimpleMigration
{
    using System.Linq;
    using System.Text.RegularExpressions;

    public class Program
    {
        static void Main(string[] args)
        {
            new CommandLineProcessor(args);
        }
    }
}
