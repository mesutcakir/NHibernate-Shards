using System;
using System.Collections.Generic;
using NHibernate.Shards;
using NHibernate.Shards.LoadBalance;
using NHibernate.Shards.Strategy;
using NHibernate.Shards.Strategy.Access;
using NHibernate.Shards.Strategy.Resolution;
using NHibernate.Shards.Strategy.Selection;

namespace NhShards.Demo2
{
    public class MyShardSelectionStrategy : IShardSelectionStrategy
    {
        private RoundRobinShardLoadBalancer loadBalancer;
        public MyShardSelectionStrategy(RoundRobinShardLoadBalancer loadBalancer)
        {
            this.loadBalancer = loadBalancer;
        }

        public ShardId SelectShardIdForNewObject(object obj)
        {
            return loadBalancer.ShardIds[0];
        }
    }
    public class MyStrategy : IShardStrategyFactory
    {
        #region IShardStrategyFactory Members

        public IShardStrategy NewShardStrategy(IEnumerable<ShardId> shardIds)
        {
            var loadBalancer = new RoundRobinShardLoadBalancer(shardIds);
            var pss = new MyShardSelectionStrategy(loadBalancer);
            IShardResolutionStrategy prs = new AllShardsShardResolutionStrategy(shardIds);
            IShardAccessStrategy pas = new SequentialShardAccessStrategy();
            return new ShardStrategyImpl(pss, prs, pas);
        }

        #endregion
    }
}