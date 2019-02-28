using System;
using System.Reflection;
using FluentNHibernate.Automapping;
using NHibernate.Cfg;
using NHibernate.Cfg.Loquacious;

namespace NhShards.Demo2
{
    public class WeatherReport
    {
        public virtual Guid ReportId { get; set; }

        public virtual string Continent { get; set; }

        public virtual long Latitude { get; set; }

        public virtual long Longitude { get; set; }

        public virtual int Temperature { get; set; }

        public virtual DateTime ReportTime { get; set; }
    }
}
