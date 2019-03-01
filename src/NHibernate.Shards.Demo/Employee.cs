using FluentNHibernate.Mapping;
using System;

namespace NHibernate.Shards.Demo
{
    #region General


    public abstract class BaseEntity
    {
        public virtual Guid Id { get; set; }
    }
    public class BaseEntityMap<T> : ClassMap<T> where T : BaseEntity
    {
        public BaseEntityMap()
        {
            Id(x => x.Id).GeneratedBy.GuidNative();
        }
    }
    public class Firm : BaseEntity
    {
        public virtual short ShardId { get; set; }
        public virtual string Name { get; set; }
    }
    public class FirmMap : BaseEntityMap<Firm>
    {
        public FirmMap()
        {
            Map(x => x.Name);
            Map(x => x.ShardId);
        }
    }
    public class DayBase : BaseEntity
    {
        public virtual DateTime Date { get; set; }
    }
    public class DayBaseMap : BaseEntityMap<DayBase>
    {
        public DayBaseMap()
        {
            Map(x => x.Date);
        }
    }
    #endregion




    #region FirmBase

    public abstract class FirmBaseEntity : BaseEntity
    {
        public virtual Firm Firm { get; set; }
    }
    public class FirmBaseEntityMap<T> : BaseEntityMap<T> where T : FirmBaseEntity
    {
        public FirmBaseEntityMap()
        {
            var typeName = typeof(T).Name;
            References(x => x.Firm, "FirmId").ForeignKey("FK_BaseEntity_" + typeName + "Id");
        }
    }
    public class Employee : FirmBaseEntity
    {
        public virtual string Name { get; set; }

        public virtual int Age { get; set; }
    }
    public class EmployeeMap : FirmBaseEntityMap<Employee>
    {
        public EmployeeMap()
        {
            Map(x => x.Name);
            Map(x => x.Age);
        }
    }
    public class Project : FirmBaseEntity
    {
        public virtual string Name { get; set; }
        public virtual DateTime StartDate { get; set; }
    }
    public class ProjectMap : FirmBaseEntityMap<Project>
    {
        public ProjectMap()
        {
            Map(x => x.Name);
            Map(x => x.StartDate);
        }
    }

    public class Task : FirmBaseEntity
    {
        public virtual string Name { get; set; }
        public virtual Project Project { get; set; }
        public virtual DayBase DayBase { get; set; }
    }
    public class TaskMap : FirmBaseEntityMap<Task>
    {
        public TaskMap()
        {
            Map(x => x.Name);
            References(x => x.DayBase, "DayBaseId").ForeignKey("FK_Task_DayBaseId");
            References(x => x.Project, "ProjectId").ForeignKey("FK_Task_ProjectId");
        }
    }


    #endregion
}