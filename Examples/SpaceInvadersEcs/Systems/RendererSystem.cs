using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using EcsSharp;
using EcsSharp.Events.EventArgs;
using SpaceInvadersEcs.Components;

namespace SpaceInvadersEcs.Systems;

public class RendererSystem
{
    private readonly IEcsRepo m_ecsRepo;
    private readonly Canvas m_canvas;
    private readonly Label m_msgCtrl;
    private readonly DispatcherTimer m_gameTimer;
    private readonly Random m_rand = new Random();

    public RendererSystem(IEcsRepo ecsRepo, Canvas canvas, Label msgCtrl)
    {
        m_ecsRepo = ecsRepo;
        m_canvas = canvas;
        m_msgCtrl = msgCtrl;
        m_ecsRepo = ecsRepo;
        m_gameTimer = new DispatcherTimer();
        m_gameTimer.Tick += render;
        m_gameTimer.Interval = TimeSpan.FromMilliseconds(20);
        m_gameTimer.Start();
        m_ecsRepo.Events.ComponentDeleted[typeof(Rectangle)] += onDeleted;
    }

    private void onDeleted(ComponentDeletedEventArgs obj)
    {

        if (obj.Component.Data is Rectangle r)
        {
            m_canvas.Dispatcher.BeginInvoke(() =>
            {
                m_canvas.Children.Remove(r);
            });
        }
    }

    private void render(object sender, EventArgs e)
    {
        updateLocations();
    }

    private void updateLocations()
    {
        IEntityCollection entityCollection = m_ecsRepo.Query([typeof(Rect), typeof(Rectangle)]);
        foreach (IEntity entity in entityCollection)
        {
            Rect rect = entity.GetComponent<Rect>();
            Rectangle canvasRect = entity.GetComponent<Rectangle>();
            if (canvasRect == null)
            {
                canvasRect = createCanvasObject(entity, rect);
                if (canvasRect != null)
                {
                    entity.SetComponent(canvasRect);
                }
            }
            else
            {
                Canvas.SetTop(canvasRect, rect.Y);
                Canvas.SetLeft(canvasRect, rect.X);
            }
        }
    }

    private Rectangle createCanvasObject(IEntity entity, Rect rect)
    {
        if (entity.HasTag(Tags.EnemyBullet))
        {
            return createBullet(rect, Brushes.Yellow, Brushes.Black, Tags.EnemyBullet);
        }

        if (entity.HasTag(Tags.PlayerBullet))
        {
            return createBullet(rect, Brushes.White, Brushes.Red, Tags.PlayerBullet);
        }

        if (entity.Id == EntityIds.Player)
        {
            return createPlayer(rect);
        }

        if (entity.HasTag(Tags.Invader))
        {
            return createInvader(rect);
        }

        return null;
    }

    private Rectangle createInvader(Rect rect)
    {
        ImageBrush enemySkin = new ImageBrush();
        Rectangle canvasRect = new Rectangle
        {
            Tag = "enemy",
            Height = 45,
            Width = 45,
            Fill = enemySkin
        };

        Canvas.SetTop(canvasRect, rect.Y);
        Canvas.SetLeft(canvasRect, rect.X);
        m_canvas.Children.Add(canvasRect);

        int next = m_rand.Next(0, 7);

        switch (next)
        {
            case 0:
                enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader1.gif"));
                break;
            case 1:
                enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader2.gif"));
                break;
            case 2:
                enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader3.gif"));
                break;
            case 3:
                enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader4.gif"));
                break;
            case 4:
                enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader5.gif"));
                break;
            case 5:
                enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader6.gif"));
                break;
            case 6:
                enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader7.gif"));
                break;
            case 7:
                enemySkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/invader8.gif"));
                break;
        }

        return canvasRect;
    }

    private Rectangle createBullet(Rect rect, Brush fill, Brush stroke, string tag)
    {
        Rectangle bullet = new Rectangle
        {
            Tag = tag,
            Height = rect.Height,
            Width = rect.Width,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = 1
        };

        Canvas.SetTop(bullet, rect.Y);
        Canvas.SetLeft(bullet, rect.X);

        m_canvas.Children.Add(bullet);
        return bullet;
    }

    private Rectangle createPlayer(Rect rect)
    {
        Rectangle player = new Rectangle
        {
            Tag = EntityIds.Player,
            Height = rect.Height,
            Width = rect.Width,
        };
        ImageBrush playerSkin = new ImageBrush();
        playerSkin.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/player.png"));
        player.Fill = playerSkin;
        Canvas.SetTop(player, rect.Y);
        Canvas.SetLeft(player, rect.X);

        m_canvas.Children.Add(player);
        return player;
    }
}