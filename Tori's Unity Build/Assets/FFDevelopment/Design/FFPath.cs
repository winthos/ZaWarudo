using UnityEngine;
using System.Collections;

//////////////////////////////////////////////////////
// Author: Micah Rust
// Data: 29/6/2015
// Purpose: A path script to interpolate between
//      points. Transfer betweena point in space to a
//      Line/curve. I have tried to make this as
//      flexable as possible. Sky's the limit. :)
//
//      The only added functionaity
//      you may want to add if changing the points from
//      Vector3[] to FFRef<Vector3>[] which will allow
//      for moving points on a path at a minor performance
//      cost. This would take some doing, but might be
//      more useful then seting points manually.
//
// Usage: AI enemy movement, Camera movement, anything
//      which needs to move along a line/curve.
///////////////////////////////////////////////////////

public class FFPath : MonoBehaviour
{
    // Lower value to increase debug line precision
    private const float DebugLineDensity = 0.5f;

    void Awake()
    {
        SetupPointData();
    }

    // Path Drawing
    void OnDrawGizmos()
    {
        if(DebugDraw)
        {
            DrawDebugLinesGizmo(Color.red);
        }
    }
    void OnDrawGizmosSelected()
    {
        if (DebugDraw)
        {
            DrawDebugLinesGizmo(Color.yellow);
        }
    }
    public void DrawDebugLinesGizmo(Color drawColor)
    {
        if (points.Length > 1)
        {
            // Save DynamicPath property
            bool dynamicPathTemp = DynamicPath;
            // Save Gizmo color
            Color gizmoColorTemp = Gizmos.color;
            Gizmos.color = drawColor;

            DynamicPath = false;
            SetupPointData();

            Vector3 n1, p1;

            if (transform) // start at first point + offset
                n1 = points[0] + transform.position;
            else
                n1 = points[0];

            for (float f = 0; f < PathLength - DebugLineDensity; f += DebugLineDensity)
            {
                p1 = PointAlongPath(f + DebugLineDensity);
                Gizmos.DrawLine(n1, p1);
                n1 = p1;
            }
            p1 = PointAlongPath(PathLength);
            Gizmos.DrawLine(n1, p1);

            // reset DynamicPath Property
            DynamicPath = dynamicPathTemp;
            // reset Gizmo color
            Gizmos.color = gizmoColorTemp;
                
        }
    }

    // Setup Path
    public bool SetupPointData()
    {
        if((points.Length + 1) != linearDistanceAlongPath.Length)
        {
            linearDistanceAlongPath = new float[points.Length + 1];
            linearDistanceAlongPath[0] = 0.0f;
        }

        if (points.Length > 1)
        {
            for (int i = 1; i < points.Length; ++i)
            {
                linearDistanceAlongPath[i] =
                    Vector3.Distance(points[i - 1], points[i])
                    + linearDistanceAlongPath[i - 1];
            }

            linearDistanceAlongPath[points.Length] = Vector3.Distance(points[0], points[points.Length - 1]) + linearDistanceAlongPath[points.Length - 1];
            return true;
        }

        return false;
    }

    // Types of Paths
    public enum PathInterpolator
    {
        Linear,
        Curved,
    }

    [SerializeField]
    public PathInterpolator InterpolationType;
    [SerializeField]
    public bool Circuit;
    [SerializeField]
    public bool DynamicPath;
    [SerializeField]
    public bool SmoothBetweenPoints = false;
    [SerializeField]
    public bool DebugDraw;
    [SerializeField]
    public Vector3[] points;

    [HideInInspector]
    public float[] linearDistanceAlongPath;
    [HideInInspector]
    public int PointCount
    {
        get
        {
            if (Circuit)
                return points.Length + 1;
            else
                return points.Length;
        }
    }
    [HideInInspector]
    public float PathLength
    {
        get
        {
            if(points.Length > 1) // atleast 2 points
            {
                if (Circuit)
                {
                    return linearDistanceAlongPath[points.Length];
                    // exception here at run time, usually means you should enable Dynamic Path
                }
                else
                {
                    return linearDistanceAlongPath[points.Length - 1];
                }
            }
            return 0.0f; // only 1 point
        }
    }

