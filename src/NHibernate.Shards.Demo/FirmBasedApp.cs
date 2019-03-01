using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Newtonsoft.Json;
using NHibernate.Cfg;
using NHibernate.Cfg.Loquacious;
using NHibernate.Criterion;
using NHibernate.Dialect;
using NHibernate.Shards.Cfg;
using NHibernate.Shards.LoadBalance;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Strategy.Access;
using NHibernate.Shards.Strategy.Resolution;
using NHibernate.Shards.Strategy.Selection;
using NHibernate.Shards.Tool;
using Configuration = System.Configuration.Configuration;

namespace NHibernate.Shards.Demo
{
    internal class FirmBasedApp
    {
        private ISessionFactory sessionFactory;

        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            var app = new FirmBasedApp();
            app.Run();
        }

        private void Run()
        {
            var shardedConfiguration = BuildShardedConfiguration();
            CreateSchema(shardedConfiguration);
            this.sessionFactory = shardedConfiguration.BuildShardedSessionFactory();

            AddData();

            using (var session = this.sessionFactory.OpenSession())
            {
                try
                {
                    var criteria = session.QueryOver<Employee>();
                    Console.WriteLine("QueryOver Total=" + criteria.RowCount());
                    Console.WriteLine("QueryOver Age>33 Total=" + criteria.Where(x => x.Age > 33).RowCount());

                    var totalCount = session.CreateSQLQuery(@"select count(*) from Employee").List<int>();
                    //var totalCount = session.CreateSQLQuery(@"select count(*) from Employee").UniqueResult<int>();

                    var criterCount = session.CreateSQLQuery(@"select count(*) from Employee where Age>33").List<int>();
                    Console.WriteLine("SQL Total=" + totalCount.Sum());
                    Console.WriteLine("SQL Age>33 Total=" + criterCount.Sum());
                }
                finally
                {
                    session.Close();
                }

                this.sessionFactory.Dispose();
                Console.WriteLine("Done.");
                Console.ReadKey(true);
            }
        }

        private static void CreateSchema(ShardedConfiguration shardedConfiguration)
        {
            var shardedSchemaExport = new ShardedSchemaExport(shardedConfiguration);
            shardedSchemaExport.Drop(false, true);
            shardedSchemaExport.Create(false, true);
        }
        private Project CreateProject(ISession session, Firm firm, string name, DateTime date)
        {
            var createProject = new Project
            {
                Firm = firm,
                Name = name,
                StartDate = date
            };
            createProject.Id = (Guid)session.Save(createProject);
            return createProject;
        }
        private Employee CreateEmployee(ISession session, Firm firm, string name, int age)
        {
            var createEmployee = new Employee
            {
                Firm = firm,
                Name = name,
                Age = age
            };
            createEmployee.Id = (Guid)session.Save(createEmployee);
            return createEmployee;
        }

        private Task CreateTask(ISession session, Project project, string name
            , object dateBase
            )
        {
            var createTask = new Task
            {
                Project = project,
                Name = name,
                //DateBase = dateBase
            };
            createTask.Id = (Guid)session.Save(createTask);
            return createTask;
        }

        private void AddData()
        {
            var session = this.sessionFactory.OpenSession();
            try
            {
                //var dateBases = new List<DateBase>();
                Firm firm1, firm2, firm3;
                using (var tr = session.BeginTransaction())
                {
                    firm1 = new Firm() { Name = "Firm1", ShardId = 1 };
                    firm2 = new Firm() { Name = "Firm2", ShardId = 2 };
                    firm3 = new Firm() { Name = "Firm3", ShardId = 3 };

                    session.Save(firm2);
                    session.Save(firm3);
                    session.Save(firm1);

                    //var start = new DateTime(2018, 1, 1);
                    //while (start < new DateTime(2019, 1, 1))
                    //{
                    //    var date = new DateBase() { Date = start, Firm = firm1 };
                    //    session.Save(date);
                    //    dateBases.Add(date);
                    //    start = start.AddDays(1);
                    //}

                    tr.Commit();
                }

                using (var tr = session.BeginTransaction())
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        CreateEmployee(session, firm2, "User" + i + " - Firm2", i * 10);
                        CreateEmployee(session, firm3, "User" + i + " - Firm3", i * 10);
                        CreateEmployee(session, firm1, "User" + i + " - Firm1", i * 10);
                    }

                    tr.Commit();
                }

