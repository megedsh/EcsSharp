using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using EcsSharp;
using EcsSharp.Events.EventArgs;
using EcsSharp.Logging;
using EcsSharp.Logging.BuiltIn;
using SpaceInvadersEcs.Components;
using SpaceInvadersEcs.Systems;

namespace SpaceInvadersEcs
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int  m_bulletTimerLimit = 90;
        private          bool m_gameOver;

        private readonly PlayerSystem       m_playerSystem;
        private readonly EnemyBulletSystem  m_enemyBulletSystem;
        private readonly PlayerBulletSystem m_playerBulletSystem;
        private readonly InvadersSystem     m_invaderSystem;
        private readonly GameStateSystem    m_gameStateSystem;

        public MainWindow()
        {
            Thread.CurrentThread.Name = "Main";
            CommonLogManager.InitLogProvider(new ConsoleCommonLogsAdapter() { RootLoggerLevel = CommonLogLevel.Info});
            InitializeComponent();

            myCanvas.Focus();
            IEcsRepo ecsRepo = new DefaultEcsRepoFactory().Create();

            m_playerSystem = new PlayerSystem(ecsRepo);
            m_enemyBulletSystem = new EnemyBulletSystem(ecsRepo, m_bulletTimerLimit);
            m_playerBulletSystem = new PlayerBulletSystem(ecsRepo);
            m_invaderSystem = new InvadersSystem(ecsRepo);
            RendererSystem _ = new RendererSystem(ecsRepo, myCanvas, msgCtrl);
            m_gameStateSystem = new GameStateSystem(ecsRepo);

            ecsRepo.Events.SpecificUpdated[EntityIds.GameState] += gameStateChanged;

            m_playerSystem.Start();
            m_gameStateSystem.Start();
            m_playerBulletSystem.Start();
            m_invaderSystem.Start();
            m_enemyBulletSystem.Start();
        }

        private void gameStateChanged(EntityUpdatedEventArgs obj)
        {
            GameStates gameStates = obj.Entity.GetComponent<GameStates>();
            InvaderCount invaderCount = obj.Entity.GetComponent<InvaderCount>();

            switch (gameStates)
            {
                case GameStates.Running:
                    showMessage($"Invader count : {invaderCount.Data}");
                    break;
                case GameStates.Won:
                    stopGame();
                    showMessage("Nice.You won - press enter to play again");
                    break;
                case GameStates.Lost:
                    stopGame();
                    showMessage("Lost !! - press enter to play again");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void showMessage(string message)
        {
            msgCtrl.Dispatcher.BeginInvoke(() => { msgCtrl.Content = message; });
        }

        private void keyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                m_playerSystem.KeyDown(e.Key);
            }

            if (e.Key == Key.Right)
            {
                m_playerSystem.KeyDown(e.Key);
            }
        }

        private void keyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                m_playerSystem.KeyUp(e.Key);
            }

            if (e.Key == Key.Right)
            {
                m_playerSystem.KeyUp(e.Key);
            }

            if (e.Key == Key.Space)
            {
                m_playerBulletSystem.Fire();
            }

            if (e.Key == Key.Enter && m_gameOver)
            {
                resetGame();
            }
        }

        private void resetGame()
        {
            m_gameOver = false;
            m_playerSystem.Reset();
            m_enemyBulletSystem.Reset();
            m_playerBulletSystem.Reset();
            m_invaderSystem.Reset();
            m_gameStateSystem.Reset();

            m_playerSystem.Start();
            m_gameStateSystem.Start();
            m_enemyBulletSystem.Start();
            m_playerBulletSystem.Start();
            m_invaderSystem.Start();
        }

        private void stopGame()
        {
            m_gameOver = true;
            m_playerSystem.Stop();
            m_playerBulletSystem.Stop();
            m_invaderSystem.Stop();
            m_enemyBulletSystem.Stop();
            m_gameStateSystem.Stop();
        }
    }
}