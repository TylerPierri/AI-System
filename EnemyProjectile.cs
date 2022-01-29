using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Game_Info info;

    [HideInInspector] public float Damage;
    [HideInInspector] public float SpeedOfProjectile;
    private float Accuracy;
    private Vector3 ShootDirection;

    [Header("Projectile Settings")]
    [Range(0.3f,0.5f)] public float ChanceToCountAsHit;
    public bool DoesItExplode;
    public ParticleSystem a, b, c;
    public Blast blast;


    public float DestroyAfter;

    public void SetUp(Vector3 PlayerLocation)
    {
        ShootDirection = PlayerLocation;

        if(DoesItExplode)
            blast.enabled = false;

        info = FindObjectOfType<Game_Info>();
        switch (info.difficulty)
        {
            case Game_Info.Difficulty.Easy: // easy
                ChanceToCountAsHit = ChanceToCountAsHit + 0.2f;
                break;

            case Game_Info.Difficulty.Hard: // hard
                ChanceToCountAsHit = ChanceToCountAsHit - 0.2f;
                break;
        }
    } 

    // Add Blooom to projectiles
    private void FixedUpdate()
    {

        //transform.position += (transform.position - ShootDirection).normalized * SpeedOfProjectile;
        transform.position = Vector3.MoveTowards(transform.position, ShootDirection, SpeedOfProjectile * Time.deltaTime);

        if (DestroyAfter <= 0)
        {
            StartCoroutine(HitObject());
        }
        else
            DestroyAfter -= 1 * Time.deltaTime;

        if (Vector3.Distance(transform.position, GameObject.FindWithTag("Player").transform.position) < 1f)
        {
            if (Random.value < ChanceToCountAsHit)
            {
                DamagePlayer();
                StartCoroutine(HitObject());
                //Debug.Log("Hit Player");
            }
        }
    }

    void OnTriggerEnter(Collider Hit)
    {
        if(Hit.gameObject.layer == LayerMask.GetMask("Player"))
        {
            DamagePlayer();
            StartCoroutine(HitObject());
            Debug.Log("Hit Player");            
        }
        else
        {
            StartCoroutine(HitObject());
        }
    }

    IEnumerator HitObject()
    {
        // hit player
        if(DoesItExplode && blast != null)
        {
            blast.enabled = true;
            a.Play();
            b.Play();
            c.Play();

            StartCoroutine(blast.BlastWave(0.1f));
            yield return new WaitForSeconds(0.5f);          
        }

        Destroy(gameObject);
    }

    void DamagePlayer()
    {
        //damage player
    }
}