    /// <summary>
    /// Returns a Vector3 of the position which
    /// corresponds to the distance along the path
    /// </summary>
    public Vector3 PointAlongPath(float distanceAlongPath)
    {
        float distmod = distanceAlongPath % PathLength;
        float distNegEqualZero = (distmod + PathLength) % PathLength;
        float distPos = distmod > 0 ? distPos = distmod : distPos = PathLength;

        if (distanceAlongPath <= 0) // given negative/zero distance
            distanceAlongPath = distNegEqualZero;
        else   // given positive distance
            distanceAlongPath = distPos;

        Vector3 pos;
        if (transform != null) { pos = transform.position; }
        else { pos = new Vector3(0, 0, 0); }

        if (!DynamicPath || SetupPointData())
        {
            switch (InterpolationType)
            {
                case PathInterpolator.Linear:
                    if (SmoothBetweenPoints)
                        return InterpolateLinearSmoothPositionAlongPath(distanceAlongPath) + pos;
                    else
                        return InterpolateLinearPositionAlongPath(distanceAlongPath) + pos;
                case PathInterpolator.Curved:
                    if (SmoothBetweenPoints)
                        return InterpolateCatmullRomSmoothPositionAlongPath(distanceAlongPath) + pos;
                    else
                        return InterpolateCatmullRomPositionAlongPath(distanceAlongPath) + pos;
                default:
                    Debug.LogError("Unhandled Interpolation Type");
                    return new Vector3(0, 0, 0);
            }
        }
        else
        {
            Debug.LogError("PositionAlongLine failed to setup");
            return new Vector3(0, 0, 0);
        }
    }

    /// <summary>
    /// returns the nearest point along path to the givenPoint and the distance along
    /// the path at which this point is. There are inaccuracies for curved paths.
    /// </summary>
    public Vector3 NearestPointAlongPath(Vector3 givenPoint, out float distAlongPathToNearestPoint)
    {
        // TODO optimize for lineary Path via return vecToPointOnLine + vecToGivenPoint + pos which is closest
        distAlongPathToNearestPoint = -1.0f;
        Vector3 pos;
        if (transform != null) { pos = transform.position; }
        else { pos = new Vector3(0, 0, 0); }

        if (!DynamicPath || SetupPointData())
        {
            givenPoint = givenPoint - pos;

            float closestDist = float.MaxValue;


            //check if first point is closest
            Vector3 nearestPoint = givenPoint - points[0];
            if (nearestPoint.magnitude < closestDist)
            {
                closestDist = nearestPoint.magnitude;
                distAlongPathToNearestPoint = linearDistanceAlongPath[0];
            }

            for (int i = 1; i < PointCount; ++i)
            {
                Vector3 vecToGivenPoint = givenPoint - points[i - 1];
                Vector3 vecToNextPoint =  points[i % points.Length] - points[i - 1];
                Vector3 vecToClosestPointOnLine = Vector3.Project(vecToGivenPoint, vecToNextPoint);
                Vector3 vecToPointOnLine = (vecToClosestPointOnLine + points[i - 1]) - givenPoint;

                nearestPoint = givenPoint - points[i];
                if (nearestPoint.magnitude < closestDist)
                {
                    closestDist = nearestPoint.magnitude;
                    distAlongPathToNearestPoint = linearDistanceAlongPath[i];
                }

                if (vecToClosestPointOnLine.magnitude > vecToNextPoint.magnitude ||
                    Vector3.Dot(vecToGivenPoint, vecToNextPoint) < 0)
                    continue;

                if (vecToPointOnLine.magnitude < closestDist)
                {
                    closestDist = vecToPointOnLine.magnitude;
                    distAlongPathToNearestPoint = vecToClosestPointOnLine.magnitude + linearDistanceAlongPath[i - 1];

                    // Debug draw
                    // vecToGivenPoint
                    //Debug.DrawLine(points[i - 1], vecToGivenPoint + points[i - 1], Color.yellow); 
                    // vecToNextPoint
                    //Debug.DrawLine(points[i - 1], vecToNextPoint + points[i - 1], Color.red);  
                    // Projection of vecToGivenPoint onto vecToNextPoint
                    //Debug.DrawLine(points[i - 1], vecToClosestPointOnLine + points[i - 1], Color.magenta);
                }
            }

            // Since we already ran the SetupPointData fuction above
            // we shouldn't need to do it again in PointAlongPath, also
            // turning off smoothBetweenPoints increases accuracy
            bool tempDynamicPath = DynamicPath;
            bool tempSmoothBetweenPoints = SmoothBetweenPoints;
            DynamicPath = false;            // Optimization
            SmoothBetweenPoints = false;    // Accuracy
            Vector3 nearestPointAlongPath = PointAlongPath(distAlongPathToNearestPoint);
            DynamicPath = tempDynamicPath;
            SmoothBetweenPoints = tempSmoothBetweenPoints;
            return nearestPointAlongPath;
        }
        else
        {
            Debug.LogError("NearestPointAlongPath failed to setup");
            return pos;
        }
    }

