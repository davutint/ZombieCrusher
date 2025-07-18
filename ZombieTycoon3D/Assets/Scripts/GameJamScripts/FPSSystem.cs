using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class FPSSystem : SystemBase
{
    private double lastTime;
    private int frameCount;
    private Entity fpsEntity;

    protected override void OnCreate()
    {
        lastTime = 0;
        frameCount = 0;

        // Eğer FPSComponent içeren entity sahnede yoksa, oluştur
        EntityQuery query = GetEntityQuery(typeof(FPSComponent));
        if (query.IsEmpty)
        {
            fpsEntity = EntityManager.CreateEntity(typeof(FPSComponent));
        }
        else
        {
            fpsEntity = query.GetSingletonEntity();
        }
    }

    protected override void OnUpdate()
    {
        frameCount++;
        double now = SystemAPI.Time.ElapsedTime;
        double elapsed = now - lastTime;

        if (elapsed >= 1.0)
        {
            float fps = (float)(frameCount / elapsed);
            frameCount = 0;
            lastTime = now;

            var fpsData = EntityManager.GetComponentData<FPSComponent>(fpsEntity);
            fpsData.currentFPS = fps;
            EntityManager.SetComponentData(fpsEntity, fpsData);
        }
    }
}