using System;
using System.Threading;
using EcsSharp;
using SpaceInvadersEcs.Components;

namespace SpaceInvadersEcs.Systems
{
    public enum GameStates
    {
        Running,
        Won,
        Lost,
    }
    public class GameStateSystem
    {
        private readonly IEcsRepo m_ecsRepo;
        private          Timer    m_gameTimer;
        public IEntity GameStateEntity { get; }
        private readonly IEntity  m_playerEntity;

        public GameStateSystem(IEcsRepo ecsRepo)
        {
            m_ecsRepo = ecsRepo;
            m_playerEntity = m_ecsRepo.QuerySingle(EntityIds.Player);
            InvaderCount count = 0;
            GameStateEntity = m_ecsRepo.EntityBuilder().WithId(EntityIds.GameState)
                                         .WithComponents(GameStates.Running, count)
                                         .Build();
        }

        public void Start()
        {
            m_gameTimer = new Timer(run, null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(30));
        }

        public void Stop()
        {
            m_gameTimer?.Dispose();
        }

        public void Reset()
        {
            InvaderCount count = 0;
            GameStateEntity.SetComponents(GameStates.Running,count);
        }

        private void run(object state)
        {
            m_ecsRepo.BatchUpdate(r =>
            {
                int count = m_ecsRepo.QueryByTags(Tags.Invader).Count;
                

                if (count == 0)
                {
                    GameStateEntity.SetComponent(GameStates.Won);
                }

                if (m_playerEntity.GetDamage().Equals(100))
                {
                    GameStateEntity.SetComponent(GameStates.Lost);
                }

                GameStateEntity.SetComponent<InvaderCount>(count);
            });
        }
    }
}