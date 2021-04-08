using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding.Examples
{
    [RequireComponent(typeof(Seeker))]
    public class EndingCondition : MonoBehaviour
    {
        public int hLimit = 1000;
        public float _maxDistance = 10;

        public Transform targetPoint;

        void OnEnable()
        {
            Seeker seeker = GetComponent<Seeker>();

            seeker.pathCallback = OnPathComplete;

            // Create a new XPath with a custom ending condition
            XPath p = XPath.Construct(transform.position, targetPoint.position, null);
            p.endingCondition = new CustomEndingCondition(p, hLimit);
            p.endingCondition = new DistanceEndingCondition(p, _maxDistance);
            //p.endingCondition = new EndingConditionProximity(p, _maxDistance);
            

            // Draw a line in black from the start to the target point
            Debug.DrawLine(transform.position, targetPoint.position, Color.black);

            seeker.StartPath(p);
        }

        public class CustomEndingCondition : ABPathEndingCondition
        {
            public int hLimit = 10000;

            public CustomEndingCondition(ABPath path, int lim) : base(path)
            {
                hLimit = lim;
            }

            public override bool TargetFound(PathNode node)
            {
                return node.H < hLimit || node.node == abPath.endNode;
            }
        }

        public class DistanceEndingCondition : ABPathEndingCondition
        {
            public float maxDistance = 10;

            public DistanceEndingCondition(ABPath path, float distance) : base(path)
            {
                maxDistance = distance;
            }

            public override bool TargetFound(PathNode node)
            {
                return ((Vector3)node.node.position - abPath.originalEndPoint).sqrMagnitude <= maxDistance*maxDistance;
            }
        }

        public void OnPathComplete(Path p)
        {
            Debug.Log("Got Callback");

            if (p.error)
            {
                Debug.Log("Ouch, the path returned an error");
                Debug.LogError(p.errorLog);
                return;
            }

            List<Vector3> path = p.vectorPath;

            for (int j = 0; j < path.Count - 1; j++)
            {
                // Plot segment j to j+1 with a nice color got from Pathfinding.AstarMath.IntToColor
                Debug.DrawLine(path[j], path[j + 1], AstarMath.IntToColor(1, 0.5F), 1);
            }
        }
    }
}