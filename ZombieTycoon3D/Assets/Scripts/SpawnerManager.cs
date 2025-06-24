using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Zombie Prefabs")]
    public List<GameObject> zombiePrefabs; // Spawn edilecek zombi prefabları

    [Header("Spawn Settings")]
    public int maxZombiesInScene; // Aynı anda sahnede bulunabilecek maksimum zombi sayısı
    public Transform player; // Oyuncunun transformu
    public float spawnDistanceMin = 20f; // Spawn için minimum mesafe
    public float spawnDistanceMax = 50f; // Spawn için maksimum mesafe

    public Transform zombieParent; // Zombileri organize etmek için parent objesi

    [SerializeField] int currentZombieCount; // Şu anda sahnedeki toplam zombi sayısı

    private void OnEnable()
    {
        EventManager.OnZombieDead += HandleZombieDeath; // Zombi ölümü eventine abone ol
    }

    private void OnDisable()
    {
        EventManager.OnZombieDead -= HandleZombieDeath; // Zombi ölümü eventinden çık
    }

    private void Start()
    {
        StartCoroutine(SpawnZombiesRoutine()); // Zombi spawn işlemini sürekli olarak kontrol eden coroutine başlat
    }

    private IEnumerator SpawnZombiesRoutine()
    {
        while (true)
        {
            if (currentZombieCount < maxZombiesInScene - 10)
            {
                SpawnZombieHorde(10); // 10 adetlik bir horde spawn et
            }
            yield return new WaitForSeconds(1f); // Spawn işlemini belirli aralıklarla kontrol et
        }
    }

    private void SpawnZombieHorde(int count)
    {
        Vector3 hordeCenter = GetRandomSpawnPosition(); // Hordeyi spawn etmek için merkezi bir nokta seç

        if (hordeCenter != Vector3.zero)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)); // Hordeyi bir alana yay
                Vector3 spawnPosition = hordeCenter + offset;

                GameObject zombiePrefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Count)]; // Rastgele bir zombi prefabı seç
                GameObject zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity, zombieParent); // Zombiyi parent altında instantiate et
                currentZombieCount++;
            }
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomPoint = Vector3.zero;
        bool validPosition = false;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 randomDirection = Random.insideUnitSphere.normalized;
            randomDirection.y = 0; // Zemin seviyesinde kal

            float randomDistance = Random.Range(spawnDistanceMin, spawnDistanceMax);
            randomPoint = player.position + randomDirection * randomDistance;

            float distanceToPlayer = Vector3.Distance(randomPoint, player.position);
            if (distanceToPlayer >= spawnDistanceMin && distanceToPlayer <= spawnDistanceMax)
            {
                validPosition = true;
                break;
            }
        }

        return validPosition ? randomPoint : Vector3.zero;
    }

    private void HandleZombieDeath(Vector3 position)
    {
        currentZombieCount--; // Zombi sayısını azalt
        currentZombieCount = Mathf.Max(currentZombieCount, 0); // Negatif değere düşmesini engelle
    }
}