    // returns the nearest point in the path to the givenPoint
    public Vector3 NearestPoint(Vector3 givenPoint)
    {
        Vector3 pos;
        if (transform != null) { pos = transform.position; }
        else { pos = new Vector3(0, 0, 0); }

        if (points.Length > 1)
        {
            givenPoint = givenPoint - pos;

            Vector3 nearestPoint = FFVector3.VecMaxValue;
            float nearestDist = float.MaxValue;

            foreach (Vector3 point in points)
            {
                float distToPoint = (point - givenPoint).magnitude;
                if (distToPoint < nearestDist)
                {
                    nearestDist = distToPoint;
                    nearestPoint = point;
                }
            }
            return nearestPoint + pos;
        }
        else
        {
            Debug.LogError("NearestPoint failed to setup");
            return pos;
        }
    }

    // returns the nearest point to a distance along the path
    public Vector3 NearestPoint(float distanceAlongPath)
    {
        float distmod = distanceAlongPath % PathLength;
        float distNegEqualZero = (distmod + PathLength) % PathLength;
        float distPos = distmod > 0 ? distPos = distmod : distPos = PathLength;

        if (distanceAlongPath <= 0) // given negative/zero distance
            distanceAlongPath = distNegEqualZero;
        else   // given positive distance
            distanceAlongPath = distPos;

        Vector3 pos;
        if (transform != null) { pos = transform.position; }
        else { pos = new Vector3(0, 0, 0); }


        if (!DynamicPath || SetupPointData())
        {
            int i = 0;
            int first = 1;
            int middle = PointCount / 2;
            int last = PointCount - 1;


            while (first <= last)
            {
                if (distanceAlongPath > (linearDistanceAlongPath[middle])) // greater than
                {
                    first = middle + 1;
                }
                else if (distanceAlongPath >= (linearDistanceAlongPath[middle - 1]) // equal to
                    && distanceAlongPath <= (linearDistanceAlongPath[middle]))
                {
                    i = middle;
                    break;
                }
                else // less than (dist < linearDistanceAlongPath[middle - 1])
                {
                    last = middle - 1;
                }

                middle = (first + last) / 2;
            }
            distanceAlongPath -= linearDistanceAlongPath[i - 1];
            float halfLengthBetweenPoints = (linearDistanceAlongPath[i] - linearDistanceAlongPath[i - 1])/2;
            if (distanceAlongPath > halfLengthBetweenPoints)
                return points[i % points.Length] + pos; // if we are more than halfway through line
            else
                return points[i - 1] + pos;  // if we less than or equal to than halfway through line
        }
        Debug.LogError("Error, Path failed to setup");
        return new FFVector3(0, 0, 0);
    }

