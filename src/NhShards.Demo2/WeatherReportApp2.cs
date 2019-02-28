using System;
using System.Collections.Generic;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Dialect;
using NHibernate.Shards;
using NHibernate.Shards.Cfg;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Tool;

namespace NhShards.Demo2
{
    internal class WeatherReportApp2
    {
        private ISessionFactory sessionFactory;

        public static void Main(string[] args)
        {
            var app = new WeatherReportApp2();
            app.Run();
        }

        private void Run()
        {
            var shardedConfiguration = BuildShardedConfiguration();
            CreateSchema(shardedConfiguration);
            this.sessionFactory = shardedConfiguration.BuildShardedSessionFactory();

            AddData();

            ISession session = this.sessionFactory.OpenSession();
            try
            {
                ICriteria crit = session.CreateCriteria(typeof(WeatherReport), "weather");
                var count = crit.List();
                if (count != null) Console.WriteLine(count.Count);
                crit.Add(Restrictions.Gt("Temperature", 33));
                var reports = crit.List();
                if (reports != null) Console.WriteLine(reports.Count);
            }
            finally
            {
                session.Close();
            }

            this.sessionFactory.Dispose();
            Console.WriteLine("Done.");
            Console.ReadKey(true);
        }

        private static void CreateSchema(ShardedConfiguration shardedConfiguration)
        {
            var shardedSchemaExport = new ShardedSchemaExport(shardedConfiguration);
            shardedSchemaExport.Drop(false, true);
            shardedSchemaExport.Create(false, true);
        }

        private void AddData()
        {
            ISession session = this.sessionFactory.OpenSession();
            try
            {
                session.BeginTransaction();
                var report = new WeatherReport
                {
                    Continent = "North America",
                    Latitude = 25,
                    Longitude = 30,
                    ReportTime = DateTime.Now,
                    Temperature = 44
                };
                session.Save(report);

                report = new WeatherReport
                {
                    Continent = "Africa",
                    Latitude = 44,
                    Longitude = 99,
                    ReportTime = DateTime.Now,
                    Temperature = 31
                };
                session.Save(report);

                report = new WeatherReport
                {
                    Continent = "Asia",
                    Latitude = 13,
                    Longitude = 12,
                    ReportTime = DateTime.Now,
                    Temperature = 104
                };
                session.Save(report);
                session.Transaction.Commit();
            }
            finally
            {
                session.Close();
            }
        }

        public ShardedConfiguration BuildShardedConfiguration()
        {
            var prototypeConfig = Fluently.Configure()
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<WeatherReportMap>())
                .Database(MsSqlConfiguration.MsSql2008.Dialect<MsSql2008Dialect>())
                .BuildConfiguration();

            //prototypeConfig.Proxy(p =>
            //     {
            //         p.Validation = false;
            //     })
            //    .DataBaseIntegration(db =>
            //    {
            //        db.Dialect<MsSql2008Dialect>();
            //    });
            //.AddResource("NHibernate.Shards.Demo.Mappings.hbm.xml", Assembly.GetExecutingAssembly());            

            var shardConfigs = BuildShardConfigurations();
            var shardStrategyFactory = BuildShardStrategyFactory();
            return new ShardedConfiguration(prototypeConfig, shardConfigs, shardStrategyFactory);
        }

        private IEnumerable<IShardConfiguration> BuildShardConfigurations()
        {
            yield return new ShardConfiguration
            {
                ShardSessionFactoryName = "Firm0",
                ShardId = 0,
                ConnectionStringName = "shard0"
            };
            yield return new ShardConfiguration
            {
                ShardSessionFactoryName = "Firm1",
                ShardId =1,
                ConnectionStringName = "shard1"
            };
            yield return new ShardConfiguration
            {
                ShardSessionFactoryName = "Firm2",
                ShardId = 2,
                ConnectionStringName = "shard2"
            };
        }

        private static IShardStrategyFactory BuildShardStrategyFactory()
        {
            return new MyStrategy();
        }
    }
}