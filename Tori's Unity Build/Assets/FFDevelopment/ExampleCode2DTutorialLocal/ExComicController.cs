using UnityEngine;
using System.Collections;

/*
 * This is an example script which shows how
 * you might make a comic reader script to move
 * around the camera. 
 */

public class ExComicController : FFComponent {

    FFAction.ActionSequence seq;

    float distAlongPath = 0;

    [Range(0.1f,100.0f)]
    public float ComicCameraSpeed;

    public Transform [] Paths;
    int currentPathNumber = 0;
    FFPath currentPath;

	// Use this for initialization
	void Start () {
        if(Paths.Length > 0)
        {
            seq = action.Sequence();
            seq.Call(WaitForInput);
            currentPath = Paths[0].GetComponent<FFPath>();
        }

	}

    void MoveForward()
    {
        if (distAlongPath >= currentPath.PathLength) // reached end of path
        {
            transform.position = currentPath.PointAlongPath(currentPath.PathLength); // goto end
            ++currentPathNumber;

            if(currentPathNumber < Paths.Length)
                currentPath = Paths[currentPathNumber].GetComponent<FFPath>();

            seq.Call(WaitForInput); // waitForInput
            return;
        }
        else
        {
            transform.position = currentPath.PointAlongPath(distAlongPath);
        }

        distAlongPath += Time.deltaTime * ComicCameraSpeed;
        seq.Sync();
        seq.Call(MoveForward);
    }
    void MoveBackward()
    {
        if (distAlongPath <= 0.0f) // reached begining
        {
            transform.position = currentPath.PointAlongPath(0.0f); // goto begining
            seq.Call(WaitForInput); // waitForInput
            return;
        }
        else
        {
            transform.position = currentPath.PointAlongPath(distAlongPath);
        }

        distAlongPath -= Time.deltaTime * ComicCameraSpeed;
        seq.Sync();
        seq.Call(MoveBackward);
    }

    void WaitForInput()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))  // go forwards
        {
            if (currentPath != null && currentPathNumber < Paths.Length)
            {
                distAlongPath = 0.0f;
                seq.Call(MoveForward);
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))  // go backwards
        {
            if (currentPath != null && currentPathNumber > 0.0f)
            {
                --currentPathNumber;
                currentPath = Paths[currentPathNumber].GetComponent<FFPath>();
                distAlongPath = currentPath.PathLength;
                seq.Call(MoveBackward);
                return;
            }
        }

        seq.Sync();
        seq.Call(WaitForInput);
    }
	
}
