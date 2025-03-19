using EcsSharp;

namespace SpaceInvadersEcs.Components
{
    internal static class EntityExtensions
    {
        public static IEntity SetDamage(this IEntity source, double pct) => source.SetComponent<DamageComponent>(100);
        public static double GetDamage(this IEntity source) => source.RefreshComponent<DamageComponent>();
    }
}