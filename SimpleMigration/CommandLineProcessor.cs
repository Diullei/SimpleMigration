using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SimpleMigration
{
    // verificar se a versão do banco é maior do que a versão do folder

    public class CommandLineProcessor
    {
        private const int RESET = -2;

        private static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public CommandLineProcessor(string[] args)
        {
            try
            {
                VerifyTag();

                if (args.Length > 0)
                {
                    switch (args[0])
                    {
                        case "?":
                            Help();
                            break;
                        case "reset":
                            MigrateTo(RESET);
                            break;
                        case "new":
                            NewTemplate();
                            break;
                        case "current":
                            Current();
                            break;
                        case "version":
                            try
                            {
                                MigrateTo(Convert.ToInt64(args[1]));
                            }
                            catch (Exception ex)
                            {
                                Error("Version number expected", ex);
                            }

                            break;
                        default:
                            Error("Unknown command. Use '?' arg to view all commands", null);
                            break;
                    }
                }
                else
                {
                    Migrate();
                }
            }
            catch (Exception ex)
            {
                Error("Unespected error --> " + ex.Message, ex);
            }
        }

        private static void VerifyTag()
        {
            var folderTag = ConfigurationManager.AppSettings["tag"];
            var databaseTag = Util.GetCurrentDataBaseTag();

            if (folderTag != databaseTag)
            {
                var cacheForegroundColor = Console.ForegroundColor;
                var cacheBackgroundColor = Console.BackgroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.Yellow;

                Console.WriteLine("=== Current database tag ({0}) is different from folder tag ({1}) ===", databaseTag, folderTag);

                Console.ForegroundColor = cacheForegroundColor;
                Console.BackgroundColor = cacheBackgroundColor;
            }
        }

        private static void Current()
        {
            var cacheForegroundColor = Console.ForegroundColor;

            var folderVersion = Util.GetMaxVersionNumberInFolder(Environment.CurrentDirectory + @"\mig");
            var databaseVersion = Util.GetCurrentDataBaseVersion();

            var folderTag = ConfigurationManager.AppSettings["tag"];
            var databaseTag = Util.GetCurrentDataBaseTag();

            Console.WriteLine("  Migration version ");

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine(" >           folder ({1}): {0}", folderVersion, folderTag);

            if (folderTag != databaseTag)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            Console.WriteLine(" >         database ({1}): {0}", databaseVersion, databaseTag);

            Console.ForegroundColor = cacheForegroundColor;

            Console.WriteLine();
        }

        private static void NewTemplate()
        {
            var nextNumber = Util.GetMaxVersionNumberInFolder(Environment.CurrentDirectory + @"\mig") + 1;

            using (var sw = new StreamWriter(string.Format("{0}\\mig\\{1}-UP.sql", Environment.CurrentDirectory, nextNumber)))
            {
                sw.Write("-- create here your up script.");
            }
            
            Console.WriteLine("file {0}\\mig\\{1}-UP.sql created.", Environment.CurrentDirectory, nextNumber);

            using (var sw = new StreamWriter(string.Format("{0}\\mig\\{1}-DOWN.sql", Environment.CurrentDirectory, nextNumber)))
            {
                sw.Write("-- create here your roolback script to " + nextNumber + ".UP.sql script.");
            }

            Console.WriteLine("file {0}\\mig\\{1}-DOWN.sql created.", Environment.CurrentDirectory, nextNumber);
        }

        private static void Migrate()
        {
            MigrateTo(Util.GetMaxVersionNumberInFolder(Environment.CurrentDirectory + @"\mig"));
        }

        private static List<string> SplitScriptByGo(string script)
        {
            return Regex.Split(script, ConfigurationManager.AppSettings["block_split_pattern"], RegexOptions.Multiline).Select(x => x.Trim()).Where(x => x.ToUpper() != ConfigurationManager.AppSettings["uppercase_block_split_token"] && !string.IsNullOrWhiteSpace(x)).ToList();
        }

        private static void MigrateTo(long number)
        {
            if(number == -1)
            {
                Console.WriteLine("Database is up to date.");
                return;
            }

            var databaseVersions = Util.GetVersionsInFolder(Environment.CurrentDirectory + @"\mig");

            if (number != RESET)
            {
                if (!databaseVersions.Contains(number))
                {
                    Error("Invalid version number", null);
                    return;
                }
            }

            var databaseVersion = Util.GetCurrentDataBaseVersion();

            if (databaseVersion == 0)
            {
                Console.WriteLine("there is no version to reset.");
            }

            if (databaseVersion == number)
            {
                Console.WriteLine("Database is up to date.");
                return;
            }

            var versionSteps = new List<long>();

            var isUp = false;

            databaseVersions.Sort();

            if(number > databaseVersion)
            {
                isUp = true;
                versionSteps = databaseVersions.Where(v => v > databaseVersion && v <= number).Select(v => v).ToList();
            }
            else
            {
                versionSteps = databaseVersions.Where(v => v > number && v <= databaseVersion).Select(v => v).Reverse().ToList();
            }

            versionSteps.ForEach(version =>
                                     {
                                         try
                                         {
                                             if (!isUp)
                                             {
                                                 Util.VerifyVersionNumber(version);
                                             }

                                             using (var connection = Util.CreateConnection())
                                             {
                                                 using (var transaction = connection.BeginTransaction())
                                                 {
                                                     try
                                                     {
                                                         var query = File.ReadAllText(string.Format("mig\\{0}-{1}.sql", version, isUp ? "up" : "down"));

                                                         var scripts = SplitScriptByGo(query);
                                                         scripts.ForEach(s => connection.Execute(s, null, transaction));

                                                         transaction.Commit();
                                                     }
                                                     catch (Exception ex) 
                                                     {
                                                         transaction.Rollback();
                                                         throw ex;
                                                     }
                                                 }
                                             }

                                             if (isUp)
                                             {
                                                 Util.InsertDataBaseVersionNumber(version);
                                             }
                                             else
                                             {
                                                 Util.DeleteDataBaseVersionNumberTo(version);
                                             }
                                         }
                                         catch (Exception ex)
                                         {
                                             Error("Migrate database to " + version + " --> " + ex.Message, ex);
                                             throw;
                                         }

                                         Console.WriteLine("Database migrated to: " + Util.GetCurrentDataBaseVersion() + " version.");
                                     });
        }

        private static void Error(string message, Exception ex)
        {
            Console.WriteLine("Simple Migration Error: " + message + ".");

            if (ex != null)
                Console.WriteLine(ex);
        }

        private static void Help()
        {
            Console.WriteLine("SimpleMigration " + Version);
            Console.WriteLine("list of commands:");
            Console.WriteLine("?                - help");
            Console.WriteLine("version 'number' - migrate database to 'number' version");
            Console.WriteLine("version 'new'    - create an up/down template file to next migration");
            Console.WriteLine("current          - show current version");
            Console.WriteLine();
            Console.WriteLine("NOTE: Use SimpleMigration without argument to migrate current database to last version");
            Console.WriteLine();
        }
    }
}