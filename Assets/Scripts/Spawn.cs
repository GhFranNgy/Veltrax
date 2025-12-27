using UnityEngine;

public class Spawn : MonoBehaviour
{
    public int numberOfEnemy;
    public GameObject objectToSpawn;

    public Transform player;
    public float speed;
    public float separationDistance = 0.5f;
    public float separationForce = 1f;

    private GameObject[] enemies;
    private Animator[] animators;

    void Start()
    {
        enemies = new GameObject[numberOfEnemy];
        animators = new Animator[numberOfEnemy];

        for (int i = 0; i < numberOfEnemy; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-11, 11), 0f, Random.Range(-11, 10));
            GameObject enemy = Instantiate(objectToSpawn, pos, Quaternion.identity);

            enemies[i] = enemy;
            animators[i] = enemy.GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        if (!player) return;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject obj = enemies[i];
            if (!obj) continue;

            Vector3 pos = obj.transform.position;

            // Follow movement
            Vector3 direction = (player.position - pos).normalized;
            Vector3 move = direction * speed;

            // Separation
            Vector3 separation = Vector3.zero;
            for (int j = 0; j < enemies.Length; j++)
            {
                if (i == j) continue;
                GameObject other = enemies[j];
                if (!other) continue;

                float dist = Vector3.Distance(pos, other.transform.position);
                if (dist < separationDistance)
                {
                    separation += (pos - other.transform.position).normalized *
                                  (separationDistance - dist) * separationForce;
                }
            }

            // Animation
            animators[i].SetFloat("Speed", (move + separation).magnitude);

            // Rotation
            Quaternion targetRot = Quaternion.LookRotation(direction);
            obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, targetRot, 5f * Time.deltaTime);

            // Movement (keep y = 0)
            pos += (move + separation) * Time.deltaTime;
            pos.y = 0f;
            obj.transform.position = pos;
        }
    }
}
