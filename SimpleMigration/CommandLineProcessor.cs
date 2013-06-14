using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SimpleMigration
{
    using System.Threading;

    public class CommandLineProcessor
    {
        private const int RESET = -2;

        private static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private static int _errorCode = 0;

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
                        case "n":
                            NewTemplate();
                            break;
                        case "current":
                            Current();
                            break;
                        case "c":
                            Current();
                            break;
                        case "join":
                            try
                            {
                                Join(Convert.ToInt64(args[1]));
                            }
                            catch (Exception ex)
                            {
                                Join(1);
                            }
                            break;
                        case "j":
                            try
                            {
                                Join(Convert.ToInt64(args[1]));
                            }
                            catch (Exception ex)
                            {
                                Join(1);
                            }
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
                        case "v":
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
                Error("Unespected error", ex);
            }

            if (_errorCode != 0)
            {
                Current();
            }
            Environment.Exit(_errorCode);
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

        private static void Join(long start)
        {
            var databaseVersions = Util.GetVersionsInFolder(Environment.CurrentDirectory + @"\mig");

            databaseVersions = databaseVersions.Where(n => n >= start).ToList();

            databaseVersions.Sort();

            Console.WriteLine("Joining UP scripts");

            using (var sw = new StreamWriter(Environment.CurrentDirectory + @"\mig\UP-JOIN.sql"))
            {
                databaseVersions.ForEach(version =>
                {
                    var file = string.Format("mig\\{0}-{1}.sql", version, "up");

                    Console.WriteLine(">> Joining " + file);

                    var content = File.ReadAllText(file);

                    sw.WriteLine("-- " + file);
                    sw.WriteLine(content);
                });
            }

            databaseVersions.Reverse();

            Console.WriteLine("Joining DOWN scripts");

            using (var sw = new StreamWriter(Environment.CurrentDirectory + @"\mig\DOWN-JOIN.sql"))
            {
                databaseVersions.ForEach(version =>
                {
                    var file = string.Format("mig\\{0}-{1}.sql", version, "down");

                    Console.WriteLine(">> Joining " + file);

                    var content = File.ReadAllText(file);

                    sw.WriteLine("-- " + file);
                    sw.WriteLine(content);
                });
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

            Thread.Sleep(100);

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
                                         string currentQuery = null;
                                         string currentFile = null;
                                         int currentFileIndex = 0;

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
                                                         currentFile = string.Format("mig\\{0}-{1}.sql", version, isUp ? "up" : "down");
                                                         var query = File.ReadAllText(currentFile);

                                                         var scripts = SplitScriptByGo(query);
                                                         scripts.ForEach(s =>
                                                             {
                                                                 currentQuery = s;
                                                                 currentFileIndex = query.Substring(currentFileIndex).IndexOf(s);
                                                                 connection.Execute(s, null, transaction);
                                                             });

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
                                             Error("Migrate database to " + version, ex, new QueryError
                                                 {
                                                     File = currentFile, 
                                                     Query = currentQuery, 
                                                     StartLine = currentFileIndex
                                                 });
                                             throw;
                                         }

                                         Console.WriteLine("Database migrated to: " + Util.GetCurrentDataBaseVersion() + " version.");
                                     });
        }

        private static void Error(string message, Exception ex, QueryError query = null)
        {
            Console.WriteLine("---------------------------------------------------------------------");
            Console.WriteLine(" Migration error: " + message);
            Console.WriteLine("---------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(" Error message: ");
            Console.WriteLine();
            if (ex != null)
            {
                var stk = ex.ToString();
                var lines = Regex.Split(stk, "\r\n|\r|\n");
                for (var i = 0; i < lines.Length; i++)
                {
                    var foreColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(">>    ");
                    Console.ForegroundColor = foreColor;
                    Console.WriteLine(lines[i]);
                    Thread.Sleep(100);
                }
            }
            else
            {
                Console.WriteLine();
            }

            if (query != null)
            {
                Console.WriteLine();
                Console.WriteLine(" File      : " + query.File);
                Console.WriteLine(" Start line: " + query.StartLine);
                Console.WriteLine(" Query     : ");

                Console.WriteLine();
                var lines = Regex.Split(query.Query, "\r\n|\r|\n");
                var max = lines.Length < 15 ? lines.Length : 15;
                var bkColor = Console.BackgroundColor;
                var foreColor = Console.ForegroundColor;
                Console.BackgroundColor = ConsoleColor.Blue;
                for (var i = 0; i < max; i++)
                {
                    Console.WriteLine("    " + lines[i]);
                }

                if (lines.Length > 15)
                {
                    Console.Write("    ... ");
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("<more>");
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = foreColor;
                    Console.WriteLine(" ...");
                }

                Console.BackgroundColor = bkColor;
                Thread.Sleep(100);

                Console.WriteLine();
            }
            Console.WriteLine("---------------------------------------------------------------------");

            _errorCode = 1;
        }

        private static void Help()
        {
            Console.WriteLine("SimpleMigration " + Version);
            Console.WriteLine("list of commands:");
            Console.WriteLine("?                   - help");
            Console.WriteLine("version [v] 'number' - migrate database to 'number' version");
            Console.WriteLine("version [n] 'new'    - create an up/down template file to next migration");
            Console.WriteLine("current [c]          - show current version");
            Console.WriteLine();
            Console.WriteLine("NOTE: Use SimpleMigration without argument to migrate current database to last version");
            Console.WriteLine();
        }
    }
}