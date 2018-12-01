using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine
{
    class WorldView
    {
        private GameEngine _gameEngine;
        private float _scale;
        private Vector2f _offset;
        private Vector2 _center;
        private Vector2 _mouseholdLocation;
        private bool _dragMode;

        private Vector2 _roundedOffset {
            get
            {
                return new Vector2((int)Math.Floor(_offset.X), (int)Math.Floor(_offset.Y));
            }
            set
            {
                _offset = new Vector2f(value.X, value.Y);
            }
        }

        public WorldView(float scale)
        {
            _gameEngine = GameEngine.GetInstance();
            _scale      = scale;
            _offset     = Vector2f.zero;
            _center     = new Vector2(_gameEngine.GetScreenWidth() / 2, _gameEngine.GetScreenHeight() / 2);
        }

        public Vector2 ScreenToWorld(Vector2 location)
        {
            Vector2 mouseLocation = location - _center;

            // Calculate chunk offset based on global offset
            Vector2 offset = new Vector2(_roundedOffset.X >> Chunk.GRID_POWER << Chunk.GRID_POWER, _roundedOffset.Y >> Chunk.GRID_POWER << Chunk.GRID_POWER);
            return new Vector2((int)Math.Floor(mouseLocation.X / _scale), (int)Math.Floor(mouseLocation.Y / _scale)) + offset;
        }

        public void Update()
        {
            // Update visual controls 

            Vector2f delta = Vector2f.zero;
            float moveSpeed = 5f / _scale;

            if (_gameEngine.GetKey(Key.Left))
                delta.X -= moveSpeed;
            else if (_gameEngine.GetKey(Key.Right))
                delta.X += moveSpeed;

            if (_gameEngine.GetKey(Key.Up))
                delta.Y -= moveSpeed;
            else if (_gameEngine.GetKey(Key.Down))
                delta.Y += moveSpeed;

            _offset += delta;

            if (_gameEngine.GetKey(Key.Add))
                _scale += 0.1f;
            else if (_gameEngine.GetKey(Key.Subtract))
                _scale -= 0.1f;


            if (_gameEngine.GetMouseButtonDown(0) && _gameEngine.GetMouseButtonDown(1))
            {
                _mouseholdLocation = _gameEngine.GetMousePosition() - _roundedOffset;
                _dragMode = true;
            }
            else if (_gameEngine.GetMouseButtonUp(0) && _gameEngine.GetMouseButtonUp(1))
            {
                _dragMode = false;
            }
            else if (_gameEngine.GetMouseButton(0) && _gameEngine.GetMouseButton(1) && _dragMode)
            {
                float movement = moveSpeed * 0.002f;
                Vector2 relativeMousePosition = _gameEngine.GetMousePosition() - _mouseholdLocation;
                // relativeMousePosition += _center;
                // Vector2f newOffset = new Vector2f(relativeMousePosition.X * movement, relativeMousePosition.X * movement);
                _roundedOffset = relativeMousePosition - relativeMousePosition - relativeMousePosition;
            } 
            else if (_gameEngine.GetMouseButton(0))
            {
                Vector2 worldPoint = ScreenToWorld(_gameEngine.GetMousePosition());
                Vector2 chunkPoint = World.GetInstance().CellToChunkLocation(worldPoint);

                if(World.GetInstance().GetChunkAt(chunkPoint, false) == null)
                    World.GetInstance().GenerateChunk(chunkPoint, true);

                Chunk chunk = World.GetInstance().GetChunkAtCell(worldPoint, false);
                Vector2 localCellLocation = Chunk.WrapLocation(worldPoint);

                chunk.SetCellAt(localCellLocation, true);

                chunk.SetState(ChunkState.ACTIVE);
            }
            else if (_gameEngine.GetMouseButton(1))
            {
                Vector2 worldPoint = ScreenToWorld(_gameEngine.GetMousePosition());
                Vector2 chunkPoint = World.GetInstance().CellToChunkLocation(worldPoint);

                if (World.GetInstance().GetChunkAt(chunkPoint, false) == null)
                    return;

                Chunk chunk = World.GetInstance().GetChunkAtCell(worldPoint, false);
                Vector2 localCellLocation = Chunk.WrapLocation(worldPoint);

                if (chunk.GetCellAt(localCellLocation))
                    chunk.SetCellAt(localCellLocation, false);

                chunk.GetState();
            }
        }

        public void Draw()
        {
            Vector2 maxCellDrawCount = new Vector2((int)Math.Floor(_gameEngine.GetScreenWidth() / _scale), (int)Math.Floor(_gameEngine.GetScreenHeight() / _scale));
            Vector2 maxChunkDrawCount = new Vector2(maxCellDrawCount.X >> Chunk.GRID_POWER, maxCellDrawCount.Y >> Chunk.GRID_POWER);

            for (int x = -(maxChunkDrawCount.X / 2) - 1; x < (maxChunkDrawCount.X / 2) + 1; x++)
            {
                for (int y = -(maxChunkDrawCount.Y / 2) - 1; y < (maxChunkDrawCount.Y / 2) + 1; y++)
                {
                    Vector2 drawLocation = new Vector2((int)(x * Chunk.GRID_SIZE * _scale), (int)(y * Chunk.GRID_SIZE * _scale)) + _center;
                    Vector2 chunkLocation = World.GetInstance().CellToChunkLocation(new Vector2(x * Chunk.GRID_SIZE, y * Chunk.GRID_SIZE) + _roundedOffset);
                    Chunk chunk = World.GetInstance().GetChunkAt(chunkLocation, false);

                    if (chunk != null)
                    {
                        Color background;
                        Color forground;
                        switch (chunk.GetState())
                        {
                            case ChunkState.ACTIVE:
                                background = new Color(59, 122, 87);
                                forground = Color.Black;
                                break;

                            case ChunkState.INACTIVE:
                                background = new Color(124, 185, 232);
                                forground = new Color(0, 0, 0, 50);
                                break;

                            case ChunkState.UNLOADED:
                                background = Color.Gray;
                                forground = new Color(0, 0, 0, 25);
                                break;

                            default:
                                background = Color.AppelBlauwZeeGroen;
                                forground = Color.Black;
                                break;
                        }

                        _gameEngine.SetColor(background);
                        _gameEngine.FillRectangle(drawLocation.X, drawLocation.Y, _scale * Chunk.GRID_SIZE, _scale * Chunk.GRID_SIZE);

                        _gameEngine.SetColor(forground);
                        for (int cx = 0; cx < Chunk.GRID_SIZE; cx++)
                        {
                            for (int cy = 0; cy < Chunk.GRID_SIZE; cy++)
                            {
                                bool cell = chunk.GetCellAt(new Vector2(cx, cy));

                                Vector2 cellDrawLocation = drawLocation + new Vector2((int)(cx * _scale), (int)(cy * _scale));

                                if (cell)
                                    _gameEngine.FillRectangle(cellDrawLocation.X, cellDrawLocation.Y, _scale, _scale);
                                else
                                    _gameEngine.DrawRectangle(cellDrawLocation.X, cellDrawLocation.Y, _scale, _scale);
                            }
                        }
                    }
                }
            }
        }
    }
}
