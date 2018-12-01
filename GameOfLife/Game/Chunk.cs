using System;

namespace GameEngine
{
    [Serializable]
    class Chunk : ICloneable
    {

        public static readonly int GRID_POWER   = 3;
        public static readonly int GRID_SIZE    = 1 << GRID_POWER;

        /// <summary>
        /// Wraps a vectors values based on the chunk's grid size. Essentially converting world location to local location.
        /// </summary>
        /// <param name="location">World location</param>
        /// <returns>Local location</returns>
        public static Vector2 WrapLocation(Vector2 location)
        {
            int x = (location.X % GRID_SIZE + GRID_SIZE) % GRID_SIZE;
            int y = (location.Y % GRID_SIZE + GRID_SIZE) % GRID_SIZE;
            return new Vector2(x, y);
        }

        private bool[,] _cellGrid;
        private Vector2 _location;
        private ChunkState _chunkState;

        private World _world;

        private Chunk(Vector2 location, bool[,] cellGrid, ChunkState state)
        {
            _cellGrid           = cellGrid;
            _location           = location;
            _chunkState         = state;
            _world = World.GetInstance();
        }

        public Chunk(Vector2 location) : this(location, new bool[GRID_SIZE, GRID_SIZE], ChunkState.INACTIVE) { }

        /// <summary>
        /// Calculated which cells in a chunk should live and die.
        /// </summary>
        /// <returns>
        /// A clone of this chunk with the updated values.
        /// </returns>
        public Chunk Update()
        {
            Chunk copiedChunk = (Chunk) Clone() as Chunk;

            int totalAliveCount = 0;

            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    if (_chunkState.Equals(ChunkState.INACTIVE) && (x != 0 && y != 0 && x != GRID_SIZE - 1 && y != GRID_SIZE - 1)) continue; // TODO: Make incative chunks less heavy

                    Vector2 cellChunkLocation = new Vector2(x, y);
                    Vector2 cellWorldLocation = cellChunkLocation + new Vector2(GRID_SIZE * _location.X, GRID_SIZE * _location.Y);

                    bool alive = GetCellAt(cellChunkLocation);

                    if (alive)
                        totalAliveCount++;

                    byte aliveNeighbors = 0;

                    for(int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (i == 0 && j == 0)
                                continue;
                         
                            Vector2 cellToCheckWorldLocation = cellWorldLocation + new Vector2(j, i);
                            Chunk chunk = _world.GetChunkAtCell(cellToCheckWorldLocation, true);

                            if (chunk == null)
                                continue;

                            Vector2 cellToCheckLocalLocation = WrapLocation(cellToCheckWorldLocation);

                            if (chunk.GetCellAt(cellToCheckLocalLocation))
                                aliveNeighbors++;
                        }
                    }

                    if (aliveNeighbors < 2 || aliveNeighbors > 3)
                        alive = false;
                    else if (aliveNeighbors == 3)
                        alive = true;

                    copiedChunk.SetCellAt(cellChunkLocation, alive);
                }
            }
            copiedChunk.GetState(true);

            return copiedChunk;
        }

        public bool GetCellAt(Vector2 chunkLocation)
        {
            return _cellGrid[chunkLocation.X, chunkLocation.Y];
        }

        public void SetCellAt(Vector2 chunkLocation, bool value)
        {
            _cellGrid[chunkLocation.X, chunkLocation.Y] = value;
        }

        public ChunkState GetState(bool forceUpdate)
        {
            if (forceUpdate)
            {
                // Check if chunk has alive cells
                foreach (bool cell in _cellGrid)
                {
                    if (cell)
                    {
                        SetState(ChunkState.ACTIVE);
                        return _chunkState;
                    }
                }

                // Check if neighbor chunks are active
                byte activeNeighbors = 0;
                for (int x = -2; x < 2; x++)
                {
                    for (int y = -2; y < 2; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        Vector2 offset = new Vector2(x, y);
                        Vector2 chunkLocation = _location + offset;

                        Chunk chunk = _world.GetChunkAt(chunkLocation, false);

                        if (chunk != null && chunk.GetState(false).Equals(ChunkState.ACTIVE))
                            activeNeighbors++;
                    }
                }
                if (activeNeighbors == 0)
                {
                    SetState(ChunkState.UNLOADED);
                    return _chunkState;
                }

                SetState(ChunkState.INACTIVE);
                return _chunkState;
            }
            else
                return _chunkState;
        }

        public ChunkState GetState()
        {
            return GetState(false);
        }

        public void SetState(ChunkState value)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Vector2 offset = new Vector2(x, y);
                    Vector2 chunkLocation = _location + offset;

                    Chunk chunk = _world.GetChunkAt(chunkLocation, false);

                    if (chunk != null && chunk.GetState().Equals(ChunkState.UNLOADED))
                        _chunkState = ChunkState.INACTIVE;
                    else if (chunk == null && value.Equals(ChunkState.ACTIVE))
                        _world.GenerateChunk(chunkLocation, true);
                }
            }

            _chunkState = value;
        }

        public Vector2 GetLocation()
        {
            return _location;
        }

        public object Clone()
        {
            return new Chunk(_location, _cellGrid.Clone() as bool[,], _chunkState);
        }

    }

    enum ChunkState
    {
        ACTIVE,
        INACTIVE,
        UNLOADED
    }
}