    // returns the next point in the Path
    public Vector3 NextPoint(float distanceAlongPath)
    {
        float distmod = distanceAlongPath % PathLength;
        float distNegEqualZero = (distmod + PathLength) % PathLength;
        float distPos = distmod > 0 ? distPos = distmod : distPos = PathLength;

        if (distanceAlongPath <= 0) // given negative/zero distance
            distanceAlongPath = distNegEqualZero;
        else   // given positive distance
            distanceAlongPath = distPos;

        Vector3 pos;
        if (transform != null) { pos = transform.position; }
        else { pos = new Vector3(0, 0, 0); }

        if (!DynamicPath || SetupPointData())
        {
            int i = 0;
            int first = 1;
            int middle = PointCount / 2;
            int last = PointCount - 1;

            // do not mod unless >, so that we can move to the end of the path
            if (distanceAlongPath > PathLength)
                distanceAlongPath = distanceAlongPath % PathLength;

            while (first <= last)
            {
                if (distanceAlongPath > (linearDistanceAlongPath[middle])) // greater than
                {
                    first = middle + 1;
                }
                else if (distanceAlongPath >= (linearDistanceAlongPath[middle - 1]) // equal to
                    && distanceAlongPath <= (linearDistanceAlongPath[middle]))
                {
                    i = middle;
                    break;
                }
                else // less than (dist < linearDistanceAlongPath[middle - 1])
                {
                    last = middle - 1;
                }

                middle = (first + last) / 2;
            }
            return points[i % points.Length] + pos;
        }

        Debug.LogError("Error, Path failed to setup");
        return new FFVector3(0, 0, 0);

    }
    
    public Vector3 PrevPoint(float distanceAlongPath)
    {
        float distmod = distanceAlongPath % PathLength;
        float distNegEqualZero = (distmod + PathLength) % PathLength;
        float distPos = distmod > 0 ? distPos = distmod : distPos = PathLength;

        if (distanceAlongPath <= 0) // given negative/zero distance
            distanceAlongPath = distNegEqualZero;
        else   // given positive distance
            distanceAlongPath = distPos;

        Vector3 pos;
        if (transform != null) { pos = transform.position; }
        else { pos = new Vector3(0, 0, 0); }

        if (!DynamicPath || SetupPointData())
        {
            int i = 0;
            int first = 1;
            int middle = PointCount / 2;
            int last = PointCount - 1;

            // do not mod unless >, so that we can move to the end of the path
            if (distanceAlongPath > PathLength)
                distanceAlongPath = distanceAlongPath % PathLength;

            while (first <= last)
            {
                if (distanceAlongPath > (linearDistanceAlongPath[middle])) // greater than
                {
                    first = middle + 1;
                }
                else if (distanceAlongPath >= (linearDistanceAlongPath[middle - 1]) // equal to
                    && distanceAlongPath <= (linearDistanceAlongPath[middle]))
                {
                    i = middle;
                    break;
                }
                else // less than (dist < linearDistanceAlongPath[middle - 1])
                {
                    last = middle - 1;
                }

                middle = (first + last) / 2;
            }
            return points[i - 1] + pos;
        }

        Debug.LogError("Error, Path failed to setup");
        return new FFVector3(0, 0, 0);
    }

