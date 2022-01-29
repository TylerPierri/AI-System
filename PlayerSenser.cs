using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSenser : MonoBehaviour
{
    private Game_Info info;
    public float ViewRadius;
    [Range(0,360)] public float ViewAngle;

    public LayerMask TargetMask;
    public LayerMask ObsticleMask;

    //[HideInInspector]
    public List<Transform> VisibleTargets = new List<Transform>();
    private void Start()
    {
        info = FindObjectOfType<Game_Info>();
        SetDifficulty();

        ViewAngle = Camera.main.fieldOfView + 45;
        StartCoroutine(FindTargetsWithDelay(0.2f));
    }
    void SetDifficulty()
    {
        switch (info.difficulty)
        {
            case Game_Info.Difficulty.Easy: // easy
                ViewRadius = ViewRadius / 1.2f;
                break;

            case Game_Info.Difficulty.Hard: // hard
                ViewRadius = ViewRadius * 1.5f;
                break;
        }
    }
    IEnumerator FindTargetsWithDelay(float Delay)
    {
        while(true)
        {
            yield return new WaitForSeconds(Delay);
            FindVisibleTargets();
        }
    }

    void FindVisibleTargets()
    {
        VisibleTargets.Clear();
        Collider[] TargetsInViewRadius = Physics.OverlapSphere(transform.position, ViewRadius, TargetMask);

        for (int i = 0; i < TargetsInViewRadius.Length; i++)
        {
            //AIManager ManagerAI = FindObjectOfType<AIManager>();
            Transform Target = TargetsInViewRadius[i].transform;
            Vector3 DirectionToTarget = (Target.position - transform.position).normalized;
            if(Vector3.Angle (transform.forward, DirectionToTarget) < ViewAngle / 2)
            {
                float DistanceToTarget = Vector3.Distance(transform.position, Target.position);

                if(!Physics.Raycast(transform.position, DirectionToTarget, DistanceToTarget, ObsticleMask))
                {
                    // the enemy is in view with no obsticales in the way
                    VisibleTargets.Add(Target);
                }
            }
        }

        for (int i = 0; i < VisibleTargets.Count; i++) // looks through all found objects
        {
            //Debug.Log(VisibleTargets[i].name);
            if(VisibleTargets[i].tag == "Drone" || VisibleTargets[i].tag == "Hover Bot" || VisibleTargets[i].tag == "Heavy Bot")
            {
                //Debug.Log(VisibleTargets[i].tag);
                VisibleTargets[i].gameObject.GetComponent<EnemyAgent>().Insights(0.5f);
            } 
            
            if(VisibleTargets[i].tag == "Shield Bot")
            {
                VisibleTargets[i].gameObject.GetComponent<ShieldBotAgent>().Insights(0.5f);
            }

            if(VisibleTargets[i].tag == "Turret")
            {
                VisibleTargets[i].gameObject.GetComponent<Turret>().Insights(0.5f);
            }

            if (VisibleTargets[i].tag == "Turret Head")
            {
                VisibleTargets[i].gameObject.GetComponent<TurretHeadSeen>().SeenByHead(0.5f);
            }
        }

    }
    public Vector3 DirectionFromAngle(float AngleInDegrees, bool AngleIsGlobal)
    {
        if(!AngleIsGlobal)
        {
            AngleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(AngleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(AngleInDegrees * Mathf.Deg2Rad));
    }
}
