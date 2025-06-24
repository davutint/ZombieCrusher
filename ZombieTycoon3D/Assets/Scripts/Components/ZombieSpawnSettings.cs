namespace Components
{
    // Components/ZombieSpawnSettings.cs
    using Unity.Entities;
    using Unity.Mathematics;

    public struct ZombieSpawnSettings : IComponentData
    {
        public Entity prefab;
        public int    totalToSpawn;
        public int    batchSize;
        public float  interval;
        public float  radius;
        public Random rnd;

        public int    spawned;     // ← ekledik
    }

    public struct SpawnTimer : IComponentData { public float value; }

}