    #region InterpolationMethods
    private void GetData(float dist, out float mu, out FFVector3 n1, out FFVector3 p1)
    {
        dist = Mathf.Abs(dist);
        if (points.Length > 1)
        {
            int i = 0;
            int first = 1;
            int middle = PointCount / 2;
            int last = PointCount - 1;

            // do not mod unless >, so that we can move to the end of the path
            if (dist > PathLength)
                dist = dist % PathLength;

            while (first <= last)
            {
                if (dist > (linearDistanceAlongPath[middle])) // greater than
                {
                    first = middle + 1;
                }
                else if (dist >= (linearDistanceAlongPath[middle - 1]) // equal to
                    && dist <= (linearDistanceAlongPath[middle]))
                {
                    i = middle;
                    break;
                }
                else // less than (dist < linearDistanceAlongPath[middle - 1])
                {
                    last = middle - 1;
                }

                middle = (first + last) / 2;
            }

            int n1Index = i - 1; // TODO remove?
            int p1Index = i % points.Length;

            n1.x = points[n1Index].x;
            n1.y = points[n1Index].y;
            n1.z = points[n1Index].z;

            p1.x = points[p1Index].x;
            p1.y = points[p1Index].y;
            p1.z = points[p1Index].z;

            float lengthBetweenPoints = linearDistanceAlongPath[i] - linearDistanceAlongPath[i - 1];
            if(lengthBetweenPoints > 0.0f)
            {
                mu = (dist - linearDistanceAlongPath[i - 1]) // dist's length into Interval of points
                    / (lengthBetweenPoints); // length of interval between points
            }
            else // zero distance between points
            {
                mu = 0.0f;
            }
            return;
        }

        p1.x = -0;
        p1.y = -0;
        p1.z = -0;
        n1 = p1;
        mu = 0;
    }
    private void GetData(float dist, out float mu, out FFVector3 n1, out FFVector3 n2, out FFVector3 p1, out FFVector3 p2)
    {
        dist = Mathf.Abs(dist);
        if (points.Length > 1)
        {
            int i = 0;
            int first = 1;
            int middle = PointCount / 2;
            int last = PointCount - 1;

            // do not mod unless >, so that we can move to the end of the path
            if (dist > PathLength)
                dist = dist % PathLength;

            while (first <= last)
            {
                if (dist > (linearDistanceAlongPath[middle])) // greater than
                {
                    first = middle + 1;
                }
                else if (dist >= (linearDistanceAlongPath[middle - 1]) // equal to
                    && dist <= (linearDistanceAlongPath[middle]))
                {
                    i = middle;
                    break;
                }
                else // less than (dist < linearDistanceAlongPath[middle - 1])
                {
                    last = middle - 1;
                }

                middle = (first + last) / 2;
            }




            if (Circuit) // line loops back to first point from the last point
            {
                int n2Index = i - 2 < 0 ? points.Length - 1 : i - 2;
                int n1Index = (i - 1) % points.Length;
                int p1Index = i % points.Length;
                int p2Index = (i + 1) % points.Length;

                n2.x = points[n2Index].x;
                n2.y = points[n2Index].y;
                n2.z = points[n2Index].z;

                n1.x = points[n1Index].x;
                n1.y = points[n1Index].y;
                n1.z = points[n1Index].z;

                p1.x = points[p1Index].x;
                p1.y = points[p1Index].y;
                p1.z = points[p1Index].z;

                p2.x = points[p2Index].x;
                p2.y = points[p2Index].y;
                p2.z = points[p2Index].z;


                mu = (dist - linearDistanceAlongPath[i - 1]) // dist's length into Interval of points
                    / (linearDistanceAlongPath[i] - linearDistanceAlongPath[i - 1]); // length of interval between points

                return;
            }
            else   // Line ends and then begins again...
            {
                int n2Index = i - 2 < 0 ? points.Length - 1 : i - 2;
                int n1Index = (i - 1);
                int p1Index = i;
                int p2Index = (i + 1) % points.Length;

                n2.x = points[n2Index].x;
                n2.y = points[n2Index].y;
                n2.z = points[n2Index].z;

                n1.x = points[n1Index].x;
                n1.y = points[n1Index].y;
                n1.z = points[n1Index].z;

                p1.x = points[p1Index].x;
                p1.y = points[p1Index].y;
                p1.z = points[p1Index].z;

                p2.x = points[p2Index].x;
                p2.y = points[p2Index].y;
                p2.z = points[p2Index].z;


                float lengthBetweenPoints = linearDistanceAlongPath[i] - linearDistanceAlongPath[i - 1];
                if (lengthBetweenPoints > 0.0f)
                {
                    mu = (dist - linearDistanceAlongPath[i - 1]) // dist's length into Interval of points
                        / (lengthBetweenPoints); // length of interval between points
                }
                else // zero distance between points
                {
                    mu = 0.0f;
                }
                return;
                        
            }
        }

        p1.x = -0.1f;
        p1.y = -0.1f;
        p1.z = -0.1f;
        n1 = n2 = p2 = p1;
        mu = 0;
    }
        
