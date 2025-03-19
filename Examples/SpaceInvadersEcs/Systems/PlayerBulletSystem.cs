using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using EcsSharp;
using SpaceInvadersEcs.Components;

namespace SpaceInvadersEcs.Systems;

public class PlayerBulletSystem
{

    private readonly IEcsRepo m_ecsRepo;
    private readonly IEntity m_playerEntity;
    private Timer m_gameTimer;
    public int BulletDeleted;
    public int BulletCreated;

    public PlayerBulletSystem(IEcsRepo ecsRepo)
    {
        m_ecsRepo = ecsRepo;
        m_playerEntity = m_ecsRepo.QuerySingle(EntityIds.Player);
    }

    public void Start()
    {
        m_gameTimer = new Timer(Run, null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(50));
    }

    public void Stop()
    {
        m_gameTimer?.Dispose();
    }


    public void Reset()
    {
        BulletDeleted = 0;
        BulletCreated = 0;
        m_ecsRepo.DeleteEntitiesWithTag(Tags.PlayerBullet);
    }

    private void Run(object state)
    {

        IEntityCollection currentBullets = m_ecsRepo.Query<Rect>([Tags.PlayerBullet]);
        List<IEntity> toDelete = new List<IEntity>();
        foreach (IEntity currentBullet in currentBullets)
        {
            Rect currentLocation = currentBullet.GetComponent<Rect>();
            if (currentLocation.Y - 20 <= -5)
            {
                toDelete.Add(currentBullet);
                continue;
            }

            Rect newLocation = new Rect(currentLocation.X, currentLocation.Y - 20, currentLocation.Width, currentLocation.Height);
            currentBullet.SetComponent(newLocation);

            IEntityCollection invaders = m_ecsRepo.Query<Rect>([Tags.Invader]);
            foreach (IEntity invader in invaders)
            {
                Rect invaderRect = invader.GetComponent<Rect>();
                if (invaderRect.IntersectsWith(newLocation))
                {
                    DamageComponent damage = 100;
                    invader.SetComponent(damage);
                    toDelete.Add(currentBullet);
                }
            }
        }

        if (toDelete.Count > 0)
        {
            m_ecsRepo.BatchUpdate((r) =>
            {
                foreach (IEntity entity in toDelete)
                {
                    r.Delete(entity);
                    BulletDeleted++;
                }
            });
        }
    }

    public void Fire()
    {
        Rect rect = m_playerEntity.RefreshComponent<Rect>();
        bulletMaker(rect.X + rect.Width / 2, rect.Y - 20);
    }

    private void bulletMaker(double x, double y)
    {
        Rect bullet = new Rect
        {
            Height = 20,
            Width = 5,
            X = x,
            Y = y
        };
        m_ecsRepo.EntityBuilder().WithComponents(bullet).WithTags(Tags.PlayerBullet).Build();
        BulletCreated++;
    }

}