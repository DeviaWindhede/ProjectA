using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _breakableBoxPrefab;

    [SerializeField]
    private Vector3 _spawnCenter;

    [SerializeField]
    private Vector2 _spawnArea;

    [SerializeField, Min(0)]
    private float _minTime;

    [SerializeField, Min(0)]
    private float _maxTime;

    [SerializeField, Min(0)]
    private int _minBoxesToSpawn;

    [SerializeField, Min(0)]
    private int _maxBoxesToSpawn;

    private Timer timer;

    private void Awake() {
        SetTimer();
        SpawnBoxes();
        SpawnBoxes();
    }

    private void SetTimer()
    {
        float randomTime = Random.Range(_minTime, _maxTime + 1f);
        timer = new Timer(randomTime);
    }

    private void SpawnBoxes()
    {
        float boxCount = Random.Range(_minBoxesToSpawn, _maxBoxesToSpawn + 1f);
        Vector3 spawnSize = new Vector3(_spawnArea.x, 0, _spawnArea.y);
        Vector3 min = _spawnCenter - spawnSize / 2;
        Vector3 max = _spawnCenter + spawnSize / 2;
        for (int i = 0; i < boxCount; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                Random.Range(min.z, max.z)
            );
            Instantiate(_breakableBoxPrefab, randomPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer.Expired)
        {
            SpawnBoxes();
            SetTimer();
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_spawnCenter, new Vector3(_spawnArea.x, 0, _spawnArea.y));
    }
}
