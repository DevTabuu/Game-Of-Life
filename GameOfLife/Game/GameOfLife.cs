namespace GameEngine
{
    public class GameOfLife : AbstractGame
    {
        private WorldView _worldView;
        private bool _paused;
        private float _tickTime;
        private float _tickTimer;

        public override void GameStart()
        {
            _worldView  = new WorldView(5);
            _paused     = true;
            _tickTime   = 0.01f;
            _tickTimer  = 0f;

            GameEngine.GetInstance().SetVSync(false);
        }

        public override void GameEnd()
        {
            World.GetInstance().Save();
        }

        public override void Update()
        {
            if (GameEngine.GetInstance().GetKeyDown(Key.P))
                _paused ^= true;

            if (GameEngine.GetInstance().GetKeyDown(Key.R))
                World.GetInstance().Reset();

            if (!_paused)
                _tickTimer += GameEngine.GetInstance().GetDeltaTime();

            // Do one update on keypress
            else if(GameEngine.GetInstance().GetKeyDown(Key.S))
                World.GetInstance().Update();

            if (_tickTimer >= _tickTime)
            {
                World.GetInstance().Update();
                _tickTimer = 0f;
            }
            _worldView.Update();
        }

        public override void Paint()
        {
            _worldView.Draw();
        }
    }
}