                using (var tr = session.BeginTransaction())
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        CreateProject(session, firm2, "Project" + i + " - Firm2", new DateTime(2019, 1, i));
                        CreateProject(session, firm3, "Project" + i + " - Firm3", new DateTime(2019, 1, i));
                        CreateProject(session, firm1, "Project" + i + " - Firm1", new DateTime(2019, 1, i));
                    }

                    tr.Commit();
                }

                var projects = session.QueryOver<Project>().List();
                using (var tr = session.BeginTransaction())
                {
                    foreach (var project in projects)
                    {
                        for (int i = 1; i <= 5; i++)
                        {

                            CreateTask(session, project, "Task" + i + " - " + project.Name, null);
                        }
                    }

                    tr.Commit();
                }

            }
            finally
            {
                session.Close();
            }
        }

        public ShardedConfiguration BuildShardedConfiguration()
        {
            var prototypeConfig = Fluently.Configure()
                  .Mappings(m => m.FluentMappings.AddFromAssemblyOf<EmployeeMap>())
                  .Database(MsSqlConfiguration.MsSql2008.Dialect<MsSql2008Dialect>())
                  .BuildConfiguration();

            var shardConfigs = BuildShardConfigurations();
            var shardStrategyFactory = BuildShardStrategyFactory();
            var virtualShardMap = new Dictionary<short, short>
            {
                //{ 0, 0 },
                //{ 1, 0 },
                //{ 2, 0 },
                //{ 3, 0 }
            };
            return new ShardedConfiguration(prototypeConfig, shardConfigs, shardStrategyFactory, virtualShardMap);
        }

        private IEnumerable<IShardConfiguration> BuildShardConfigurations()
        {
            foreach (var firmDefinition in FirmDefinitionHelper.Firms)
            {
                yield return CreateShardConfig(firmDefinition.ShardId, firmDefinition.FirmKey);
            }

        }

        private IShardConfiguration CreateShardConfig(short shardId, string firmKey)
        {
            var connectionStringTemplate = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            return new ShardConfiguration
            {
                ShardSessionFactoryName = firmKey + "Shard",
                ShardId = shardId,
                ConnectionString = string.Format(connectionStringTemplate, firmKey)
            }; ;
        }

        private static IShardStrategyFactory BuildShardStrategyFactory()
        {
            return new MyStrategy();
        }
    }


    public class FirmShardSelectionStrategy : IShardSelectionStrategy
    {
        private readonly RoundRobinShardLoadBalancer loadBalancer;
        private readonly IList<FirmDefinition> firms;
        public FirmShardSelectionStrategy(RoundRobinShardLoadBalancer loadBalancer)
        {
            this.loadBalancer = loadBalancer;
            this.firms = FirmDefinitionHelper.Firms;
        }

        public ShardId SelectShardIdForNewObject(object obj)
        {
            if (obj is Firm firm)
            {
                var shard = this.loadBalancer.ShardIds.SingleOrDefault(x => x.Id == firm.ShardId);
                if (shard == null)
                    throw new NotSupportedException("Only BaseEntity inherits");
                return shard;
            }
            //if (obj is DateBase dayBase)
            //{
            //    var shard = this.loadBalancer.ShardIds[0];
            //    if (shard == null)
            //        throw new NotSupportedException("Only BaseEntity inherits");
            //    return shard;
            //}
            throw new NotSupportedException("Unsupported shard type:" + obj.GetType().Name);
        }
    }
    public class MyStrategy : IShardStrategyFactory
    {
        #region IShardStrategyFactory Members

        public IShardStrategy NewShardStrategy(IEnumerable<ShardId> shardIds)
        {
            var loadBalancer = new RoundRobinShardLoadBalancer(shardIds);
            var pss = new FirmShardSelectionStrategy(loadBalancer);
            IShardResolutionStrategy prs = new AllShardsShardResolutionStrategy(shardIds);
            IShardAccessStrategy pas = new SequentialShardAccessStrategy();
            return new ShardStrategyImpl(pss, prs, pas);
        }

        #endregion
    }


    public class FirmDefinition
    {
        public short ShardId { get; }
        public string FirmKey { get; }

        public FirmDefinition(short shardId, string firmKey)
        {
            ShardId = shardId;
            FirmKey = firmKey;
        }
    }
    public static class FirmDefinitionHelper
    {
        public static IList<FirmDefinition> Firms =>
            new List<FirmDefinition>
            {
                new FirmDefinition(0, "config"),
                new FirmDefinition(1, "firm1"),
                new FirmDefinition(2, "firm2"),
                new FirmDefinition(3, "firm3")
            };
    }

}