using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

#if UNITY_EDITOR
using input = GoogleARCore.InstantPreviewInput;
#endif

public class ARController : MonoBehaviour {

    private List<TrackedPlane> m_NewTrackedPlanes = new List<TrackedPlane>();
    public GameObject GridPrefab;
    public GameObject portal;
    public GameObject ARCamera;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        //checking the status
        if(Session.Status != SessionStatus.Tracking)
        {
            return;
        }
        //m_trackedplanes filled with planes ar core detects in current frame
        Session.GetTrackables<TrackedPlane>(m_NewTrackedPlanes, TrackableQueryFilter.New);
		

        for(int i = 0; i < m_NewTrackedPlanes.Count; i++)
        {
            GameObject grid = Instantiate(GridPrefab, Vector3.zero, Quaternion.identity, transform);

            grid.GetComponent<GridVisualizer>().Initialize(m_NewTrackedPlanes[i]);
        }

        Touch touch;
        if(Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
        {
            return;
        }

        TrackableHit hit;
        if(Frame.Raycast(touch.position.x, touch.position.y, TrackableHitFlags.PlaneWithinPolygon, out hit))
        {
            //enable the portal
            portal.SetActive(true);

            //anchor
            Anchor anchor = hit.Trackable.CreateAnchor(hit.Pose);

            //set the portal positioin
            portal.transform.position = hit.Pose.position;
            portal.transform.rotation = hit.Pose.rotation;

            Vector3 camerePosition = ARCamera.transform.position;

            camerePosition.y = hit.Pose.position.y;

            portal.transform.LookAt(camerePosition, portal.transform.up);

            portal.transform.parent = anchor.transform;

        }
	}
}
