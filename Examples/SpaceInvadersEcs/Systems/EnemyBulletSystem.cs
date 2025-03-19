using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using EcsSharp;
using SpaceInvadersEcs.Components;

namespace SpaceInvadersEcs.Systems;

public class EnemyBulletSystem
{
    private readonly int m_bulletTimerLimit;
    private readonly IEcsRepo m_ecsRepo;
    private Timer m_gameTimer;
    private int m_bulletTimer;
    private IEntity m_player;

    public EnemyBulletSystem(IEcsRepo ecsRepo, int bulletTimerLimit)
    {
        m_bulletTimerLimit = bulletTimerLimit;
        m_ecsRepo = ecsRepo;
    }

    public void Start()
    {
        m_player = m_ecsRepo.QuerySingle(EntityIds.Player);
        m_gameTimer = new Timer(Run, null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(100));
    }

    public void Stop()
    {
        m_gameTimer?.Dispose();
    }

    private void Run(object state)
    {
        IEntityCollection currentBullets = m_ecsRepo.QueryByTags(Tags.EnemyBullet);
        List<IEntity> toDelete = new List<IEntity>();
        Rect playerRect = m_player.RefreshComponent<Rect>();
        foreach (IEntity currentBullet in currentBullets)
        {
            Rect component = currentBullet.GetComponent<Rect>();
            if (component.Y + 50 > 480)
            {
                toDelete.Add(currentBullet);
                continue;
            }

            Rect loc = new Rect(component.X, component.Y + 45, component.Width, component.Height);
            currentBullet.SetComponent(loc);

            if (playerRect.IntersectsWith(loc))
            {
                m_player.SetDamage(100);
            }
        }

        if (toDelete.Count > 0)
        {
            foreach (IEntity entity in toDelete)
            {
                m_ecsRepo.Delete(entity);
            }
        }

        m_bulletTimer -= 13;
        if (m_bulletTimer < 0)
        {
            enemyBulletMaker(playerRect.X + 20, 10);
            m_bulletTimer = m_bulletTimerLimit;
        }
    }

    private void enemyBulletMaker(double x, double y)
    {
        Rect rect = new Rect
        {
            Height = 25,
            Width = 8,
            X = x,
            Y = y
        };
        m_ecsRepo.EntityBuilder().WithComponents(rect).WithTags(Tags.EnemyBullet).Build();
    }

    public void Reset()
    {
        m_ecsRepo.DeleteEntitiesWithTag(Tags.EnemyBullet);
    }
}