    private Vector3 InterpolateLinearPositionAlongPath(float dist)
    {
        float mu;
        FFVector3 n1;
        FFVector3 p1;
        GetData(dist, out mu, out n1, out p1);
        //PrintData(dist, mu, n1, p1);
            
        return new Vector3(n1.x * (1 - mu) + p1.x * mu,
                            n1.y * (1 - mu) + p1.y * mu,
                            n1.z * (1 - mu) + p1.z * mu);
    }
    private Vector3 InterpolateLinearSmoothPositionAlongPath(float dist)
    {
        float mu;
        float mu2;
        FFVector3 n1;
        FFVector3 p1;
        GetData(dist, out mu, out n1, out p1);
        //PrintData(dist, mu, n1, p1);

        // Smoothing
        // f(x) = -(x)^2 (x -1.5) * 2;
        mu2 = -(mu * mu) * (mu - 1.5f) * 2;

        return new Vector3(n1.x * (1 - mu2) + p1.x * mu2,
                            n1.y * (1 - mu2) + p1.y * mu2,
                            n1.z * (1 - mu2) + p1.z * mu2);
    }
    private Vector3 InterpolateCatmullRomPositionAlongPath(float dist)
    {
        float mu;
        FFVector3 n2, n1, p1, p2;
        GetData(dist, out mu, out n1, out n2, out p1, out p2);
        //PrintData(dist, mu, n1, n2, p1, p2);

        //Catmull-Rom Splines
        #region Catmull-Rom Splines

        FFVector3 a0, a1, a2, a3;
        float mu2;

        mu2 = mu * mu;
        a0.x = (-0.5f * n2.x) + (1.5f * n1.x) + (-1.5f * p1.x) + (0.5f * p2.x);
        a0.y = (-0.5f * n2.y) + (1.5f * n1.y) + (-1.5f * p1.y) + (0.5f * p2.y);
        a0.z = (-0.5f * n2.z) + (1.5f * n1.z) + (-1.5f * p1.z) + (0.5f * p2.z);

        a1.x = (n2.x) + (-2.5f * n1.x) + (2.0f * p1.x) + (-0.5f * p2.x);
        a1.y = (n2.y) + (-2.5f * n1.y) + (2.0f * p1.y) + (-0.5f * p2.y);
        a1.z = (n2.z) + (-2.5f * n1.z) + (2.0f * p1.z) + (-0.5f * p2.z);

        a2.x = (-0.5f * n2.x) + (0.5f * p1.x);
        a2.y = (-0.5f * n2.y) + (0.5f * p1.y);
        a2.z = (-0.5f * n2.z) + (0.5f * p1.z);

        a3.x = n1.x;
        a3.y = n1.y;
        a3.z = n1.z;

        return new Vector3((a0.x * mu * mu2) + (a1.x * mu2) + (a2.x * mu) + (a3.x),
                            (a0.y * mu * mu2) + (a1.y * mu2) + (a2.y * mu) + (a3.y),
                            (a0.z * mu * mu2) + (a1.z * mu2) + (a2.z * mu) + (a3.z));
        #endregion Catmull-Rom Splines

        // Cubic
        #region CubicOld
        /*
        float mu2;
        float a0x, a0y, a0z;
        float a1x, a1y, a1z;
        float a2x, a2y, a2z;
        float a3x, a3y, a3z;
            
        mu2 = mu * mu;
        //
        a0x = p2.x - p1.x - n1.x + n2.x;
        a0y = p2.y - p1.y - n1.y + n2.y;
        a0z = p2.z - p1.z - n1.z + n2.z;
        //
        a1x = n2.x - n1.x - p1.x;
        a1y = n2.y - n1.y - p1.y;
        a1z = n2.z - n1.z - p1.z;
        //
        a2x = p1.x - n2.x;
        a2y = p1.y - n2.y;
        a2z = p1.z - n2.z;
        //
        a3x = n1.x;
        a3y = n1.y;
        a3z = n1.z;

        return new Vector3((a0x * mu * mu2) + (a1x * mu2) + (a2x + mu) + (a3x),
                            (a0y * mu * mu2) + (a1y * mu2) + (a2y + mu) + (a3y),
                            (a0z * mu * mu2) + (a1z * mu2) + (a2z + mu) + (a3z));
            */
        #endregion CubicOld
    }
    private Vector3 InterpolateCatmullRomSmoothPositionAlongPath(float dist)
    {
        float mu;
        FFVector3 n2, n1, p1, p2;
        GetData(dist, out mu, out n1, out n2, out p1, out p2);

        mu = -(mu * mu) * (mu - 1.5f) * 2;
        //PrintData(dist, mu, n1, n2, p1, p2);

        //Catmull-Rom Splines
        #region Catmull-Rom Splines

        FFVector3 a0, a1, a2, a3;
        float mu2;

        mu2 = mu * mu;
        a0.x = (-0.5f * n2.x) + (1.5f * n1.x) + (-1.5f * p1.x) + (0.5f * p2.x);
        a0.y = (-0.5f * n2.y) + (1.5f * n1.y) + (-1.5f * p1.y) + (0.5f * p2.y);
        a0.z = (-0.5f * n2.z) + (1.5f * n1.z) + (-1.5f * p1.z) + (0.5f * p2.z);

        a1.x = (n2.x) + (-2.5f * n1.x) + (2.0f * p1.x) + (-0.5f * p2.x);
        a1.y = (n2.y) + (-2.5f * n1.y) + (2.0f * p1.y) + (-0.5f * p2.y);
        a1.z = (n2.z) + (-2.5f * n1.z) + (2.0f * p1.z) + (-0.5f * p2.z);

        a2.x = (-0.5f * n2.x) + (0.5f * p1.x);
        a2.y = (-0.5f * n2.y) + (0.5f * p1.y);
        a2.z = (-0.5f * n2.z) + (0.5f * p1.z);

        a3.x = n1.x;
        a3.y = n1.y;
        a3.z = n1.z;

        return new Vector3((a0.x * mu * mu2) + (a1.x * mu2) + (a2.x * mu) + (a3.x),
                            (a0.y * mu * mu2) + (a1.y * mu2) + (a2.y * mu) + (a3.y),
                            (a0.z * mu * mu2) + (a1.z * mu2) + (a2.z * mu) + (a3.z));
        #endregion Catmull-Rom Splines

        // Cubic
        #region CubicOld
        /*
        float mu2;
        float a0x, a0y, a0z;
        float a1x, a1y, a1z;
        float a2x, a2y, a2z;
        float a3x, a3y, a3z;
            
        mu2 = mu * mu;
        //
        a0x = p2.x - p1.x - n1.x + n2.x;
        a0y = p2.y - p1.y - n1.y + n2.y;
        a0z = p2.z - p1.z - n1.z + n2.z;
        //
        a1x = n2.x - n1.x - p1.x;
        a1y = n2.y - n1.y - p1.y;
        a1z = n2.z - n1.z - p1.z;
        //
        a2x = p1.x - n2.x;
        a2y = p1.y - n2.y;
        a2z = p1.z - n2.z;
        //
        a3x = n1.x;
        a3y = n1.y;
        a3z = n1.z;

        return new Vector3((a0x * mu * mu2) + (a1x * mu2) + (a2x + mu) + (a3x),
                            (a0y * mu * mu2) + (a1y * mu2) + (a2y + mu) + (a3y),
                            (a0z * mu * mu2) + (a1z * mu2) + (a2z + mu) + (a3z));
            */
        #endregion CubicOld
    }

    //Debug print for GetData
    private void PrintData(float dist, float mu, FFVector3 n1, FFVector3 p1)
    {
        Debug.Log("Dist: " + dist);
        Debug.Log("mu: " + mu);
        Debug.Log("n1: " + "(" + n1.x + "," + n1.y + "," + n1.z + ")");
        Debug.Log("p1: " + "(" + p1.x + "," + p1.y + "," + p1.z + ")");
    }
    //Debug print for GetData
    private void PrintData(float dist, float mu, FFVector3 n1, FFVector3 n2, FFVector3 p1, FFVector3 p2)
    {
        Debug.Log("Dist: " + dist);
        Debug.Log("mu: " + mu);
        Debug.Log("n2: " + "(" + n2.x + "," + n2.y + "," + n2.z + ")");
        Debug.Log("n1: " + "(" + n1.x + "," + n1.y + "," + n1.z + ")");
        Debug.Log("p1: " + "(" + p1.x + "," + p1.y + "," + p1.z + ")");
        Debug.Log("p2: " + "(" + p2.x + "," + p2.y + "," + p2.z + ")");
    }
    #endregion InterpolationMethods
}