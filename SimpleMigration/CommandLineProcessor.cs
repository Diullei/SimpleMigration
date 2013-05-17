using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SimpleMigration
{
    using System.Text.RegularExpressions;

    public class CommandLineProcessor
    {
        private const int RESET = -2;

        private static string _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public CommandLineProcessor(string[] args)
        {
            try
            {
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
                        case "version":
                            try
                            {
                                MigrateTo(Convert.ToInt64(args[1]));
                            }
                            catch (Exception)
                            {
                                Error("Version number expected");
                            }
                            break;
                        default:
                            Error("Unknown command. Use '?' arg to view all commands");
                            break;
                    }
                }
                else
                    Migrate();
            }
            catch (Exception ex)
            {
                Error("Unespected error --> " + ex.Message);
            }
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
            return Regex.Split(script, "^([Gg][Oo]\r\n|[Gg][Oo]\r|[Gg][Oo]\n|[Gg][Oo])$", RegexOptions.Multiline).Select(x => x.Trim()).Where(x => x.ToUpper() != "GO" && !string.IsNullOrWhiteSpace(x)).ToList();
        }

        private static void MigrateTo(long number)
        {
            if(number == -1)
            {
                Console.WriteLine("Database is up to date.");
                return;
            }

            var dbVersions = Util.GetVersionsInFolder(Environment.CurrentDirectory + @"\mig");

            if(number != RESET)
                if (!dbVersions.Contains(number))
                {
                    Error("Invalid version number");
                    return;
                }

            var dbVersion = Util.GetCurrentDataBaseVersion();

            if(dbVersion == 0)
                Console.WriteLine("there is no version to reset.");

            if (dbVersion == number)
            {
                Console.WriteLine("Database is up to date.");
                return;
            }

            var versionSteps = new List<long>();

            var isUp = false;

            dbVersions.Sort();

            if(number > dbVersion)
            {
                isUp = true;
                versionSteps = dbVersions.Where(v => v > dbVersion && v <= number).Select(v => v).ToList();
            }
            else
            {
                versionSteps = dbVersions.Where(v => v > number && v <= dbVersion).Select(v => v).Reverse().ToList();
            }

            versionSteps.ForEach(version =>
                                     {
                                         try
                                         {
                                             if(!isUp)
                                                Util.VerifyVersionNumber(version);

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
                                                     catch(Exception ex) 
                                                     {
                                                         transaction.Rollback();
                                                         throw ex;
                                                     }
                                                 }
                                             }

                                             if (isUp)
                                                Util.InsertDataBaseVersionNumber(version);
                                             else
                                                 Util.DeleteDataBaseVersionNumberTo(version);
                                         }
                                         catch (Exception ex)
                                         {
                                             Error("Migrate database to " + version + " --> " + ex.Message);
                                             throw ex;
                                         }

                                         Console.WriteLine("Database migrated to: " + Util.GetCurrentDataBaseVersion() + " version.");
                                     });
        }

        private static void Error(string message)
        {
            Console.WriteLine("Simple Migration Error: " + message + ".");
        }

        private static void Help()
        {
            Console.WriteLine("SimpleMigration " + _version);
            Console.WriteLine("list of commands:");
            Console.WriteLine("?                - help");
            Console.WriteLine("version 'number' - migrate database to 'number' version");
            Console.WriteLine("version 'new'    - create an up/down template file to next migration");
            Console.WriteLine();
            Console.WriteLine("NOTE: Use SimpleMigration without argument to migrate current database to last version");
            Console.WriteLine();
        }
    }
}