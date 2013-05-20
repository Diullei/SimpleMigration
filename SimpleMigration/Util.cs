using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleMigration
{
    public static class Util
    {
        public static void ValidateVersionFiles(string versionFolder)
        {
            if(Directory.Exists(versionFolder))
            {
                var files = Directory.GetFiles(versionFolder).Where(f => f.EndsWith(".sql")).Select(f => Path.GetFileName(f)).ToList();
                var versions = new List<string>();
                files.ForEach(f =>
                                  {
                                      if(!Regex.IsMatch(f, @"[0-9]+\-([Uu][Pp]|[Dd][Oo][Ww][Nn])\.sql"))
                                      {
                                          throw new Exception(string.Format("Invalid file name '{0}'", f));
                                      }

                                      var version = f.Substring(0, f.LastIndexOf('-'));

                                      if(!versions.Contains(version))
                                          versions.Add(version);
                                  });

                versions.ForEach(v =>
                                     {
                                         if(!File.Exists(string.Format("mig\\{0}-up.sql", v)))
                                         {
                                             throw new Exception(string.Format("UP version file: '{0}-up.sql' not found.", v));
                                         }

                                         if (!File.Exists(string.Format("mig\\{0}-down.sql", v)))
                                         {
                                             throw new Exception(string.Format("DOWN version file: '{0}-down.sql' not found.", v));
                                         }
                                     });
            }
            else
                throw new Exception("Invalid version folder.");
        }

        public static long GetMaxVersionNumberInFolder(string versionFolder)
        {
            var list = GetVersionsInFolder(versionFolder);

            if (list.Count == 0)
                return -1;

            return list.Max(l => l);
        }

        public static long GetMinVersionNumberInFolder(string versionFolder)
        {
            var list = GetVersionsInFolder(versionFolder);

            if (list.Count == 0)
                return -1;

            return list.Min(l => l);
        }

        public static List<long> GetVersionsInFolder(string versionFolder)
        {
            ValidateVersionFiles(versionFolder);

            if (Directory.Exists(versionFolder))
            {
                var files = Directory.GetFiles(versionFolder).Where(f => f.EndsWith(".sql")).Select(f => Path.GetFileName(f)).ToList();
                var versions = new List<long>();
                files.ForEach(f =>
                {
                    if (!Regex.IsMatch(f, @"[0-9]+\-([Uu][Pp]|[Dd][Oo][Ww][Nn])\.sql"))
                    {
                        throw new Exception(string.Format("Invalid file name '{0}'", f));
                    }

                    var version = f.Substring(0, f.LastIndexOf('-'));

                    if (!versions.Contains(Convert.ToInt64(version)))
                        versions.Add(Convert.ToInt64(version));
                });

                return versions;
            }
            throw new Exception("Invalid version folder.");
        }

        public static IDbConnection CreateConnection()
        {
            var connectionCfg = ConfigurationManager.ConnectionStrings["SimpleMigration"];
            var providerName = connectionCfg.ProviderName;
            var factory = DbProviderFactories.GetFactory(providerName);
            var dbConnection = factory.CreateConnection();
            dbConnection.ConnectionString = connectionCfg.ConnectionString;
            dbConnection.Open();
            return dbConnection;
        }

        public static void VerifyDataBaseConnection()
        {
            using(var connection = CreateConnection())
            {
                //...
            }
        }

        public static long GetCurrentDataBaseVersion()
        {
            DbVersion dbVersion = null;

            using (var connection = CreateConnection())
            {
                var dbVersions = connection.Query<DbVersion>("select max(version) Version from SimpleMigration_VersionInfo");
                dbVersion = dbVersions.ToList().Count > 0 ? dbVersions.First() : new DbVersion() { Version = -1 };
            }

            return dbVersion.Version;
        }

        public static string GetCurrentDataBaseTag()
        {
            DbTagVersion dbVersion = null;

            using (var connection = CreateConnection())
            {
                var dbVersions = connection.Query<DbTagVersion>("select tag Tag from SimpleMigration_VersionInfo where version = (select max(version) Version from SimpleMigration_VersionInfo)");
                dbVersion = dbVersions.ToList().Count > 0 ? dbVersions.First() : new DbTagVersion() { Tag = "" };
            }

            return dbVersion.Tag;
        }

        public static void VerifyVersionNumber(long number)
        {
            using (var connection = CreateConnection())
            {
                var dbVersions = connection.Query<dynamic>("select count(*) Count from SimpleMigration_VersionInfo where version = @VERSION", new { VERSION = number });

                if (dbVersions.ToList()[0].Count == 0)
                    throw new Exception("Invalid version number");
            }
        }

        public static void InsertDataBaseVersionNumber(long version)
        {
            var tag = ConfigurationManager.AppSettings["tag"];
            using (var connection = CreateConnection())
            {
                connection.Execute("insert into SimpleMigration_VersionInfo values (@VERSION, @TAG)", new { VERSION = version, TAG = tag });
            }
        }

        public static void DeleteDataBaseVersionNumberTo(long version)
        {
            using (var connection = CreateConnection())
            {
                connection.Execute("delete from SimpleMigration_VersionInfo where version >= @VERSION", new { VERSION = version });
            }
        }
    }
}