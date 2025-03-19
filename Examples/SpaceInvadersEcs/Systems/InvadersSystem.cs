using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using EcsSharp;
using SpaceInvadersEcs.Components;

namespace SpaceInvadersEcs.Systems;

public class InvadersSystem
{
    private readonly IEcsRepo m_ecsRepo;
    private Timer m_gameTimer;
    private double m_enemySpeed = 7;
    private readonly IEntity m_player;

    public InvadersSystem(IEcsRepo ecsRepo)
    {
        m_ecsRepo = ecsRepo;
        m_player = m_ecsRepo.QuerySingle(EntityIds.Player);
    }

    public void Start()
    {
        makeEnemies(30);
        m_gameTimer = new Timer(run, null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(30));
    }

    public void Stop()
    {
        m_gameTimer?.Dispose();
    }

    private void run(object state)
    {
        updateMovement();
    }

    private void updateMovement()
    {

        m_ecsRepo.BatchUpdate(r =>
        {
            Rect playerRect = m_player.RefreshComponent<Rect>();
            List<IEntity> toDelete = new List<IEntity>();
            IEntityCollection queryByTags = r.QueryByTags(Tags.Invader);
            if (queryByTags.Count < 10)
            {
                m_enemySpeed = 10;
            }
            foreach (IEntity e in queryByTags)
            {
                
                if (e.GetDamage().Equals(100))
                {
                    toDelete.Add(e);
                    continue;
                }

                e.UpdateComponent<Rect>(c =>
                {
                    double x = c.X + m_enemySpeed;
                    double y = c.Y;
                    if (x > 820)
                    {
                        x = -80;
                        y = c.Y + c.Height + 10;
                    }

                    return new Rect(x, y, c.Width, c.Height);
                });
                checkContact(e.GetComponent<Rect>(), playerRect);
            }

            foreach (IEntity entity in toDelete)
            {
                m_ecsRepo.Delete(entity);
            }
        });
    }

    private void checkContact(Rect entity, Rect playerRect)
    {
        if (playerRect.IntersectsWith(entity))
        {
            
            m_player.SetDamage(100);
        }
    }

    private void makeEnemies(int limit)
    {
        int left = 0;

        for (int i = 0; i < limit; i++)
        {
            Rect rect = new Rect
            {
                Height = 45,
                Width = 45,
            };
            rect.Y = 30;
            rect.X = left;

            m_ecsRepo.EntityBuilder().WithComponents(rect).WithTags(Tags.Invader).Build();

            left -= 60;
        }
    }

    public void Reset()
    {
        m_enemySpeed = 7;
        m_ecsRepo.DeleteEntitiesWithTag(Tags.Invader);
    }
}