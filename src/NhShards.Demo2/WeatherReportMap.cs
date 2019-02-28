using FluentNHibernate.Mapping;

namespace NhShards.Demo2
{
    public class WeatherReportMap : ClassMap<WeatherReport>
    {
        public WeatherReportMap()
        {
            Id(x => x.ReportId).GeneratedBy.GuidNative();
            Map(x => x.ReportTime);
            Map(x => x.Temperature);
            Map(x => x.Longitude);
            Map(x => x.Latitude);
            Map(x => x.Continent);
        }
    }
}