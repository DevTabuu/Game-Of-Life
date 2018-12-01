using GameOfLife.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GameEngine
{
    [Serializable]
    class World : ICloneable
    {
        private static World _instance;
        private static readonly object _lock = new object();

        public static World GetInstance() // removed lock
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    if (File.Exists("save_file.gol"))
                        _instance = SaveUtil.ReadFromBinaryFile<World>("save_file.gol");
                    else
                        _instance = new World();
                }
                return _instance;
            }
        }

        private Dictionary<Vector2, Chunk> _chunkGrid;

        private World(Dictionary<Vector2, Chunk> chunkGrid)
        {
            _chunkGrid = chunkGrid;
        }

        private World() : this(new Dictionary<Vector2, Chunk>()) { }

        public void Update()
        {
            // Using a clone of the chunk grid since updating might change the size of the original.
            Dictionary<Vector2, Chunk> currentCloned = (Clone() as World)._chunkGrid;
            Dictionary<Vector2, Chunk> updatedChunks = new Dictionary<Vector2, Chunk>();

            foreach (KeyValuePair<Vector2, Chunk> pair in currentCloned)
            {
                Vector2 chunkLocation = pair.Key;
                Chunk chunk = pair.Value;

                if (chunk.GetState().Equals(ChunkState.UNLOADED))
                    _chunkGrid.Remove(chunkLocation);
                else
                    updatedChunks.Add(chunkLocation, chunk.Update());
            }

            foreach (KeyValuePair<Vector2, Chunk> pair in updatedChunks)
            {
                _chunkGrid[pair.Key] = pair.Value.Clone() as Chunk;
            }
        }

        public void GenerateChunk(Vector2 chunkLocation, bool force)
        {
            if (!_chunkGrid.ContainsKey(chunkLocation))
            {
                if (force)
                {
                    _chunkGrid.Add(chunkLocation, new Chunk(chunkLocation));
                    return;
                }
                    

                byte activeNeighborChunks = 0;
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        Vector2 offset = new Vector2(x, y);
                        Vector2 neighborChunkLocation = offset + chunkLocation;

                        Chunk chunk = GetChunkAt(neighborChunkLocation, false);
                        if (chunk != null && chunk.GetState().Equals(ChunkState.ACTIVE))
                            activeNeighborChunks++;
                    }
                }

                if(activeNeighborChunks > 0)
                    _chunkGrid.Add(chunkLocation, new Chunk(chunkLocation));
            }
        }

        public Vector2 CellToChunkLocation(Vector2 worldLocation)
        {
            return new Vector2(worldLocation.X >> Chunk.GRID_POWER, worldLocation.Y >> Chunk.GRID_POWER);
        }

        public Chunk GetChunkAt(Vector2 chunkLocation, bool generate)
        {
            if (!_chunkGrid.ContainsKey(chunkLocation))
            {
                if (!generate)
                    return null;
                else
                {
                    GenerateChunk(chunkLocation, false);
                    return GetChunkAt(chunkLocation, false);
                }
            }
            return _chunkGrid[chunkLocation];
        }

        public Chunk GetChunkAtCell(Vector2 cellLocation, bool generate)
        {
            return GetChunkAt(CellToChunkLocation(cellLocation), generate);
        }

        public object Clone()
        {
            Dictionary<Vector2, Chunk> clone = new Dictionary<Vector2, Chunk>();
            clone = _chunkGrid.ToDictionary(entry => entry.Key, entry => entry.Value.Clone() as Chunk);
            return new World(clone);
        }

        public void Save()
        {
            SaveUtil.WriteToBinaryFile<World>("save_file.gol", this);
        }

        public void Reset()
        {
            _chunkGrid.Clear();
        }
    }
}
