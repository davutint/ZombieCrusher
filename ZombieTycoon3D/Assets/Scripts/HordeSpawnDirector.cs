using UnityEngine;
using UnityEngine.AI;

public enum HordeFormation
{
    Cluster,
    Line,
    Wedge,
    Crescent
}

public sealed class HordeSpawnDirector
{
    private const float GoldenAngleRadians = 2.39996323f;

    private readonly Vector3[] projectedBoundsCorners = new Vector3[8];

    public int LastVisibleCandidateRejections { get; private set; }
    public int LastNavMeshCandidateRejections { get; private set; }

    public bool TryPlanHorde(
        Camera gameplayCamera,
        Vector3 playerPosition,
        int zombieCount,
        float spawnDistanceMin,
        float spawnDistanceMax,
        int placementAttempts,
        float navMeshSampleRadius,
        float viewportPadding,
        float zombieVisibilityRadius,
        float zombieVisibilityHeight,
        Vector3[] spawnPositions,
        out HordeFormation formation)
    {
        formation = HordeFormation.Cluster;
        LastVisibleCandidateRejections = 0;
        LastNavMeshCandidateRejections = 0;

        if (gameplayCamera == null
            || zombieCount <= 0
            || spawnPositions == null
            || spawnPositions.Length < zombieCount)
        {
            return false;
        }

        float minimumDistance = Mathf.Max(0f, spawnDistanceMin);
        float maximumDistance = Mathf.Max(minimumDistance, spawnDistanceMax);
        int attemptCount = Mathf.Max(1, placementAttempts);
        float sampleRadius = Mathf.Max(0.1f, navMeshSampleRadius);

        for (int attempt = 0; attempt < attemptCount; attempt++)
        {
            if (!TryFindHordeCenter(
                    playerPosition,
                    minimumDistance,
                    maximumDistance,
                    out Vector3 hordeCenter))
            {
                LastNavMeshCandidateRejections++;
                continue;
            }

            formation = SelectFormation(zombieCount);
            Vector3 inward = playerPosition - hordeCenter;
            inward.y = 0f;
            if (inward.sqrMagnitude <= Mathf.Epsilon)
            {
                inward = Vector3.forward;
            }
            else
            {
                inward.Normalize();
            }

            Vector3 lateral = Vector3.Cross(Vector3.up, inward).normalized;
            bool validFormation = true;

            for (int i = 0; i < zombieCount; i++)
            {
                Vector2 localOffset = GetFormationOffset(
                    formation,
                    i,
                    zombieCount);
                Vector3 requestedPosition =
                    hordeCenter
                    + lateral * localOffset.x
                    + inward * localOffset.y;

                if (!NavMesh.SamplePosition(
                        requestedPosition,
                        out NavMeshHit hit,
                        sampleRadius,
                        NavMesh.AllAreas))
                {
                    LastNavMeshCandidateRejections++;
                    validFormation = false;
                    break;
                }

                if (OverlapsExpandedCameraViewport(
                        gameplayCamera,
                        hit.position,
                        viewportPadding,
                        zombieVisibilityRadius,
                        zombieVisibilityHeight))
                {
                    LastVisibleCandidateRejections++;
                    validFormation = false;
                    break;
                }

                spawnPositions[i] = hit.position;
            }

            if (validFormation)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindHordeCenter(
        Vector3 playerPosition,
        float minimumDistance,
        float maximumDistance,
        out Vector3 hordeCenter)
    {
        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            randomDirection = Vector2.right;
        }
        else
        {
            randomDirection.Normalize();
        }

        float randomDistance = Random.Range(minimumDistance, maximumDistance);
        Vector3 requestedCenter = playerPosition
                                  + new Vector3(
                                      randomDirection.x,
                                      0f,
                                      randomDirection.y)
                                  * randomDistance;

        if (!NavMesh.SamplePosition(
                requestedCenter,
                out NavMeshHit hit,
                5f,
                NavMesh.AllAreas))
        {
            hordeCenter = default;
            return false;
        }

        Vector3 toCenter = hit.position - playerPosition;
        toCenter.y = 0f;
        float distanceToPlayer = toCenter.magnitude;
        if (distanceToPlayer < minimumDistance
            || distanceToPlayer > maximumDistance)
        {
            hordeCenter = default;
            return false;
        }

        hordeCenter = hit.position;
        return true;
    }

    private static HordeFormation SelectFormation(int zombieCount)
    {
        if (zombieCount < 6)
        {
            return HordeFormation.Cluster;
        }

        float roll = Random.value;
        if (roll < 0.35f)
        {
            return HordeFormation.Cluster;
        }

        if (roll < 0.6f)
        {
            return HordeFormation.Line;
        }

        if (roll < 0.82f)
        {
            return HordeFormation.Wedge;
        }

        return HordeFormation.Crescent;
    }

    private static Vector2 GetFormationOffset(
        HordeFormation formation,
        int index,
        int zombieCount)
    {
        switch (formation)
        {
            case HordeFormation.Line:
                return GetLineOffset(index, zombieCount);
            case HordeFormation.Wedge:
                return GetWedgeOffset(index);
            case HordeFormation.Crescent:
                return GetCrescentOffset(index, zombieCount);
            default:
                return GetClusterOffset(index);
        }
    }

    private static Vector2 GetClusterOffset(int index)
    {
        if (index == 0)
        {
            return Vector2.zero;
        }

        float angle = index * GoldenAngleRadians;
        float radius = 0.78f * Mathf.Sqrt(index);
        return new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius);
    }

