using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using EcsSharp;
using SpaceInvadersEcs.Components;

namespace SpaceInvadersEcs.Systems;

public class PlayerSystem
{
    private readonly IEntity m_playerEntity;
    private bool m_goLeft;
    private bool m_goRight;
    private Timer m_gameTimer;
    private Rect m_updated;
    private readonly Rect m_startingPosition;

    public PlayerSystem(IEcsRepo ecsRepo)
    {
        m_startingPosition = new Rect(381, 394, 55, 65);
        Rect rect = m_startingPosition;
        m_updated = rect;
        m_playerEntity = ecsRepo.EntityBuilder().WithComponents(rect).WithId(EntityIds.Player).Build();
    }

    public void Start()
    {
        m_gameTimer = new Timer(run, null, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(20));
    }

    public void Stop()
    {
        m_gameTimer?.Dispose();
    }

    public void Reset()
    {
        m_updated = m_startingPosition;
        m_playerEntity.SetComponent<DamageComponent>(0);
        m_playerEntity.SetComponent(m_updated);
    }

    private void run(object state)
    {
        updateMovement();
    }

    private void updateMovement()
    {
        if (m_goLeft || m_goRight)
        {
            double x = m_updated.X + 10 * (m_goLeft ? (double)-1 : 1);
            m_updated = new Rect(x, m_updated.Y, m_updated.Width, m_updated.Height);
            m_playerEntity.SetComponent(m_updated);
        }
    }

    public void KeyDown(Key eKey)
    {
        if (eKey == Key.Left)
        {
            m_goLeft = true;
        }

        if (eKey == Key.Right)
        {
            m_goRight = true;
        }
    }

    public void KeyUp(Key eKey)
    {
        if (eKey == Key.Left)
        {
            m_goLeft = false;
        }

        if (eKey == Key.Right)
        {
            m_goRight = false;
        }
    }


}