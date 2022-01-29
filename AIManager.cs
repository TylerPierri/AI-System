using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIManager : MonoBehaviour
{
    private Game_Info info;
    [Header("General info")]
    public bool AttackPlayer;

    [Header("Bot info")]
    [Range(1, 40)] public float HoverBotCircleRadius;
    [Range(1, 40)] public float DroneCircleRadius;
    [Range(1, 40)] public float HeavyBotCircleRadius;
    [Range(1, 40)] public float ShieldBotCircleRadius;
    public int MaxAmountOfEnemiesAttacking;

    private Transform Target;
    public List<EnemyAgent> enemies = new List<EnemyAgent>();

    public List<EnemyAgent> AttackingEnemies = new List<EnemyAgent>();

    private void Awake()
    {     
        info = FindObjectOfType<Game_Info>();
        SetDifficulty();
    }

    void SetDifficulty() // changes the values based of difficulty chosen
    {
        switch (info.difficulty)
        {
            case Game_Info.Difficulty.Easy: // easy
                HoverBotCircleRadius = HoverBotCircleRadius / 1.2f;
                DroneCircleRadius = DroneCircleRadius / 1.2f;
                HeavyBotCircleRadius = HeavyBotCircleRadius / 1.2f;
                MaxAmountOfEnemiesAttacking = MaxAmountOfEnemiesAttacking - 1;
                break;

            // Normal means the values are unchanged

            case Game_Info.Difficulty.Hard: // hard
                HoverBotCircleRadius = HoverBotCircleRadius * 1.2f;
                DroneCircleRadius = DroneCircleRadius * 1.2f;
                HeavyBotCircleRadius = HeavyBotCircleRadius * 1.2f;
                MaxAmountOfEnemiesAttacking = MaxAmountOfEnemiesAttacking + 1;
                break;
        }

    }

    public void CheckForAttackPermission() // this is to check if the Ai is allowed to attack the player
    {
        for (int i = 0; i < AttackingEnemies.Count; i++)
        {
            AttackingEnemies[i].AllowedToAttack = true;
        }
       
    }

    public void MakeAgentCircleTarget() // sets the ring around the player for every enemy in scene
    {
        Target = GameObject.FindWithTag("Player").transform;
        for (int i = 0; i < enemies.Count; i++)
        {
            switch (enemies[i].EnemyType)
            {
                case EnemyAgent.Enemy_Type.HoverBot:
                    enemies[i].PlayerLocation = new Vector3(
                    Target.transform.position.x + HoverBotCircleRadius * Mathf.Cos(2 * Mathf.PI * i / enemies.Count),
                    Target.transform.position.y,
                    Target.transform.position.z + HoverBotCircleRadius * Mathf.Sin(2 * Mathf.PI * i / enemies.Count));
                    break;

                case EnemyAgent.Enemy_Type.Drone:
                    enemies[i].PlayerLocation = new Vector3(
                    Target.transform.position.x + DroneCircleRadius * Mathf.Cos(2 * Mathf.PI * i / enemies.Count),
                    Target.transform.position.y,
                    Target.transform.position.z + DroneCircleRadius * Mathf.Sin(2 * Mathf.PI * i / enemies.Count));
                    break;

                case EnemyAgent.Enemy_Type.HeavyBot:
                    enemies[i].PlayerLocation = new Vector3(
                    Target.transform.position.x + HeavyBotCircleRadius * Mathf.Cos(2 * Mathf.PI * i / enemies.Count),
                    Target.transform.position.y,
                    Target.transform.position.z + HeavyBotCircleRadius *Mathf.Sin(2 * Mathf.PI * i / enemies.Count));
                    break;

               
            }

        }

        //Random.Range(HoverBotCircleRadius - 1, HoverBotCircleRadius + 1); - replace for random distance within circle
    }
}