    private static Vector2 GetLineOffset(int index, int zombieCount)
    {
        int columnCount = Mathf.CeilToInt(zombieCount * 0.5f);
        int row = index / columnCount;
        int column = index % columnCount;
        int entriesInRow = Mathf.Min(
            columnCount,
            zombieCount - row * columnCount);
        float lateral =
            (column - (entriesInRow - 1) * 0.5f) * 1.35f;
        float depth = row == 0 ? 0.65f : -0.65f;
        return new Vector2(lateral, depth);
    }

    private static Vector2 GetWedgeOffset(int index)
    {
        int row = 0;
        int firstIndexInRow = 0;
        while (firstIndexInRow + row + 1 <= index)
        {
            firstIndexInRow += row + 1;
            row++;
        }

        int indexInRow = index - firstIndexInRow;
        float lateral = (indexInRow - row * 0.5f) * 1.3f;
        float depth = -row * 1.15f;
        return new Vector2(lateral, depth);
    }

    private static Vector2 GetCrescentOffset(int index, int zombieCount)
    {
        if (zombieCount <= 1)
        {
            return Vector2.zero;
        }

        float normalized = index / (float)(zombieCount - 1);
        float angle = Mathf.Lerp(-70f, 70f, normalized) * Mathf.Deg2Rad;
        float ringOffset = (index & 1) == 0 ? 0f : 1.1f;
        float radius = 4.2f + ringOffset;
        float lateral = Mathf.Sin(angle) * radius;
        float depth = Mathf.Cos(angle) * radius - 4.2f - ringOffset * 0.3f;
        return new Vector2(lateral, depth);
    }

    private bool OverlapsExpandedCameraViewport(
        Camera gameplayCamera,
        Vector3 spawnPosition,
        float viewportPadding,
        float visibilityRadius,
        float visibilityHeight)
    {
        float radius = Mathf.Max(0.1f, visibilityRadius);
        float height = Mathf.Max(0.5f, visibilityHeight);
        Vector3 center = spawnPosition + Vector3.up * (height * 0.5f);
        Vector3 extents = new Vector3(radius, height * 0.5f, radius);

        int cornerIndex = 0;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    projectedBoundsCorners[cornerIndex++] = center
                        + Vector3.Scale(
                            extents,
                            new Vector3(x, y, z));
                }
            }
        }

        bool hasCornerInFront = false;
        float minimumX = float.PositiveInfinity;
        float maximumX = float.NegativeInfinity;
        float minimumY = float.PositiveInfinity;
        float maximumY = float.NegativeInfinity;
        float nearPlane = Mathf.Max(0f, gameplayCamera.nearClipPlane);

        for (int i = 0; i < projectedBoundsCorners.Length; i++)
        {
            Vector3 viewportPoint = gameplayCamera.WorldToViewportPoint(
                projectedBoundsCorners[i]);
            if (viewportPoint.z <= nearPlane)
            {
                continue;
            }

            hasCornerInFront = true;
            minimumX = Mathf.Min(minimumX, viewportPoint.x);
            maximumX = Mathf.Max(maximumX, viewportPoint.x);
            minimumY = Mathf.Min(minimumY, viewportPoint.y);
            maximumY = Mathf.Max(maximumY, viewportPoint.y);
        }

        if (!hasCornerInFront)
        {
            return false;
        }

        float padding = Mathf.Max(0f, viewportPadding);
        return maximumX >= -padding
               && minimumX <= 1f + padding
               && maximumY >= -padding
               && minimumY <= 1f + padding;
    }
}
