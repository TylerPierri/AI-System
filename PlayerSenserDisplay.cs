using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(PlayerSenser))]
public class PlayerSenserDisplay : Editor
{
    void OnSceneGUI() // this draws the radius around the player with the angle to help the developer see how much the radius is
    {
        PlayerSenser Senser = (PlayerSenser)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(Senser.transform.position, Vector3.up, Vector3.forward, 360, Senser.ViewRadius);
        Vector3 ViewAngleA = Senser.DirectionFromAngle(-Senser.ViewAngle / 2, false);
        Vector3 ViewAngleB = Senser.DirectionFromAngle(Senser.ViewAngle / 2, false);

        Handles.DrawLine(Senser.transform.position, Senser.transform.position + ViewAngleA * Senser.ViewRadius);
        Handles.DrawLine(Senser.transform.position, Senser.transform.position + ViewAngleB * Senser.ViewRadius);

        Handles.color = Color.red;
        foreach (Transform VisableTarget in Senser.VisibleTargets)
        {
            Handles.DrawLine(Senser.transform.position, VisableTarget.position);
        }
    }
}
