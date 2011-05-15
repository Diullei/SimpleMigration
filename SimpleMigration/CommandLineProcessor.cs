using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SimpleMigration
{
    public class CommandLineProcessor
    {
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
                        case "version":
                            try
                            {
                                MigrateTo(Convert.ToInt32(args[1]));
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

        private static void Migrate()
        {
            MigrateTo(Util.GetMaxVersionNumberInFolder(Environment.CurrentDirectory + @"\mig"));
        }

        private static void MigrateTo(long number)
        {
            if(number == -1)
            {
                Console.WriteLine("Database is up to date.");
                return;
            }

            var dbVersions = Util.GetVersionsInFolder(Environment.CurrentDirectory + @"\mig");

            if (!dbVersions.Contains(number))
            {
                Error("Invalid version number");
                return;
            }

            var dbVersion = Util.GetCurrentDataBaseVersion();
            if (dbVersion == number)
            {
                Console.WriteLine("Database is up to date.");
                return;
            }


            var versionSteps = new List<long>();

            var isUp = false;

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
                                                         connection.Execute(query, null, transaction);
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
            Console.WriteLine();
            Console.WriteLine("list of commands:");
            Console.WriteLine();
            Console.WriteLine("?                - help");
            Console.WriteLine("version 'number' - migrate database to 'number' version");
            Console.WriteLine();
            Console.WriteLine("NOTE: Use SimpleMigration without argument to migrate current database to last version");
            Console.WriteLine();
        }
    }
}