using FluentNHibernate.Mapping;
using System;

namespace NHibernate.Shards.Demo
{
    public class WeatherReport
    {
        public virtual string ReportId { get; set; }

        public virtual string Continent { get; set; }

        public virtual long Latitude { get; set; }

        public virtual long Longitude { get; set; }

        public virtual int Temperature { get; set; }

        public virtual DateTime ReportTime { get; set; }
    }
    public class WeatherReportMap : ClassMap<WeatherReport>
    {
        public WeatherReportMap()
        {
            Id(x => x.ReportId).GeneratedBy.Custom<NHibernate.Shards.Id.ShardedUUIDGenerator>();
            Map(x => x.ReportTime);
            Map(x => x.Temperature);
            Map(x => x.Longitude);
            Map(x => x.Latitude);
            Map(x => x.Continent);
        }
    }
}